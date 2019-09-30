/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Bitfinex Brokerage implementation
    /// </summary>
    public partial class BitfinexBrokerage
    {
        private const string ApiVersion = "v1";
        private readonly IAlgorithm _algorithm;
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private volatile bool _streamLocked;
        private readonly RateGate _restRateLimiter = new RateGate(8, TimeSpan.FromMinutes(1));
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();
        private readonly BitfinexSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        public readonly object TickLocker = new object();

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="restUrl">rest api url</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        public BitfinexBrokerage(string wssUrl, string restUrl, string apiKey, string apiSecret, IAlgorithm algorithm, IPriceProvider priceProvider)
            : base(wssUrl, new WebSocketWrapper(), new RestClient(restUrl), apiKey, apiSecret, Market.Bitfinex, "Bitfinex")
        {
            _subscriptionManager = new BitfinexSubscriptionManager(this, wssUrl, _symbolMapper);
            _algorithm = algorithm;

            WebSocket.Open += (sender, args) =>
            {
                SubscribeAuth();
            };
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMessage(object sender, WebSocketMessage e)
        {
            LastHeartbeatUtcTime = DateTime.UtcNow;

            // Verify if we're allowed to handle the streaming packet yet; while we're placing an order we delay the
            // stream processing a touch.
            try
            {
                if (_streamLocked)
                {
                    _messageBuffer.Enqueue(e);
                    return;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            OnMessageImpl(e);
        }

        /// <summary>
        /// Subscribes to the authenticated channels (using an single streaming channel)
        /// </summary>
        public void SubscribeAuth()
        {
            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiSecret))
                return;

            var authNonce = GetNonce();
            var authPayload = "AUTH" + authNonce;
            var authSig = AuthenticationToken(authPayload);
            WebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "auth",
                apiKey = ApiKey,
                authNonce,
                authPayload,
                authSig
            }));

            Log.Trace("BitfinexBrokerage.SubscribeAuth(): Sent authentication request.");
        }

        /// <summary>
        /// Subscribes to the requested symbols (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (_subscriptionManager.IsSubscribed(symbol) ||
                    symbol.Value.Contains("UNIVERSE") ||
                    !_symbolMapper.IsKnownBrokerageSymbol(symbol.Value) ||
                    symbol.SecurityType != _symbolMapper.GetLeanSecurityType(symbol.Value))
                {
                    continue;
                }

                _subscriptionManager.Subscribe(symbol);

                Log.Trace($"BitfinexBrokerage.Subscribe(): Sent subscribe for {symbol.Value}.");
            }
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                _subscriptionManager.Unsubscribe(symbol);

                Log.Trace($"BitfinexBrokerage.Unsubscribe(): Sent unsubscribe for {symbol.Value}.");
            }
        }

        /// <summary>
        /// Implementation of the OnMessage event
        /// </summary>
        /// <param name="e"></param>
        private void OnMessageImpl(WebSocketMessage e)
        {
            try
            {
                var token = JToken.Parse(e.Message);

                if (token is JArray)
                {
                    var channel = token[0].ToObject<int>();
                    // heartbeat
                    if (token[1].Type == JTokenType.String && token[1].Value<string>() == "hb")
                    {
                        return;
                    }
                    //public channels
                    if (channel == 0)
                    {
                        var term = token[1].ToObject<string>();
                        switch (term.ToLowerInvariant())
                        {
                            case "oc":
                                OnOrderClose(token[2].ToObject<string[]>());
                                return;
                            case "tu":
                                EmitFillOrder(token[2].ToObject<string[]>());
                                return;
                            default:
                                return;
                        }
                    }
                }
                else if (token is JObject)
                {
                    var raw = token.ToObject<Messages.BaseMessage>();
                    switch (raw.Event.ToLowerInvariant())
                    {
                        case "auth":
                            var auth = token.ToObject<Messages.AuthResponseMessage>();
                            var result = string.Equals(auth.Status, "OK", StringComparison.OrdinalIgnoreCase) ? "succeed" : "failed";
                            Log.Trace($"BitfinexWebsocketsBrokerage.OnMessage: Subscribing to authenticated channels {result}");
                            return;
                        case "info":
                        case "ping":
                            return;
                        case "error":
                            var error = token.ToObject<Messages.ErrorMessage>();
                            Log.Trace($"BitfinexWebsocketsBrokerage.OnMessage: {error.Level}: {error.Message}");
                            return;
                        default:
                            Log.Trace($"BitfinexWebsocketsBrokerage.OnMessage: Unexpected message format: {e.Message}");
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnOrderClose(string[] entries)
        {
            var brokerId = entries[0];
            if (entries[5].IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;
                if (order == null)
                {
                    order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
                    if (order == null)
                    {
                        // not our order, nothing else to do here
                        return;
                    }
                }
                Order outOrder;
                if (CachedOrderIDs.TryRemove(order.Id, out outOrder))
                {
                    OnOrderEvent(new OrderEvent(order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Bitfinex Order Event") { Status = OrderStatus.Canceled });
                }
            }
        }

        private void EmitFillOrder(string[] entries)
        {
            try
            {
                var brokerId = entries[4];
                var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;
                if (order == null)
                {
                    order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
                    if (order == null)
                    {
                        // not our order, nothing else to do here
                        return;
                    }
                }

                var symbol = _symbolMapper.GetLeanSymbol(entries[2]);
                var fillPrice = decimal.Parse(entries[6], NumberStyles.Float, CultureInfo.InvariantCulture);
                var fillQuantity = decimal.Parse(entries[5], NumberStyles.Float, CultureInfo.InvariantCulture);
                var direction = fillQuantity < 0 ? OrderDirection.Sell : OrderDirection.Buy;
                var updTime = Time.UnixTimeStampToDateTime(double.Parse(entries[3], NumberStyles.Float, CultureInfo.InvariantCulture));
                var orderFee = new OrderFee(new CashAmount(
                        Math.Abs(decimal.Parse(entries[9], NumberStyles.Float, CultureInfo.InvariantCulture)),
                        entries[10]
                    ));

                var status = OrderStatus.Filled;
                if (fillQuantity != order.Quantity)
                {
                    decimal totalFillQuantity;
                    _fills.TryGetValue(order.Id, out totalFillQuantity);
                    totalFillQuantity += fillQuantity;
                    _fills[order.Id] = totalFillQuantity;

                    status = totalFillQuantity == order.Quantity
                        ? OrderStatus.Filled
                        : OrderStatus.PartiallyFilled;
                }

                var orderEvent = new OrderEvent
                (
                    order.Id, symbol, updTime, status,
                    direction, fillPrice, fillQuantity,
                    orderFee, $"Bitfinex Order Event {direction}"
                );

                // if the order is closed, we no longer need it in the active order list
                if (status == OrderStatus.Filled)
                {
                    Order outOrder;
                    CachedOrderIDs.TryRemove(order.Id, out outOrder);
                    decimal ignored;
                    _fills.TryRemove(order.Id, out ignored);
                }

                OnOrderEvent(orderEvent);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the REST call returns.
        /// </summary>
        public void LockStream()
        {
            Log.Trace("BitfinexBrokerage.Messaging.LockStream(): Locking Stream");
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        public void UnlockStream()
        {
            Log.Trace("BitfinexBrokerage.Messaging.UnlockStream(): Processing Backlog...");
            while (_messageBuffer.Any())
            {
                WebSocketMessage e;
                _messageBuffer.TryDequeue(out e);
                OnMessageImpl(e);
            }
            Log.Trace("BitfinexBrokerage.Messaging.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        /// <summary>
        /// Gets a list of current subscriptions
        /// </summary>
        /// <returns></returns>
        protected override IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();
            lock (ChannelList)
            {
                foreach (var ticker in ChannelList.Select(x => x.Value.Symbol).Distinct())
                {
                    list.Add(_symbolMapper.GetLeanSymbol(ticker));
                }
            }
            return list;
        }
    }

    /// <summary>
    /// Represents Bitfinex channel information
    /// </summary>
    public class BitfinexChannel : BaseWebsocketsBrokerage.Channel
    {
        /// <summary>
        /// Represents channel identifier for specific subscription
        /// </summary>
        public string ChannelId { get; set; }
    }
}
