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
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Data;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        #region IBrokerage
        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            base.Disconnect();

            WebSocket.Close();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();
            var endpoint = GetEndpoint("orders");
            var request = new RestRequest(endpoint, Method.POST);

            JsonObject payload = new JsonObject();
            payload.Add("request", endpoint);
            payload.Add("nonce", GetNonce().ToString());

            request.AddJsonBody(payload.ToString());
            SignRequest(request, payload.ToString());

            var response = ExecuteRestRequest(request, BitfinexEndpointType.Private);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetOpenOrders: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var orders = JsonConvert.DeserializeObject<Messages.Order[]>(response.Content)
                .Where(OrderFilter(_algorithm.BrokerageModel.AccountType));
            foreach (var item in orders)
            {
                Order order;
                if (item.Type.Replace("exchange", "").Trim() == "market")
                {
                    order = new MarketOrder { Price = item.Price };
                }
                else if (item.Type.Replace("exchange", "").Trim() == "limit")
                {
                    order = new LimitOrder { LimitPrice = item.Price };
                }
                else if (item.Type.Replace("exchange", "").Trim() == "stop")
                {
                    order = new StopMarketOrder { StopPrice = item.Price };
                }
                else
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode,
                        "BitfinexBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                    continue;
                }

                order.Quantity = item.Side == "sell" ? -item.OriginalAmount : item.OriginalAmount;
                order.BrokerId = new List<string> { item.Id };
                order.Symbol = CreateSymbol(item.Symbol);
                order.Time = Time.UnixTimeStampToDateTime(item.Timestamp);
                order.Status = ConvertOrderStatus(item);
                order.Price = item.Price;
                list.Add(order);
            }

            foreach (var item in list)
            {
                if (item.Status.IsOpen())
                {
                    var cached = CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                    if (cached.Any())
                    {
                        CachedOrderIDs[cached.First().Key] = item;
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var endpoint = GetEndpoint("positions");
            var request = new RestRequest(endpoint, Method.POST);

            JsonObject payload = new JsonObject();
            payload.Add("request", endpoint);
            payload.Add("nonce", GetNonce().ToString());

            request.AddJsonBody(payload.ToString());
            SignRequest(request, payload.ToString());

            var response = ExecuteRestRequest(request, BitfinexEndpointType.Private);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetAccountHoldings: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var positions = JsonConvert.DeserializeObject<Messages.Position[]>(response.Content);
            return positions.Where(p=> p.Amount != 0)
                .Select(ConvertHolding)
                .ToList();
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<Cash> GetCashBalance()
        {
            var list = new List<Cash>();
            var endpoint = GetEndpoint("balances"); ;
            var request = new RestRequest(endpoint, Method.POST);

            JsonObject payload = new JsonObject();
            payload.Add("request", endpoint);
            payload.Add("nonce", GetNonce().ToString());

            request.AddJsonBody(payload.ToString());
            SignRequest(request, payload.ToString());

            var response = ExecuteRestRequest(request, BitfinexEndpointType.Private);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var availableWallets = JsonConvert.DeserializeObject<Messages.Wallet[]>(response.Content)
                .Where(WalletFilter(_algorithm.BrokerageModel.AccountType));
            foreach (var item in availableWallets)
            {
                if (item.Available > 0)
                {
                    if (string.Equals(item.Currency, "USD", StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(new Cash(item.Currency, item.Available, 1));
                    }
                    else if (new[] { "JPY", "GBP", "EUR" }.Contains(item.Currency, StringComparer.OrdinalIgnoreCase))
                    {
                        var rate = GetConversionRate(item.Currency);
                        list.Add(new Cash(item.Currency.ToUpper(), item.Available, rate));
                    }
                    else
                    {
                        var tick = GetTick(Symbol.Create((item.Currency + "USD").ToUpper(), SecurityType.Crypto, Market.Bitfinex));

                        list.Add(new Cash(item.Currency.ToUpper(), item.Available, tick.Price));
                    }
                }
            }

            return list;
        }

        #endregion

        #region IDataQueueHandler
        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (TickLocker)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            Subscribe(symbols);
        }


        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            Unsubscribe(symbols);
        }
        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _restRateLimiter.Dispose();
        }
    }
}
