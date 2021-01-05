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

using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Brokerages.Alpaca.Markets;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using OrderStatus = QuantConnect.Orders.OrderStatus;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Alpaca Brokerage utility methods
    /// </summary>
    public partial class AlpacaBrokerage
    {
        /// <summary>
        /// Retrieves the current quotes for an instrument
        /// </summary>
        /// <param name="instrument">the instrument to check</param>
        /// <returns>Returns a Tick object with the current bid/ask prices for the instrument</returns>
        public Tick GetRates(string instrument)
        {
            CheckRateLimiting();

            var task = _polygonDataClient.GetLastQuoteAsync(instrument);
            var response = task.SynchronouslyAwaitTaskResult();

            return new Tick
            {
                Symbol = _symbolMapper.GetLeanSymbol(response.Symbol, SecurityType.Equity, Market.USA),
                BidPrice = response.BidPrice,
                AskPrice = response.AskPrice,
                Time = response.Time,
                TickType = TickType.Quote
            };
        }

        private IOrder GenerateAndPlaceOrder(Order order)
        {
            var quantity = (long)order.Quantity;
            var side = order.Quantity > 0 ? OrderSide.Buy : OrderSide.Sell;
            if (order.Quantity < 0) quantity = -quantity;
            Markets.OrderType type;
            decimal? limitPrice = null;
            decimal? stopPrice = null;
            var timeInForce = Markets.TimeInForce.Gtc;

            switch (order.Type)
            {
                case Orders.OrderType.Market:
                    type = Markets.OrderType.Market;
                    break;

                case Orders.OrderType.Limit:
                    type = Markets.OrderType.Limit;
                    limitPrice = ((LimitOrder)order).LimitPrice;
                    break;

                case Orders.OrderType.StopMarket:
                    type = Markets.OrderType.Stop;
                    stopPrice = ((StopMarketOrder)order).StopPrice;
                    break;

                case Orders.OrderType.StopLimit:
                    type = Markets.OrderType.StopLimit;
                    stopPrice = ((StopLimitOrder)order).StopPrice;
                    limitPrice = ((StopLimitOrder)order).LimitPrice;
                    break;

                case Orders.OrderType.MarketOnOpen:
                    type = Markets.OrderType.Market;
                    timeInForce = Markets.TimeInForce.Opg;
                    break;

                default:
                    throw new NotSupportedException("The order type " + order.Type + " is not supported.");
            }

            CheckRateLimiting();
            var task = _alpacaTradingClient.PostOrderAsync(new NewOrderRequest(_symbolMapper.GetBrokerageSymbol(order.Symbol), quantity, side, type, timeInForce)
            {
                LimitPrice = limitPrice,
                StopPrice = stopPrice
            });

            var apOrder = task.SynchronouslyAwaitTaskResult();

            return apOrder;
        }

        /// <summary>
        /// Event handler for streaming events
        /// </summary>
        /// <param name="trade">The event object</param>
        private void OnTradeUpdate(ITradeUpdate trade)
        {
            Log.Trace($"AlpacaBrokerage.OnTradeUpdate(): Event:{trade.Event} OrderId:{trade.Order.OrderId} Symbol:{trade.Order.Symbol} OrderStatus:{trade.Order.OrderStatus} FillQuantity:{trade.Order.FilledQuantity} FillPrice:{trade.Price} Quantity:{trade.Order.Quantity} LimitPrice:{trade.Order.LimitPrice} StopPrice:{trade.Order.StopPrice}");

            Order order;
            OrderTicket ticket = null;
            lock (_locker)
            {
                order = _orderProvider.GetOrderByBrokerageId(trade.Order.OrderId.ToString());
                if (order != null)
                {
                    ticket = _orderProvider.GetOrderTicket(order.Id);
                }
            }

            if (order != null && ticket != null)
            {
                if (trade.Event == TradeEvent.Fill || trade.Event == TradeEvent.PartialFill)
                {
                    order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

                    var status = trade.Event == TradeEvent.Fill ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

                    // The Alpaca API does not return the individual quantity for each partial fill, but the cumulative filled quantity
                    var fillQuantity = trade.Order.FilledQuantity - Math.Abs(ticket.QuantityFilled);

                    OnOrderEvent(new OrderEvent(order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Alpaca Fill Event")
                    {
                        Status = status,
                        FillPrice = trade.Price.Value,
                        FillQuantity = fillQuantity * (order.Direction == OrderDirection.Buy ? 1 : -1)
                    });
                }
                else if (trade.Event == TradeEvent.Rejected)
                {
                    OnOrderEvent(new OrderEvent(order,
                            DateTime.UtcNow,
                            OrderFee.Zero,
                            "Alpaca Rejected Order Event") { Status = OrderStatus.Invalid });
                }
                else if (trade.Event == TradeEvent.Canceled || trade.Event == TradeEvent.Expired)
                {
                    OnOrderEvent(new OrderEvent(order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        $"Alpaca {trade.Event} Order Event") { Status = OrderStatus.Canceled });
                }
                else if (trade.Event == TradeEvent.OrderCancelRejected)
                {
                    var message = $"Order cancellation rejected: OrderId: {order.Id}";
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
                }
            }
            else
            {
                Log.Error($"AlpacaBrokerage.OnTradeUpdate(): order id not found: {trade.Order.OrderId}");
            }
        }

        private static void OnSockClientError(Exception exception)
        {
            Log.Error($"SockClient error: {exception.Message}");
        }

        /// <summary>
        /// Downloads a list of TradeBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        private IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            // Only equities supported
            if (symbol.SecurityType != SecurityType.Equity)
            {
                yield break;
            }

            // Only minute/hour/daily resolutions supported
            if (resolution < Resolution.Minute)
            {
                yield break;
            }

            var period = resolution.ToTimeSpan();

            var startTime = startTimeUtc.RoundDown(period);
            var endTime = endTimeUtc.RoundDown(period).Add(period);

            while (startTime < endTime)
            {
                CheckRateLimiting();

                var task = _polygonDataClient.ListAggregatesAsync(
                    new AggregatesRequest(
                        _symbolMapper.GetBrokerageSymbol(symbol),
                        new AggregationPeriod(
                            1,
                            resolution == Resolution.Daily ? AggregationPeriodUnit.Day : AggregationPeriodUnit.Minute
                        )
                    ).SetInclusiveTimeInterval(startTime, endTime));

                var time = startTime;
                var items = task.SynchronouslyAwaitTaskResult()
                    .Items
                    .Where(x => x.Time >= time)
                    .ToList();

                if (!items.Any())
                {
                    break;
                }

                if (resolution == Resolution.Hour)
                {
                    // aggregate minute tradebars into hourly tradebars
                    var bars = items
                        .GroupBy(x => x.Time.RoundDown(period))
                        .Select(
                            x => new TradeBar(
                                x.Key.ConvertFromUtc(requestedTimeZone),
                                symbol,
                                x.First().Open,
                                x.Max(t => t.High),
                                x.Min(t => t.Low),
                                x.Last().Close,
                                x.Sum(t => t.Volume),
                                period
                            ));

                    foreach (var bar in bars)
                    {
                        yield return bar;
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        // we do not convert time zones for daily bars here because the API endpoint
                        // for historical daily bars returns only dates instead of timestamps
                        yield return new TradeBar(
                            resolution == Resolution.Daily
                                ? item.Time
                                : item.Time.ConvertFromUtc(requestedTimeZone),
                            symbol,
                            item.Open,
                            item.High,
                            item.Low,
                            item.Close,
                            item.Volume,
                            period);
                    }
                }

                startTime = items.Last().Time.Add(period);
            }
        }

        /// <summary>
        /// Downloads a list of Trade ticks
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of ticks</returns>
        private IEnumerable<Tick> DownloadTradeTicks(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, DateTimeZone requestedTimeZone)
        {
            // The Polygon API only accepts nanosecond level resolution for the expected epoch time.
            // It is also an inclusive time, so we must increment this by one in order to get the
            // expected results when paginating.
            var previousTimestamp = (long?)(DateTimeHelper.GetUnixTimeMilliseconds(startTimeUtc) * 1000000);

            while (startTimeUtc < endTimeUtc)
            {
                CheckRateLimiting();

                var dateUtc = startTimeUtc.Date;
                var date = startTimeUtc.ConvertFromUtc(requestedTimeZone).Date;
                var task = _polygonDataClient.ListHistoricalTradesAsync(new HistoricalRequest(_symbolMapper.GetBrokerageSymbol(symbol), date)
                {
                    Timestamp = previousTimestamp
                });

                var rawItems = task.SynchronouslyAwaitTaskResult().Items;
                var items = rawItems.Where(x => x.Timestamp >= startTimeUtc && x.Timestamp <= endTimeUtc);

                foreach (var item in items)
                {
                    yield return new Tick
                    {
                        TickType = TickType.Trade,
                        Time = item.Timestamp.ConvertFromUtc(requestedTimeZone),
                        Symbol = symbol,
                        Value = item.Price,
                        Quantity = item.Size
                    };
                }

                // Cache the timestamp we're planning on using so we don't null check twice.
                var nextTime = rawItems.LastOrDefault()?.Timestamp ?? dateUtc.AddDays(1);

                // Timestamp of items are in UTC, and so should the date we're incrementing.
                startTimeUtc = nextTime;
                // Convert milliseconds to nanoseconds and add one nanosecond to the time (timestamp is inclusive)
                previousTimestamp = (DateTimeHelper.GetUnixTimeMilliseconds(nextTime) * 1000000) + 1;
            }
        }

        /// <summary>
        /// Aggregates a list of trade ticks into tradebars
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="ticks">The IEnumerable of ticks</param>
        /// <param name="period">The time span for the resolution</param>
        /// <returns></returns>
        internal static IEnumerable<TradeBar> AggregateTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan period)
        {
            return
                from t in ticks
                group t by t.Time.RoundDown(period)
                into g
                select new TradeBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Open = g.First().LastPrice,
                    High = g.Max(t => t.LastPrice),
                    Low = g.Min(t => t.LastPrice),
                    Close = g.Last().LastPrice,
                    Volume = g.Sum(t => t.Quantity),
                    Period = period
                };
        }

        /// <summary>
        /// Converts an Alpaca order into a LEAN order.
        /// </summary>
        private Order ConvertOrder(IOrder order)
        {
            var type = order.OrderType;

            Order qcOrder;
            switch (type)
            {
                case Markets.OrderType.Stop:
                    qcOrder = new StopMarketOrder
                    {
                        StopPrice = order.StopPrice.Value
                    };
                    break;

                case Markets.OrderType.Limit:
                    qcOrder = new LimitOrder
                    {
                        LimitPrice = order.LimitPrice.Value
                    };
                    break;

                case Markets.OrderType.StopLimit:
                    qcOrder = new StopLimitOrder
                    {
                        Price = order.StopPrice.Value,
                        LimitPrice = order.LimitPrice.Value
                    };
                    break;

                case Markets.OrderType.Market:
                    qcOrder = new MarketOrder();
                    break;

                default:
                    throw new NotSupportedException(
                        "An existing " + type + " working order was found and is currently unsupported. Please manually cancel the order before restarting the algorithm.");
            }

            var instrument = order.Symbol;
            var id = order.OrderId.ToString();

            qcOrder.Symbol = _symbolMapper.GetLeanSymbol(instrument, SecurityType.Equity, Market.USA);

            if (order.SubmittedAt != null)
            {
                qcOrder.Time = order.SubmittedAt.Value;
            }

            qcOrder.Quantity = order.OrderSide == OrderSide.Buy ? order.Quantity : -order.Quantity;
            qcOrder.Status = OrderStatus.None;
            qcOrder.BrokerId.Add(id);

            Order orderByBrokerageId;
            lock (_locker)
            {
                orderByBrokerageId = _orderProvider.GetOrderByBrokerageId(id);
            }

            if (orderByBrokerageId != null)
            {
                qcOrder.Id = orderByBrokerageId.Id;
            }

            if (order.ExpiredAt != null)
            {
                qcOrder.Properties.TimeInForce = Orders.TimeInForce.GoodTilDate(order.ExpiredAt.Value);
            }

            return qcOrder;
        }

        /// <summary>
        /// Converts an Alpaca position into a LEAN holding.
        /// </summary>
        private Holding ConvertHolding(IPosition position)
        {
            return new Holding
            {
                Symbol = _symbolMapper.GetLeanSymbol(position.Symbol, SecurityType.Equity, Market.USA),
                Type = SecurityType.Equity,
                AveragePrice = position.AverageEntryPrice,
                MarketPrice = position.AssetCurrentPrice,
                MarketValue = position.MarketValue,
                CurrencySymbol = "$",
                Quantity = position.Quantity
            };
        }

        private void CheckRateLimiting()
        {
            if (!_messagingRateLimiter.WaitToProceed(TimeSpan.Zero))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "RateLimit",
                    "The API request has been rate limited. To avoid this message, please reduce the frequency of API calls."));

                _messagingRateLimiter.WaitToProceed();
            }
        }
    }
}
