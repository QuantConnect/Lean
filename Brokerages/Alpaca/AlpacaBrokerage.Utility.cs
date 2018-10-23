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

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Alpaca Brokerage utility methods
    /// </summary>
    public partial class AlpacaBrokerage
    {
        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        private Dictionary<string, Tick> GetRates(List<string> instruments)
        {
            try
            {
                CheckRateLimiting();
                var task = _restClient.ListQuotesAsync(instruments);
                var response = task.SynchronouslyAwaitTaskResult();
                return response
                    .ToDictionary(
                        x => x.Symbol,
                        x => new Tick { Symbol = Symbol.Create(x.Symbol, SecurityType.Equity, Market.USA), BidPrice = x.BidPrice, AskPrice = x.AskPrice, Time = x.LastTime, TickType = TickType.Trade }
                    );
            }
            catch (Exception e)
            {
                Log.Error(e);
                if (e.InnerException != null)
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                throw;
            }
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
            var task = _restClient.PostOrderAsync(order.Symbol.Value, quantity, side, type, timeInForce,
                limitPrice, stopPrice);

            var apOrder = task.SynchronouslyAwaitTaskResult();

            return apOrder;
        }

        /// <summary>
        /// Event handler for streaming events
        /// </summary>
        /// <param name="trade">The event object</param>
        private void OnTradeUpdate(ITradeUpdate trade)
        {
            Log.Trace("OnTransactionData: {0} {1} {2}", trade.Event, trade.Order.OrderId, trade.Order.OrderStatus);

            Order order;
            string tradeEvent = trade.Event.ToUpper();
            lock (_locker)
            {
                order = _orderProvider.GetOrderByBrokerageId(trade.Order.OrderId.ToString());
            }
            if (order != null)
            {
                if (tradeEvent == "FILL" || tradeEvent == "PARTIAL_FILL")
                {
                    order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

                    var status = Orders.OrderStatus.Filled;
                    if (trade.Order.FilledQuantity < trade.Order.Quantity) status = Orders.OrderStatus.PartiallyFilled;
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Fill Event")
                    {
                        Status = status,
                        FillPrice = trade.Price.Value,
                        FillQuantity = Convert.ToInt32(trade.Order.Quantity)
                    });
                }
                else if (tradeEvent == "CANCELED")
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Cancel Order Event") { Status = Orders.OrderStatus.Canceled });
                }
                else if (tradeEvent == "ORDER_CANCEL_REJECTED")
                {
                    Log.Trace($"AlpacaBrokerage.OnTradeUpdate(): Order cancel rejected.");
                }
            }
            else
            {
                Log.Error($"AlpacaBrokerage.OnTradeUpdate(): order id not found: {trade.Order.OrderId}");
            }
        }

        private void OnNatsClientError(string error)
        {
            Log.Error($"NatsClient error: {error}");
        }

        private void OnSockClientError(Exception exception)
        {
            Log.Error(exception, "SockClient error");
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
            // This is due to the polygon API logic.
            // If start and end date is equal, then the result is null
            var endTimeUtcForAPI = endTimeUtc.Add(TimeSpan.FromDays(1));

            var period = resolution.ToTimeSpan();

            // No seconds resolution
            if (period.Seconds < 60)
            {
                yield return null;
            }

            DateTime startTime = startTimeUtc;
            DateTime startTimeWithTZ = startTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            DateTime endTimeWithTZ = endTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);

            TradeBar currentBar = new TradeBar();
            while (true)
            {
                List<Markets.IBar> newBars = new List<Markets.IBar>();
                try
                {
                    CheckRateLimiting();
                    var task = (period.Days < 1) ? _restClient.ListMinuteAggregatesAsync(symbol.Value, startTime, endTimeUtcForAPI) : _restClient.ListDayAggregatesAsync(symbol.Value, startTime, endTimeUtc);
                    newBars = task.SynchronouslyAwaitTaskResult().Items.ToList();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    if (e.InnerException != null)
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                    throw;
                }

                if (newBars.Count == 0)
                {
                    if (currentBar.Symbol != Symbol.Empty) yield return currentBar;
                    break;
                }
                if (startTime == newBars.Last().Time)
                {
                    yield return currentBar;
                    break;
                }

                startTime = newBars.Last().Time;

                var result = newBars
                        .GroupBy(x => x.Time.RoundDown(period))
                        .Select(x => new TradeBar(
                            x.Key.ConvertFromUtc(requestedTimeZone),
                            symbol,
                            x.First().Open,
                            x.Max(t => t.High),
                            x.Min(t => t.Low),
                            x.Last().Close,
                            0,
                            period
                            ))
                         .ToList();
                if (currentBar.Symbol == Symbol.Empty) currentBar = result[0];
                if (currentBar.Time == result[0].Time)
                {
                    // Update the last QuoteBar
                    var newBar = result[0];
                    currentBar.High = currentBar.High > newBar.High ? currentBar.High : newBar.High;
                    currentBar.Low = currentBar.Low < newBar.Low ? currentBar.Low : newBar.Low;
                    currentBar.Close = newBar.Close;
                    result[0] = currentBar;
                }
                else
                {
                    result.Insert(0, currentBar);
                }
                if (result.Count == 1 && result[0].Time == currentBar.Time) continue;
                bool isEnd = false;
                for (int i = 0; i < result.Count - 1; i++)
                {
                    if (result[i].Time < startTimeWithTZ) continue;
                    if (result[i].Time > endTimeWithTZ)
                    {
                        isEnd = true;
                        break;
                    }
                    yield return result[i];
                }
                currentBar = result[result.Count - 1];

                if (isEnd) break;
                if (currentBar.Time == endTimeWithTZ)
                {
                    yield return currentBar;
                    break;
                }
            }
        }

        /// <summary>
        /// Downloads a list of QuoteBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        private IEnumerable<QuoteBar> DownloadQuoteBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            var period = resolution.ToTimeSpan();

            DateTime startTime = startTimeUtc;
            DateTime startTimeWithTZ = startTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            DateTime endTimeWithTZ = endTimeUtc.ConvertFromUtc(requestedTimeZone).RoundDown(period);
            long offsets = 0;

            QuoteBar currentBar = new QuoteBar();
            while (true)
            {

                List<IHistoricalQuote> asList = new List<IHistoricalQuote>();
                try
                {
                    CheckRateLimiting();
                    var task = _restClient.ListHistoricalQuotesAsync(symbol.Value, startTime, offsets);
                    var newBars = task.SynchronouslyAwaitTaskResult();
                    asList = newBars.Items.ToList();

                    if (asList.Count == 0)
                    {
                        startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day);
                        startTime = startTime.AddDays(1);
                        if (startTime > endTimeUtc) break;
                        offsets = 0;
                        continue;
                    }

                    // The first item in the HistoricalQuote is always 0 on BidPrice, so ignore it.
                    asList.RemoveAt(0);
                    if (asList.Count == 0) break;

                    offsets = asList.Last().TimeOffset;
                    if (DateTimeHelper.FromUnixTimeMilliseconds(offsets) < startTimeUtc) continue;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    if (e.InnerException != null)
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                    throw;
                }
                var result = asList
                        .GroupBy(x => DateTimeHelper.FromUnixTimeMilliseconds(x.TimeOffset).RoundDown(period))
                        .Select(x => new QuoteBar(
                            x.Key.ConvertFromUtc(requestedTimeZone),
                            symbol,
                            new Bar(
                                x.First().BidPrice,
                                x.Max(t => t.BidPrice),
                                x.Min(t => t.BidPrice),
                                x.Last().BidPrice
                            ),
                            x.Last().BidSize,
                            new Bar(
                                x.First().AskPrice,
                                x.Max(t => t.AskPrice),
                                x.Min(t => t.AskPrice),
                                x.Last().AskPrice
                            ),
                            x.Last().AskPrice,
                            period
                            ))
                         .ToList();
                if (currentBar.Symbol == Symbol.Empty) currentBar = result[0];
                if (currentBar.Time == result[0].Time)
                {
                    // Update the last QuoteBar
                    var newBar = result[0];
                    currentBar.Bid.High = currentBar.Bid.High > newBar.Bid.High ? currentBar.Bid.High : newBar.Bid.High;
                    currentBar.Bid.Low = currentBar.Bid.Low < newBar.Bid.Low ? currentBar.Bid.Low : newBar.Bid.Low;
                    currentBar.Bid.Close = newBar.Bid.Close;

                    currentBar.Ask.High = currentBar.Ask.High > newBar.Ask.High ? currentBar.Ask.High : newBar.Ask.High;
                    currentBar.Ask.Low = currentBar.Ask.Low < newBar.Ask.Low ? currentBar.Ask.Low : newBar.Ask.Low;
                    currentBar.Ask.Close = newBar.Ask.Close;
                    result[0] = currentBar;
                }
                else
                {
                    result.Insert(0, currentBar);
                }
                if (result.Count == 1 && result[0].Time == currentBar.Time) continue;
                bool isEnd = false;
                for (int i = 0; i < result.Count - 1; i++)
                {
                    if (startTimeWithTZ > result[i].Time) continue;
                    if (endTimeWithTZ < result[i].Time)
                    {
                        isEnd = true;
                        break;
                    }
                    yield return result[i];
                }
                currentBar = result[result.Count - 1];

                if (isEnd) break;
                if (currentBar.Time == endTimeWithTZ)
                {
                    yield return currentBar;
                    break;
                }
            }
        }

        /// <summary>
        /// Downloads a list of Ticks for the requested period
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of ticks</returns>
        private IEnumerable<Tick> DownloadTicks(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, DateTimeZone requestedTimeZone)
        {
            DateTime startTime = startTimeUtc;
            DateTime startTimeWithTZ = startTimeUtc.ConvertFromUtc(requestedTimeZone);
            DateTime endTimeWithTZ = endTimeUtc.ConvertFromUtc(requestedTimeZone);
            long offsets = 0;

            Tick currentTick = new Tick();
            while (true)
            {
                List<IHistoricalQuote> asList = new List<IHistoricalQuote>();
                try
                {
                    CheckRateLimiting();
                    var task = _restClient.ListHistoricalQuotesAsync(symbol.Value, startTime, offsets);
                    var newBars = task.SynchronouslyAwaitTaskResult();
                    asList = newBars.Items.ToList();
                    if (asList.Count == 0)
                    {
                        startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day);
                        startTime = startTime.AddDays(1);
                        if (startTime > endTimeUtc) break;
                        offsets = 0;
                    }
                    else
                    {
                        // The first item in the HistoricalQuote is always 0 on BidPrice, so ignore it.
                        asList.RemoveAt(0);

                        offsets = asList.Last().TimeOffset;
                        if (DateTimeHelper.FromUnixTimeMilliseconds(offsets) < startTimeUtc) continue;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    if (e.InnerException != null)
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                    throw;
                }
                bool isEnd = false;
                for (int i = 0; i < asList.Count; i++)
                {
                    var currentTime = DateTimeHelper.FromUnixTimeMilliseconds(asList[i].TimeOffset).ConvertFromUtc(requestedTimeZone);
                    if (startTimeWithTZ > currentTime) continue;
                    if (endTimeWithTZ < currentTime)
                    {
                        isEnd = true;
                        break;
                    }
                    currentTick.Time = currentTime;
                    currentTick.Symbol = symbol;
                    currentTick.BidPrice = asList[i].BidPrice;
                    currentTick.AskPrice = asList[i].AskPrice;
                    yield return currentTick;
                }
                asList.Clear();
                if (isEnd) break;
            }
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

            qcOrder.Symbol = Symbol.Create(instrument, SecurityType.Equity, Market.USA);
            qcOrder.Time = order.SubmittedAt.Value;
            qcOrder.Quantity = order.Quantity;
            qcOrder.Status = Orders.OrderStatus.None;
            qcOrder.BrokerId.Add(id);

            var orderByBrokerageId = _orderProvider.GetOrderByBrokerageId(id);
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
            var securityType = SecurityType.Equity;
            var symbol = Symbol.Create(position.Symbol, securityType, Market.USA);

            return new Holding
            {
                Symbol = symbol,
                Type = securityType,
                AveragePrice = position.AverageEntryPrice,
                ConversionRate = 1.0m,
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
