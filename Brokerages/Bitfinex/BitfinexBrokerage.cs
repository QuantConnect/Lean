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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        private readonly BitfinexSymbolMapper _symbolMapper = new BitfinexSymbolMapper();

        #region IBrokerage
        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            return SubmitOrder(GetEndpoint("order/new"), order);
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            if (order.BrokerId.Count == 0)
            {
                throw new ArgumentNullException("BitfinexBrokerage.UpdateOrder: There is no brokerage id to be updated for this order.");
            }
            if (order.BrokerId.Count > 1)
            {
                throw new NotSupportedException("BitfinexBrokerage.UpdateOrder: Multiple orders update not supported. Please cancel and re-create.");
            }

            return SubmitOrder(GetEndpoint("order/cancel/replace"), order);
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("BitfinexBrokerage.CancelOrder(): {0}", order);

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform a cancellation
                Log.Trace("BitfinexBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            LockStream();
            var endpoint = GetEndpoint("order/cancel/multi");
            var payload = new JsonObject();
            payload.Add("request", endpoint);
            payload.Add("nonce", GetNonce().ToString());
            payload.Add("order_ids", order.BrokerId.Select(i => long.Parse(i)));

            var request = new RestRequest(endpoint, Method.POST);
            request.AddJsonBody(payload.ToString());
            SignRequest(request, payload.ToString());

            var response = ExecuteRestRequest(request);
            var cancellationSubmitted = false;
            if (response.StatusCode == HttpStatusCode.OK && !(response.Content?.IndexOf("None to cancel", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.CancelPending });

                cancellationSubmitted = true;
            }

            UnlockStream();
            return cancellationSubmitted;
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

            var response = ExecuteRestRequest(request);

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
                order.Symbol = _symbolMapper.GetLeanSymbol(item.Symbol);
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

            var response = ExecuteRestRequest(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetAccountHoldings: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var positions = JsonConvert.DeserializeObject<Messages.Position[]>(response.Content);
            return positions.Where(p => p.Amount != 0)
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

            var response = ExecuteRestRequest(request);

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
                    else if (_symbolMapper.IsKnownBrokerageCurrency(item.Currency))
                    {
                        var rate = GetConversionRate(item.Currency);
                        list.Add(new Cash(item.Currency.ToUpper(), item.Available, rate));
                    }
                    else
                    {
                        var symbol = item.Currency + "USD";
                        var tick = GetTick(_symbolMapper.GetLeanSymbol(symbol));

                        list.Add(new Cash(item.Currency.ToUpper(), item.Available, tick.Price));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(Data.HistoryRequest request)
        {
            string resolution = ConvertResolution(request.Resolution);
            long resolutionInMS = (long)request.Resolution.ToTimeSpan().TotalMilliseconds;
            string symbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            long startMTS = (long)Time.DateTimeToUnixTimeStamp(request.StartTimeUtc) * 1000;
            long endMTS = (long)Time.DateTimeToUnixTimeStamp(request.EndTimeUtc) * 1000;
            string endpoint = $"v2/candles/trade:{resolution}:t{symbol}/hist?limit=1000&sort=1";

            while ((endMTS - startMTS) > resolutionInMS)
            {
                var timeframe = $"&start={startMTS}&end={endMTS}";

                var restRequest = new RestRequest(endpoint + timeframe, Method.GET);
                var response = ExecuteRestRequest(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"BitfinexBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                }

                var candles = JsonConvert.DeserializeObject<object[][]>(response.Content)
                    .Select(entries => new Messages.Candle(entries))
                    .ToList();

                RestoreMissedCandles(candles, startMTS, endMTS, resolutionInMS);

                startMTS = candles.Last().Timestamp + resolutionInMS;
                var period = request.Resolution.ToTimeSpan();

                foreach (var candle in candles)
                {
                    yield return new TradeBar()
                    {
                        Time = Time.UnixMillisecondTimeStampToDateTime(candle.Timestamp),
                        Symbol = request.Symbol,
                        Low = candle.Low,
                        High = candle.High,
                        Open = candle.Open,
                        Close = candle.Close,
                        Volume = candle.Volume,
                        Value = candle.Close,
                        DataType = MarketDataType.TradeBar,
                        Period = period,
                        EndTime = Time.UnixMillisecondTimeStampToDateTime(candle.Timestamp + (long)period.TotalMilliseconds)
                    };
                }
            }
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
