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
using QuantConnect.Data;
using QuantConnect.Data.Market;
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
using System.Linq;
using System.Net;
using QuantConnect.Brokerages.Bitfinex.Messages;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Bitfinex Brokerage implementation
    /// </summary>
    public partial class BitfinexBrokerage
    {
        private const string ApiVersion = "v2";
        private const string RestApiUrl = "https://api.bitfinex.com";
        private const string WebSocketUrl = "wss://api.bitfinex.com/ws/2";

        private readonly IAlgorithm _algorithm;
        private readonly RateGate _restRateLimiter = new RateGate(10, TimeSpan.FromMinutes(1));
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;
        private readonly IDataAggregator _aggregator;

        // map Bitfinex ClientOrderId -> LEAN order (only used for orders submitted in PlaceOrder, not for existing orders)
        private readonly ConcurrentDictionary<long, Order> _orderMap = new ConcurrentDictionary<long, Order>();
        private readonly object _clientOrderIdLocker = new object();
        private long _nextClientOrderId;

        // map Bitfinex currency to LEAN currency
        private readonly Dictionary<string, string> _currencyMap;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        public readonly object TickLocker = new object();

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        /// <param name="aggregator">consolidate ticks</param>
        public BitfinexBrokerage(string apiKey, string apiSecret, IAlgorithm algorithm, IPriceProvider priceProvider, IDataAggregator aggregator)
            : this(new WebSocketClientWrapper(), new RestClient(RestApiUrl), apiKey, apiSecret, algorithm, priceProvider, aggregator)
        {
        }

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        /// <param name="aggregator">consolidate ticks</param>
        public BitfinexBrokerage(IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, IAlgorithm algorithm, IPriceProvider priceProvider, IDataAggregator aggregator)
            : base(WebSocketUrl, websocket, restClient, apiKey, apiSecret, "Bitfinex")
        {
            SubscriptionManager = new BitfinexSubscriptionManager(this, WebSocketUrl, _symbolMapper);
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            _algorithm = algorithm;
            _aggregator = aggregator;

            // load currency map
            using (var wc = new WebClient())
            {
                var json = wc.DownloadString("https://api-pub.bitfinex.com/v2/conf/pub:map:currency:sym");
                var rows = JsonConvert.DeserializeObject<List<List<List<string>>>>(json)[0];
                _currencyMap = rows
                    .ToDictionary(row => row[0], row => row[1].ToUpperInvariant());
            }

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
        /// Should be empty, Bitfinex brokerage manages his public channels including subscribe/unsubscribe/reconnect methods using <see cref="BitfinexSubscriptionManager"/>
        /// Not used in master
        /// </summary>
        /// <param name="symbols"></param>
        public override void Subscribe(IEnumerable<Symbol> symbols) { }

        private long GetNextClientOrderId()
        {
            lock (_clientOrderIdLocker)
            {
                // ensure unique id
                var id = Convert.ToInt64(Time.DateTimeToUnixTimeStampMilliseconds(DateTime.UtcNow));

                if (id > _nextClientOrderId)
                {
                    _nextClientOrderId = id;
                }
                else
                {
                    _nextClientOrderId++;
                }
            }

            return _nextClientOrderId;
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

                    // account information channel
                    if (channel == 0)
                    {
                        var term = token[1].ToObject<string>();
                        switch (term.ToLowerInvariant())
                        {
                            // order closed
                            case "oc":
                                OnOrderClose(token[2].ToObject<Messages.Order>());
                                return;

                            // trade execution update
                            case "tu":
                                EmitFillOrder(token[2].ToObject<TradeExecutionUpdate>());
                                return;

                            // notification
                            case "n":
                                var notification = token[2];
                                var status = notification[6].ToString();

                                if (status == "ERROR")
                                {
                                    var errorMessage = notification[7].ToString();
                                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Error: {errorMessage}"));

                                    OnOrderError(notification[4].ToObject<Messages.Order>());
                                }
                                else if (status == "SUCCESS")
                                {
                                    var type = notification[1].ToString();

                                    if (type == "on-req")
                                    {
                                        OnOrderNew(notification[4].ToObject<Messages.Order>());
                                    }
                                    else if (type == "ou-req")
                                    {
                                        OnOrderUpdate(notification[4].ToObject<Messages.Order>());
                                    }
                                }
                                return;

                            default:
                                return;
                        }
                    }
                }
                else if (token is JObject)
                {
                    var raw = token.ToObject<BaseMessage>();
                    switch (raw.Event.ToLowerInvariant())
                    {
                        case "auth":
                            var auth = token.ToObject<AuthResponseMessage>();
                            var result = string.Equals(auth.Status, "OK", StringComparison.OrdinalIgnoreCase) ? "succeed" : "failed";
                            Log.Trace($"BitfinexWebsocketsBrokerage.OnMessage: Subscribing to authenticated channels {result}");
                            return;

                        case "info":
                        case "ping":
                            return;

                        case "error":
                            var error = token.ToObject<ErrorMessage>();
                            Log.Error($"BitfinexWebsocketsBrokerage.OnMessage: {error.Level}: {error.Message}");
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

        private void OnOrderError(Messages.Order bitfinexOrder)
        {
            Order order;
            if (_orderMap.TryGetValue(bitfinexOrder.ClientOrderId, out order))
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Bitfinex Order Event")
                {
                    Status = OrderStatus.Invalid
                });
            }
        }

        private void OnOrderNew(Messages.Order bitfinexOrder)
        {
            if (bitfinexOrder.Status == "ACTIVE")
            {
                var brokerId = bitfinexOrder.Id.ToStringInvariant();

                Order order;
                if (_orderMap.TryGetValue(bitfinexOrder.ClientOrderId, out order))
                {
                    if (CachedOrderIDs.ContainsKey(order.Id))
                    {
                        CachedOrderIDs[order.Id].BrokerId.Clear();
                        CachedOrderIDs[order.Id].BrokerId.Add(brokerId);
                    }
                    else
                    {
                        order.BrokerId.Add(brokerId);
                        CachedOrderIDs.TryAdd(order.Id, order);
                    }

                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Bitfinex Order Event")
                    {
                        Status = OrderStatus.Submitted
                    });
                }
            }
        }

        private void OnOrderUpdate(Messages.Order bitfinexOrder)
        {
            if (bitfinexOrder.Status == "ACTIVE")
            {
                var brokerId = bitfinexOrder.Id.ToStringInvariant();

                var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;
                if (order == null)
                {
                    order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
                    if (order == null)
                    {
                        Log.Error($"OnOrderUpdate(): order not found: BrokerId: {brokerId}");
                        return;
                    }
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Bitfinex Order Event")
                {
                    Status = OrderStatus.UpdateSubmitted
                });
            }
        }

        private void OnOrderClose(Messages.Order bitfinexOrder)
        {
            if (bitfinexOrder.Status.StartsWith("CANCELED"))
            {
                var brokerId = bitfinexOrder.Id.ToStringInvariant();

                var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;
                if (order == null)
                {
                    order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
                    if (order == null)
                    {
                        Log.Error($"OnOrderClose(): order not found: BrokerId: {brokerId}");
                        return;
                    }
                }
                else
                {
                    Order outOrder;
                    CachedOrderIDs.TryRemove(order.Id, out outOrder);
                }

                if (bitfinexOrder.ClientOrderId > 0)
                {
                    Order removed;
                    _orderMap.TryRemove(bitfinexOrder.ClientOrderId, out removed);
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Bitfinex Order Event")
                {
                    Status = OrderStatus.Canceled
                });
            }
        }

        private void EmitFillOrder(TradeExecutionUpdate update)
        {
            try
            {
                var brokerId = update.OrderId.ToStringInvariant();

                var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;

                if (order == null)
                {
                    order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
                    if (order == null)
                    {
                        Log.Error($"EmitFillOrder(): order not found: BrokerId: {brokerId}");
                        return;
                    }
                }

                var symbol = _symbolMapper.GetLeanSymbol(update.Symbol, SecurityType.Crypto, Market.Bitfinex);
                var fillPrice = update.ExecPrice;
                var fillQuantity = update.ExecAmount;
                var direction = fillQuantity < 0 ? OrderDirection.Sell : OrderDirection.Buy;
                var updTime = Time.UnixMillisecondTimeStampToDateTime(update.MtsCreate);
                var orderFee = new OrderFee(new CashAmount(Math.Abs(update.Fee), GetLeanCurrency(update.FeeCurrency)));

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

                if (_algorithm.BrokerageModel.AccountType == AccountType.Cash &&
                    order.Direction == OrderDirection.Buy)
                {
                    // fees are debited in the base currency, so we have to subtract them from the filled quantity
                    fillQuantity -= orderFee.Value.Amount;

                    orderFee = new ModifiedFillQuantityOrderFee(orderFee.Value);
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

                    var clientOrderId = _orderMap.FirstOrDefault(x => x.Value.BrokerId.Contains(brokerId)).Key;
                    if (clientOrderId > 0)
                    {
                        _orderMap.TryRemove(clientOrderId, out outOrder);
                    }
                }

                OnOrderEvent(orderEvent);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private string GetLeanCurrency(string brokerageCurrency)
        {
            string currency;
            if (!_currencyMap.TryGetValue(brokerageCurrency.ToUpperInvariant(), out currency))
            {
                currency = brokerageCurrency.ToUpperInvariant();
            }

            return currency;
        }

        /// <summary>
        /// Emit stream tick
        /// </summary>
        /// <param name="tick"></param>
        public void EmitTick(Tick tick)
        {
            _aggregator.Update(tick);
        }

        /// <summary>
        /// Should be empty. <see cref="BitfinexSubscriptionManager"/> manages each <see cref="BitfinexWebSocketWrapper"/> individually
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Symbol> GetSubscribed() => new List<Symbol>();
    }
}
