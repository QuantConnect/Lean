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
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using QuantConnect.Brokerages.Bitfinex.Messages;
using QuantConnect.Securities.Crypto;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Bitfinex Brokerage implementation
    /// </summary>
    public partial class BitfinexBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        private readonly SymbolPropertiesDatabaseSymbolMapper _symbolMapper = new SymbolPropertiesDatabaseSymbolMapper(Market.Bitfinex);

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
            var parameters = new JsonObject
            {
                { "symbol", _symbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "amount", order.Quantity.ToStringInvariant() },
                { "type", ConvertOrderType(_algorithm.BrokerageModel.AccountType, order.Type) },
                { "price", GetOrderPrice(order).ToStringInvariant() }
            };

            var orderProperties = order.Properties as BitfinexOrderProperties;
            if (orderProperties != null)
            {
                if (order.Type == OrderType.Limit)
                {
                    var flags = 0;
                    if (orderProperties.Hidden) flags |= OrderFlags.Hidden;
                    if (orderProperties.PostOnly) flags |= OrderFlags.PostOnly;

                    parameters.Add("flags", flags);
                }
            }

            var clientOrderId = GetNextClientOrderId();
            parameters.Add("cid", clientOrderId);

            _orderMap.TryAdd(clientOrderId, order);

            var obj = new JsonArray { 0, "on", null, parameters };
            var json = JsonConvert.SerializeObject(obj);
            WebSocket.Send(json);

            return true;
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
                throw new ArgumentNullException(nameof(order.BrokerId), "BitfinexBrokerage.UpdateOrder: There is no brokerage id to be updated for this order.");
            }

            if (order.BrokerId.Count > 1)
            {
                throw new NotSupportedException("BitfinexBrokerage.UpdateOrder: Multiple orders update not supported. Please cancel and re-create.");
            }

            var parameters = new JsonObject
            {
                { "id", Parse.Long(order.BrokerId.First()) },
                { "amount", order.Quantity.ToStringInvariant() },
                { "price", GetOrderPrice(order).ToStringInvariant() }
            };

            var obj = new JsonArray { 0, "ou", null, parameters };
            var json = JsonConvert.SerializeObject(obj);
            WebSocket.Send(json);

            return true;
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

            var parameters = new JsonObject
            {
                { "id", order.BrokerId.Select(Parse.Long).First() }
            };

            var obj = new JsonArray { 0, "oc", null, parameters };
            var json = JsonConvert.SerializeObject(obj);
            WebSocket.Send(json);

            return true;
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            WebSocket.Close();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var endpoint = GetEndpoint("auth/r/orders");
            var request = new RestRequest(endpoint, Method.POST);

            var parameters = new JsonObject();

            request.AddJsonBody(parameters.ToString());
            SignRequest(request, endpoint, parameters);

            var response = ExecuteRestRequest(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetOpenOrders: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var orders = JsonConvert.DeserializeObject<Messages.Order[]>(response.Content)
                .Where(OrderFilter(_algorithm.BrokerageModel.AccountType));

            var list = new List<Order>();
            foreach (var item in orders)
            {
                Order order;
                if (item.Type.Replace("EXCHANGE", "").Trim() == "MARKET")
                {
                    order = new MarketOrder { Price = item.Price };
                }
                else if (item.Type.Replace("EXCHANGE", "").Trim() == "LIMIT")
                {
                    order = new LimitOrder { LimitPrice = item.Price };
                }
                else if (item.Type.Replace("EXCHANGE", "").Trim() == "STOP")
                {
                    order = new StopMarketOrder { StopPrice = item.Price };
                }
                else
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode,
                        "BitfinexBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                    continue;
                }

                order.Quantity = item.Amount;
                order.BrokerId = new List<string> { item.Id.ToStringInvariant() };
                order.Symbol = _symbolMapper.GetLeanSymbol(item.Symbol, SecurityType.Crypto, Market.Bitfinex);
                order.Time = Time.UnixMillisecondTimeStampToDateTime(item.MtsCreate);
                order.Status = ConvertOrderStatus(item);
                order.Price = item.Price;
                list.Add(order);
            }

            foreach (var item in list)
            {
                if (item.Status.IsOpen())
                {
                    var cached = CachedOrderIDs
                        .FirstOrDefault(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                    if (cached.Value != null)
                    {
                        CachedOrderIDs[cached.Key] = item;
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
            var endpoint = GetEndpoint("auth/r/positions");
            var request = new RestRequest(endpoint, Method.POST);

            var parameters = new JsonObject();

            request.AddJsonBody(parameters.ToString());
            SignRequest(request, endpoint, parameters);

            var response = ExecuteRestRequest(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetAccountHoldings: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var positions = JsonConvert.DeserializeObject<Position[]>(response.Content);
            return positions.Where(p => p.Amount != 0 && p.Symbol.StartsWith("t"))
                .Select(ConvertHolding)
                .ToList();
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var endpoint = GetEndpoint("auth/r/wallets");
            var request = new RestRequest(endpoint, Method.POST);

            var parameters = new JsonObject();

            request.AddJsonBody(parameters.ToString());
            SignRequest(request, endpoint, parameters);

            var response = ExecuteRestRequest(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BitfinexBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var availableWallets = JsonConvert.DeserializeObject<Wallet[]>(response.Content)
                .Where(WalletFilter(_algorithm.BrokerageModel.AccountType));

            var list = new List<CashAmount>();
            foreach (var item in availableWallets)
            {
                if (item.Balance > 0)
                {
                    list.Add(new CashAmount(item.Balance, GetLeanCurrency(item.Currency)));
                }
            }

            var balances = list.ToDictionary(x => x.Currency);

            if (_algorithm.BrokerageModel.AccountType == AccountType.Margin)
            {
                // include cash balances from currency swaps for open Crypto positions
                foreach (var holding in GetAccountHoldings().Where(x => x.Symbol.SecurityType == SecurityType.Crypto))
                {
                    var defaultQuoteCurrency = _algorithm.Portfolio.CashBook.AccountCurrency;

                    var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(
                        holding.Symbol.ID.Market,
                        holding.Symbol,
                        holding.Symbol.SecurityType,
                        defaultQuoteCurrency);

                    string baseCurrency;
                    string quoteCurrency;
                    Crypto.DecomposeCurrencyPair(holding.Symbol, symbolProperties, out baseCurrency, out quoteCurrency);

                    var baseQuantity = holding.Quantity;
                    CashAmount baseCurrencyAmount;
                    balances[baseCurrency] = balances.TryGetValue(baseCurrency, out baseCurrencyAmount)
                        ? new CashAmount(baseQuantity + baseCurrencyAmount.Amount, baseCurrency)
                        : new CashAmount(baseQuantity, baseCurrency);

                    var quoteQuantity = -holding.Quantity * holding.AveragePrice;
                    CashAmount quoteCurrencyAmount;
                    balances[quoteCurrency] = balances.TryGetValue(quoteCurrency, out quoteCurrencyAmount)
                        ? new CashAmount(quoteQuantity + quoteCurrencyAmount.Amount, quoteCurrency)
                        : new CashAmount(quoteQuantity, quoteCurrency);
                }
            }

            return balances.Values.ToList();
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(Data.HistoryRequest request)
        {
            if (request.Symbol.SecurityType != SecurityType.Crypto)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidSecurityType",
                    $"{request.Symbol.SecurityType} security type not supported, no history returned"));
                yield break;
            }

            if (request.Resolution == Resolution.Tick || request.Resolution == Resolution.Second)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidResolution",
                    $"{request.Resolution} resolution not supported, no history returned"));
                yield break;
            }

            if (request.StartTimeUtc >= request.EndTimeUtc)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidDateRange",
                    "The history request start date must precede the end date, no history returned"));
                yield break;
            }

            // if the end time cannot be rounded to resolution without a remainder
            if (request.EndTimeUtc.Ticks % request.Resolution.ToTimeSpan().Ticks > 0)
            {
                // give a warning and return
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidEndTime",
                    "The history request's end date is not a full multiple of a resolution. " +
                    "Bitfinex API only allows to support trade bar history requests. The start and end dates " +
                    "of a such request are expected to match exactly with the beginning of the first bar and ending of the last"));
                yield break;
            }

            string resolution = ConvertResolution(request.Resolution);
            long resolutionInMsec = (long)request.Resolution.ToTimeSpan().TotalMilliseconds;
            string symbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            long startMsec = (long)Time.DateTimeToUnixTimeStamp(request.StartTimeUtc) * 1000;
            long endMsec = (long)Time.DateTimeToUnixTimeStamp(request.EndTimeUtc) * 1000;
            string endpoint = $"{ApiVersion}/candles/trade:{resolution}:{symbol}/hist?limit=1000&sort=1";
            var period = request.Resolution.ToTimeSpan();

            do
            {
                var timeframe = $"&start={startMsec}&end={endMsec}";

                var restRequest = new RestRequest(endpoint + timeframe, Method.GET);
                var response = ExecuteRestRequest(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(
                        $"BitfinexBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, " +
                        $"Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                }

                // we need to drop the last bar provided by the exchange as its open time is a history request's end time
                var candles = JsonConvert.DeserializeObject<object[][]>(response.Content)
                    .Select(entries => new Candle(entries))
                    .Where(candle => candle.Timestamp != endMsec)
                    .ToList();

                // bitfinex exchange may return us an empty result - if we request data for a small time interval
                // during which no trades occurred - so it's rational to ensure 'candles' list is not empty before
                // we proceed to avoid an exception to be thrown
                if (candles.Any())
                {
                    startMsec = candles.Last().Timestamp + resolutionInMsec;
                }
                else
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
                        $"Exchange returned no data for {symbol} on history request " +
                        $"from {request.StartTimeUtc:s} to {request.EndTimeUtc:s}"));
                    yield break;
                }

                foreach (var candle in candles)
                {
                    yield return new TradeBar
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
            } while (startMsec < endMsec);
        }

        #endregion

        #region IDataQueueHandler

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            var symbol = dataConfig.Symbol;
            if (symbol.Value.Contains("UNIVERSE") ||
                !_symbolMapper.IsKnownLeanSymbol(symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            SubscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            SubscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        #endregion

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        public new void OnMessage(BrokerageMessageEvent e)
        {
            base.OnMessage(e);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _aggregator.Dispose();
            _restRateLimiter.Dispose();
        }
    }
}
