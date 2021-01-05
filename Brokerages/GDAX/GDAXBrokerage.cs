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
using System.Globalization;
using System.Linq;
using System.Net;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders.Fees;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage : BaseWebsocketsBrokerage
    {
        private const int MaxDataPointsPerHistoricalRequest = 300;

        // These are the only currencies accepted for fiat deposits
        private static readonly HashSet<string> FiatCurrencies = new List<string>
        {
            Currencies.EUR,
            Currencies.GBP,
            Currencies.USD
        }.ToHashSet();

        #region IBrokerage
        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Order order)
        {
            var req = new RestRequest("/orders", Method.POST);

            dynamic payload = new ExpandoObject();

            payload.size = Math.Abs(order.Quantity);
            payload.side = order.Direction.ToLower();
            payload.type = ConvertOrderType(order.Type);

            if (order.Type != OrderType.Market)
            {
                payload.price =
                    (order as LimitOrder)?.LimitPrice ??
                    (order as StopLimitOrder)?.LimitPrice ??
                    (order as StopMarketOrder)?.StopPrice ?? 0;
            }

            payload.product_id = _symbolMapper.GetBrokerageSymbol(order.Symbol);

            if (_algorithm.BrokerageModel.AccountType == AccountType.Margin)
            {
                payload.overdraft_enabled = true;
            }

            var orderProperties = order.Properties as GDAXOrderProperties;
            if (orderProperties != null)
            {
                if (order.Type == OrderType.Limit)
                {
                    payload.post_only = orderProperties.PostOnly;
                }
            }

            if (order.Type == OrderType.StopLimit)
            {
                payload.stop = order.Direction == OrderDirection.Buy ? "entry" : "loss";
                payload.stop_price = (order as StopLimitOrder).StopPrice;
            }

            var json = JsonConvert.SerializeObject(payload);
            Log.Trace($"GDAXBrokerage.PlaceOrder(): {json}");
            req.AddJsonBody(json);

            GetAuthenticationToken(req);
            var response = ExecuteRestRequest(req, GdaxEndpointType.Private);
            var orderFee = OrderFee.Zero;
            if (response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                var raw = JsonConvert.DeserializeObject<Messages.Order>(response.Content);

                if (raw?.Id == null)
                {
                    var errorMessage = $"Error parsing response from place order: {response.Content}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "GDAX Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (int)response.StatusCode, errorMessage));

                    return true;
                }

                if (raw.Status == "rejected")
                {
                    var errorMessage = "Reject reason: " + raw.RejectReason;
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "GDAX Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (int)response.StatusCode, errorMessage));

                    return true;
                }

                var brokerId = raw.Id;
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Add(brokerId);
                }
                else
                {
                    order.BrokerId.Add(brokerId);
                    CachedOrderIDs.TryAdd(order.Id, order);
                }

                // Add fill splits in all cases; we'll need to handle market fills too.
                FillSplit.TryAdd(order.Id, new GDAXFill(order));

                // Generate submitted event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "GDAX Order Event") { Status = OrderStatus.Submitted });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                _pendingOrders.TryAdd(brokerId, order);
                _fillMonitorResetEvent.Set();

                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Content}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "GDAX Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            return true;
        }

        /// <summary>
        /// This operation is not supported
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Order order)
        {
            throw new NotSupportedException("GDAXBrokerage.UpdateOrder: Order update not supported. Please cancel and re-create.");
        }

        /// <summary>
        /// Cancels an order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Order order)
        {
            var success = new List<bool>();

            foreach (var id in order.BrokerId)
            {
                var req = new RestRequest("/orders/" + id, Method.DELETE);
                GetAuthenticationToken(req);
                var response = ExecuteRestRequest(req, GdaxEndpointType.Private);
                success.Add(response.StatusCode == HttpStatusCode.OK);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    OnOrderEvent(new OrderEvent(order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "GDAX Order Event") { Status = OrderStatus.Canceled });

                    Order orderRemoved;
                    _pendingOrders.TryRemove(id, out orderRemoved);
                }
            }

            return success.All(a => a);
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            base.Connect();

            AccountBaseCurrency = GetAccountBaseCurrency();
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            if (!_canceller.IsCancellationRequested)
            {
                _canceller.Cancel();
            }
            WebSocket.Close();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();

            var req = new RestRequest("/orders?status=open&status=pending&status=active", Method.GET);
            GetAuthenticationToken(req);
            var response = ExecuteRestRequest(req, GdaxEndpointType.Private);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"GDAXBrokerage.GetOpenOrders: request failed: [{(int) response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var orders = JsonConvert.DeserializeObject<Messages.Order[]>(response.Content);
            foreach (var item in orders)
            {
                Order order;
                if (item.Type == "market")
                {
                    order = new MarketOrder { Price = item.Price };
                }
                else if (!string.IsNullOrEmpty(item.Stop))
                {
                    order = new StopLimitOrder { StopPrice = item.StopPrice, LimitPrice = item.Price };
                }
                else if (item.Type == "limit")
                {
                    order = new LimitOrder { LimitPrice = item.Price };
                }
                else if (item.Type == "stop")
                {
                    order = new StopMarketOrder { StopPrice = item.Price };
                }
                else
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode,
                        "GDAXBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                    continue;
                }

                order.Quantity = item.Side == "sell" ? -item.Size : item.Size;
                order.BrokerId = new List<string> { item.Id };
                order.Symbol = _symbolMapper.GetLeanSymbol(item.ProductId, SecurityType.Crypto, Market.GDAX);
                order.Time = DateTime.UtcNow;
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
            /*
             * On launching the algorithm the cash balances are pulled and stored in the cashbook.
             * There are no pre-existing currency swaps as we don't know the entire historical breakdown that brought us here.
             * Attempting to figure this out would be growing problem; every new trade would need to be processed.
             */
            return new List<Holding>();
        }

        /// <summary>
        /// Gets the total account cash balance
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var list = new List<CashAmount>();

            var request = new RestRequest("/accounts", Method.GET);
            GetAuthenticationToken(request);
            var response = ExecuteRestRequest(request, GdaxEndpointType.Private);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"GDAXBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            foreach (var item in JsonConvert.DeserializeObject<Messages.Account[]>(response.Content))
            {
                if (item.Balance > 0)
                {
                    list.Add(new CashAmount(item.Balance, item.Currency.ToUpperInvariant()));
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            // GDAX API only allows us to support history requests for TickType.Trade
            if (request.TickType != TickType.Trade)
            {
                yield break;
            }

            if (!_symbolMapper.IsKnownLeanSymbol(request.Symbol))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidSymbol",
                    $"Unknown symbol: {request.Symbol.Value}, no history returned"));
                yield break;
            }

            if (request.EndTimeUtc < request.StartTimeUtc)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidDateRange",
                    "The history request start date must precede the end date, no history returned"));
                yield break;
            }

            if (request.Resolution == Resolution.Tick || request.Resolution == Resolution.Second)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidResolution",
                    $"{request.Resolution} resolution not supported, no history returned"));
                yield break;
            }

            Log.Trace($"GDAXBrokerage.GetHistory(): Submitting request: {request.Symbol.Value}: {request.Resolution} {request.StartTimeUtc} UTC -> {request.EndTimeUtc} UTC");

            foreach (var tradeBar in GetHistoryFromCandles(request))
            {
                yield return tradeBar;
            }
        }

        /// <summary>
        /// Returns TradeBars from GDAX candles (only for Minute/Hour/Daily resolutions)
        /// </summary>
        /// <param name="request">The history request instance</param>
        private IEnumerable<TradeBar> GetHistoryFromCandles(HistoryRequest request)
        {
            var productId = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            var granularity = Convert.ToInt32(request.Resolution.ToTimeSpan().TotalSeconds);

            var startTime = request.StartTimeUtc;
            var endTime = request.EndTimeUtc;
            var maximumRange = TimeSpan.FromSeconds(MaxDataPointsPerHistoricalRequest * granularity);

            do
            {
                var maximumEndTime = startTime.Add(maximumRange);
                if (endTime > maximumEndTime)
                {
                    endTime = maximumEndTime;
                }

                var restRequest = new RestRequest($"/products/{productId}/candles?start={startTime:o}&end={endTime:o}&granularity={granularity}", Method.GET);
                var response = ExecuteRestRequest(restRequest, GdaxEndpointType.Public);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "HistoryError",
                        $"History request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}"));
                    yield break;
                }

                var bars = ParseCandleData(request.Symbol, granularity, response.Content, startTime);

                TradeBar lastPointReceived = null;
                foreach (var datapoint in bars.OrderBy(x => x.Time))
                {
                    lastPointReceived = datapoint;
                    yield return datapoint;
                }

                startTime = lastPointReceived?.EndTime ?? request.EndTimeUtc;
                endTime = request.EndTimeUtc;
            } while (startTime < request.EndTimeUtc);
        }

        /// <summary>
        /// Parse TradeBars from JSON response
        /// https://docs.pro.coinbase.com/#get-historic-rates
        /// </summary>
        private static IEnumerable<TradeBar> ParseCandleData(Symbol symbol, int granularity, string data, DateTime startTimeUtc)
        {
            if (data.Length == 0)
            {
                yield break;
            }

            var parsedData = JsonConvert.DeserializeObject<string[][]>(data);
            var period = TimeSpan.FromSeconds(granularity);

            foreach (var datapoint in parsedData)
            {
                var time = Time.UnixTimeStampToDateTime(double.Parse(datapoint[0], CultureInfo.InvariantCulture));

                if (time < startTimeUtc)
                {
                    // Note from GDAX docs:
                    // If data points are readily available, your response may contain as many as 300 candles
                    // and some of those candles may precede your declared start value.
                    yield break;
                }

                var close = datapoint[4].ToDecimal();

                yield return new TradeBar
                {
                    Symbol = symbol,
                    Time = time,
                    Period = period,
                    Open = datapoint[3].ToDecimal(),
                    High = datapoint[2].ToDecimal(),
                    Low = datapoint[1].ToDecimal(),
                    Close = close,
                    Value = close,
                    Volume = decimal.Parse(datapoint[5], NumberStyles.Float, CultureInfo.InvariantCulture)
                };
            }
        }

        #endregion

        /// <summary>
        /// Gets the account base currency
        /// </summary>
        private string GetAccountBaseCurrency()
        {
            var req = new RestRequest("/accounts", Method.GET);
            GetAuthenticationToken(req);
            var response = ExecuteRestRequest(req, GdaxEndpointType.Private);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"GDAXBrokerage.GetAccountBaseCurrency(): request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            foreach (var item in JsonConvert.DeserializeObject<Messages.Account[]>(response.Content))
            {
                if (FiatCurrencies.Contains(item.Currency))
                {
                    return item.Currency;
                }
            }

            return Currencies.USD;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _ctsFillMonitor.Cancel();
            _fillMonitorTask.Wait(TimeSpan.FromSeconds(5));

            _canceller.DisposeSafely();
            _aggregator.DisposeSafely();

            _publicEndpointRateLimiter.Dispose();
            _privateEndpointRateLimiter.Dispose();
        }
    }
}
