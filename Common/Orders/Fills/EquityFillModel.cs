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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Provides a base class for all fill models
    /// </summary>
    public class EquityFillModel : IFillModel
    {
        /// <summary>
        /// The parameters instance to be used by the different XxxxFill() implementations
        /// </summary>
        protected FillModelParameters Parameters { get; set; }

        /// <summary>
        /// This is required due to a limitation in PythonNet to resolved overriden methods.
        /// When Python calls a C# method that calls a method that's overriden in python it won't
        /// run the python implementation unless the call is performed through python too.
        /// </summary>
        protected FillModelPythonWrapper PythonWrapper;

        /// <summary>
        /// Used to set the <see cref="FillModelPythonWrapper"/> instance if any
        /// </summary>
        public void SetPythonWrapper(FillModelPythonWrapper pythonWrapper)
        {
            PythonWrapper = pythonWrapper;
        }

        /// <summary>
        /// Return an order event with the fill details
        /// </summary>
        /// <param name="parameters">A <see cref="FillModelParameters"/> object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual Fill Fill(FillModelParameters parameters)
        {
            // Important: setting the parameters is required because it is
            // consumed by the different XxxxFill() implementations
            Parameters = parameters;

            var order = parameters.Order;
            OrderEvent orderEvent;
            switch (order.Type)
            {
                case OrderType.Market:
                    orderEvent = PythonWrapper != null
                        ? PythonWrapper.MarketFill(parameters.Security, parameters.Order as MarketOrder)
                        : MarketFill(parameters.Security, parameters.Order as MarketOrder);
                    break;
                case OrderType.Limit:
                    orderEvent = PythonWrapper != null
                        ? PythonWrapper.LimitFill(parameters.Security, parameters.Order as LimitOrder)
                        : LimitFill(parameters.Security, parameters.Order as LimitOrder);
                    break;
                case OrderType.StopMarket:
                    orderEvent = PythonWrapper != null
                        ? PythonWrapper.StopMarketFill(parameters.Security, parameters.Order as StopMarketOrder)
                        : StopMarketFill(parameters.Security, parameters.Order as StopMarketOrder);
                    break;
                case OrderType.StopLimit:
                    orderEvent = PythonWrapper != null
                        ? PythonWrapper.StopLimitFill(parameters.Security, parameters.Order as StopLimitOrder)
                        : StopLimitFill(parameters.Security, parameters.Order as StopLimitOrder);
                    break;
                case OrderType.MarketOnOpen:
                    orderEvent = PythonWrapper != null
                        ? PythonWrapper.MarketOnOpenFill(parameters.Security, parameters.Order as MarketOnOpenOrder)
                        : MarketOnOpenFill(parameters.Security, parameters.Order as MarketOnOpenOrder);
                    break;
                case OrderType.MarketOnClose:
                    orderEvent = PythonWrapper != null
                        ? PythonWrapper.MarketOnCloseFill(parameters.Security, parameters.Order as MarketOnCloseOrder)
                        : MarketOnCloseFill(parameters.Security, parameters.Order as MarketOnCloseOrder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new Fill(orderEvent);
        }

        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            // Define the last bid or ask time to set stale prices message
            var endTime = DateTime.MinValue;
            fill.Status = OrderStatus.Filled;

            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //Order [fill]price for a buy market order model is the current security ask price
                    fill.FillPrice = GetAskPrice(asset, out endTime) + slip;
                    break;
                case OrderDirection.Sell:
                    //Order [fill]price for a buy market order model is the current security bid price
                    fill.FillPrice = GetBidPrice(asset, out endTime) - slip;
                    break;
            }

            var endTimeUtc = endTime.ConvertToUtc(asset.Exchange.TimeZone);

            // if the order is filled on stale (fill-forward) data, set a warning message on the order event
            if (endTimeUtc.Add(Parameters.StalePriceTimeSpan) < order.Time)
            {
                fill.Message = $"Warning: fill at stale price ({endTime.ToStringInvariant()} {asset.Exchange.TimeZone})";
            }

            // assume the order completely filled
            fill.FillQuantity = order.Quantity;

            return fill;
        }

        /// <summary>
        /// Default stop fill model implementation in base class security. (Stop Market Order Type)
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public virtual OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            //Get the range of prices in the last bar:
            var lastTradeBar = GetLastTradeBar(asset);
            var lastTradeBarEndTimeUtc = lastTradeBar.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (lastTradeBarEndTimeUtc <= order.Time) return fill;

            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Sell:
                    //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                    if (lastTradeBar.Low <= order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // Bar opens above stop price, fill at open price
                        if (lastTradeBar.Open <= order.StopPrice)
                        {
                            fill.FillPrice = lastTradeBar.Open - slip;
                        }
                        else
                        {
                            // Assuming worse case scenario fill - fill at lowest of the stop & asset price.
                            DateTime unused;
                            fill.FillPrice = Math.Min(order.StopPrice, GetBidPrice(asset, out unused) - slip);
                        }

                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;

                case OrderDirection.Buy:
                    //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                    if (lastTradeBar.High >= order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // Bar opens above stop price, fill at open price
                        if (lastTradeBar.Open >= order.StopPrice)
                        {
                            fill.FillPrice = lastTradeBar.Open + slip;
                        }
                        else
                        {
                            // Assuming worse case scenario fill - fill at highest of the stop & asset price.
                            DateTime unused;
                            fill.FillPrice = Math.Max(order.StopPrice, GetAskPrice(asset, out unused) + slip);
                        }

                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Default stop limit fill model implementation in base class security. (Stop Limit Order Type)
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <remarks>
        ///     There is no good way to model limit orders with OHLC because we never know whether the market has
        ///     gapped past our fill price. We have to make the assumption of a fluid, high volume market.
        ///
        ///     Stop limit orders we also can't be sure of the order of the H - L values for the limit fill. The assumption
        ///     was made the limit fill will be done with closing price of the bar after the stop has been triggered..
        /// </remarks>
        public virtual OrderEvent StopLimitFill(Security asset, StopLimitOrder order)
        {
            if (order.StopTriggered)
            {
                return LimitFillImpl(asset, order, order.LimitPrice);
            }

            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(
                asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            //Get the range of prices in the last bar:
            var lastTradeBar = GetLastTradeBar(asset);
            var lastTradeBarEndTimeUtc = lastTradeBar.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (lastTradeBarEndTimeUtc <= order.Time) return fill;

            //Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                    if (lastTradeBar.High >= order.StopPrice || order.StopTriggered)
                    {
                        order.StopTriggered = true;
                        DateTime unused;

                        // Fill the limit order, using closing price of bar:
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (GetAskPrice(asset, out unused) <= order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Min(lastTradeBar.High, order.LimitPrice);
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;

                case OrderDirection.Sell:
                    //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                    if (lastTradeBar.Low <= order.StopPrice || order.StopTriggered)
                    {
                        order.StopTriggered = true;
                        DateTime unused;

                        // Fill the limit order, using minimum price of the bar
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (GetBidPrice(asset, out unused) >= order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Max(lastTradeBar.Low, order.LimitPrice);
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Default limit order fill model in the base security class.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public virtual OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            return LimitFillImpl(asset, order, order.LimitPrice);
        }

        /// <summary>
        /// Default limit order fill model in the base security class.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <param name="limitPrice">Limit price for this order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        private OrderEvent LimitFillImpl(Security asset, Order order, decimal limitPrice)
        {
            //Initialise;
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }
            //Get the range of prices in the last bar:
            var lastQuoteBar = GetLastQuoteBar(asset);
            var lastQuoteBarEndTimeUtc = lastQuoteBar.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (lastQuoteBarEndTimeUtc <= order.Time) return fill;

            //-> Valid Live/Model Order:
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //Buy limit seeks lowest price
                    if (lastQuoteBar.Ask.Low <= limitPrice)
                    {
                        //Set order fill:
                        fill.Status = OrderStatus.Filled;
                        // fill at the worse price this bar or the limit price, this allows far out of the money limits
                        // to be executed properly
                        fill.FillPrice = Math.Min(lastQuoteBar.Ask.High, limitPrice);
                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;
                case OrderDirection.Sell:
                    //Sell limit seeks highest price possible
                    if (lastQuoteBar.Bid.High >= limitPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // fill at the worse price this bar or the limit price, this allows far out of the money limits
                        // to be executed properly
                        fill.FillPrice = Math.Max(lastQuoteBar.Bid.Low, limitPrice);
                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Market on Open Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
        {
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            // MOO should never fill on the same bar or on stale data
            // Imagine the case where we have a thinly traded equity, ASUR, and another liquid
            // equity, say SPY, SPY gets data every minute but ASUR, if not on fill forward, maybe
            // have large gaps, in which case the currentBar.EndTime will be in the past
            // ASUR  | | |      [order]        | | | | | | |
            //  SPY  | | | | | | | | | | | | | | | | | | | |
            var lastTradeBar = GetLastTradeBar(asset);
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            if (localOrderTime >= lastTradeBar.EndTime) return fill;

            // if the MOO was submitted during market the previous day, wait for a day to turn over
            if (asset.Exchange.DateTimeIsOpen(localOrderTime) && localOrderTime.Date == asset.LocalTime.Date)
            {
                return fill;
            }

            // wait until market open
            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            fill.FillPrice = lastTradeBar.Open;
            fill.Status = OrderStatus.Filled;
            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    fill.FillPrice += slip;
                    // assume the order completely filled
                    fill.FillQuantity = order.Quantity;
                    break;
                case OrderDirection.Sell:
                    fill.FillPrice -= slip;
                    // assume the order completely filled
                    fill.FillQuantity = order.Quantity;
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Market on Close Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
        {
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            var nextMarketClose = asset.Exchange.Hours.GetNextMarketClose(localOrderTime, false);

            // wait until market closes after the order time
            if (asset.LocalTime < nextMarketClose)
            {
                return fill;
            }
            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            fill.FillPrice = GetLastTradeBar(asset).Close;
            fill.Status = OrderStatus.Filled;
            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    fill.FillPrice += slip;
                    // assume the order completely filled
                    fill.FillQuantity = order.Quantity;
                    break;
                case OrderDirection.Sell:
                    fill.FillPrice -= slip;
                    // assume the order completely filled
                    fill.FillQuantity = order.Quantity;
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Determines if the exchange is open using the current time of the asset
        /// </summary>
        protected static bool IsExchangeOpen(Security asset, bool isExtendedMarketHours)
        {
            if (!asset.Exchange.DateTimeIsOpen(asset.LocalTime))
            {
                // if we're not open at the current time exactly, check the bar size, this handle large sized bars (hours/days)
                var currentBar = asset.GetLastData();
                if (asset.LocalTime.Date != currentBar.EndTime.Date
                    || !asset.Exchange.IsOpenDuringBar(currentBar.Time, currentBar.EndTime, isExtendedMarketHours))
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Get data types the Security is subscribed to
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private HashSet<Type> GetSubscribedTypes(Security asset)
        {
            var subscribedTypes = Parameters
                .ConfigProvider
                .GetSubscriptionDataConfigs(asset.Symbol)
                .ToHashSet(x => x.Type);

            if (subscribedTypes.Count == 0)
            {
                throw new InvalidOperationException($"Cannot perform fill for {asset.Symbol} because no data subscription were found.");
            }

            return subscribedTypes;
        }

        /// <summary>
        /// Get current ask price for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="endTime">Timestamp of the most recent data type</param>
        private decimal GetAskPrice(Security asset, out DateTime endTime)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            List<Tick> ticks = null;
            var isTickSubscribed = subscribedTypes.Contains(typeof(Tick));

            if (isTickSubscribed)
            {
                ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice != 0);
                if (quote != null)
                {
                    endTime = quote.EndTime;
                    return quote.AskPrice;
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null)
                {
                    endTime = quoteBar.EndTime;
                    return quoteBar.Ask?.Close ?? quoteBar.Close;
                }
            }

            if (isTickSubscribed)
            {
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    endTime = trade.EndTime;
                    return trade.Price;
                }
            }

            if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null)
                {
                    endTime = tradeBar.EndTime;
                    return tradeBar.Close;
                }
            }

            endTime = asset.LocalTime;
            return asset.AskPrice;
        }

        /// <summary>
        /// Get current bid price for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="endTime">Timestamp of the most recent data type</param>
        private decimal GetBidPrice(Security asset, out DateTime endTime)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            List<Tick> ticks = null;
            var isTickSubscribed = subscribedTypes.Contains(typeof(Tick));

            if (isTickSubscribed)
            {
                ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.BidPrice != 0);
                if (quote != null)
                {
                    endTime = quote.EndTime;
                    return quote.BidPrice;
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null)
                {
                    endTime = quoteBar.EndTime;
                    return quoteBar.Bid?.Close ?? quoteBar.Close;
                }
            }

            if (isTickSubscribed)
            {
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    endTime = trade.EndTime;
                    return trade.Price;
                }
            }

            if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null)
                {
                    endTime = tradeBar.EndTime;
                    return tradeBar.Close;
                }
            }

            endTime = asset.LocalTime;
            return asset.BidPrice;
        }

        /// <summary>
        /// Get current trade bar for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private TradeBar GetLastTradeBar(Security asset)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var ticks = asset.Cache.GetAll<Tick>().ToList();

                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    return new TradeBar(trade.Time, trade.Symbol, trade.Price, trade.Price, trade.Price, trade.Price, trade.Quantity, TimeSpan.Zero);
                }

                DateTime quoteTime;
                decimal askPrice;
                decimal askSize;
                decimal bidPrice;
                decimal bidSize;

                if (TryGetLastQuoteTick(asset, ticks, out quoteTime, out bidPrice, out bidSize, out askPrice, out askSize))
                {
                    var price = (askPrice + bidPrice) / 2;
                    return new TradeBar(quoteTime, asset.Symbol, price, price, price, price, 0, TimeSpan.Zero);
                }
            }

            if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null)
                {
                    return tradeBar;
                }
            }

            // Use any data available
            var lastData = asset.GetLastData();
            var time = lastData?.Time ?? asset.LocalTime;
            var period = lastData == null ? TimeSpan.Zero : lastData.EndTime - time;

            return new TradeBar(time, asset.Symbol, asset.Open, asset.High, asset.Low, asset.Close, asset.Volume, period);
        }

        /// <summary>
        /// Get current quote bar for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private QuoteBar GetLastQuoteBar(Security asset)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var ticks = asset.Cache.GetAll<Tick>().ToList();

                DateTime quoteTime;
                decimal askPrice;
                decimal askSize;
                decimal bidPrice;
                decimal bidSize;

                if (TryGetLastQuoteTick(asset, ticks, out quoteTime, out bidPrice, out bidSize, out askPrice, out askSize))
                {
                    return new QuoteBar(
                        quoteTime,
                        asset.Symbol,
                        new Bar(bidPrice, bidPrice, bidPrice, bidPrice),
                        bidSize,
                        new Bar(askPrice, askPrice, askPrice, askPrice),
                        askSize,
                        TimeSpan.Zero
                    );
                }

                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    var bar = new Bar(trade.Price, trade.Price, trade.Price, trade.Price);
                    return new QuoteBar(trade.Time, trade.Symbol, bar, 0, bar, 0, TimeSpan.Zero);
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null)
                {
                    return quoteBar;
                }
            }

            // Use any data available
            var lastData = asset.GetLastData();
            var time = lastData?.Time ?? asset.LocalTime;
            var period = lastData == null ? TimeSpan.Zero : lastData.EndTime - time;

            return new QuoteBar(
                time,
                asset.Symbol,
                new Bar(asset.Open, asset.High, asset.Low, asset.BidPrice),
                asset.BidSize,
                new Bar(asset.Open, asset.High, asset.Low, asset.AskPrice),
                asset.AskSize,
                period
            );
        }

        /// <summary>
        /// Get the last quote information
        /// </summary>
        /// <param name="asset">Security which has subscribed to tick data</param>
        /// <param name="ticks">Current collection of Tick for the asset</param>
        /// <param name="time">DateTime of the last bid or ask</param>
        /// <param name="bidPrice">Last bid price available</param>
        /// <param name="bidSize">Last bid size. Zero if there is no bid information in the current collection of Tick</param>
        /// <param name="askPrice">Last ask price available</param>
        /// <param name="askSize">Last ask size. Zero if there is no ask information in the current collection of Tick</param>
        /// <returns></returns>
        private bool TryGetLastQuoteTick(Security asset, List<Tick> ticks, out DateTime time, out decimal bidPrice, out decimal bidSize, out decimal askPrice, out decimal askSize)
        {
            time = DateTime.MinValue;
            bidPrice = asset.BidPrice;
            bidSize = 0;
            askPrice = asset.AskPrice;
            askSize = 0;

            var quoteAsk = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice != 0);
            if (quoteAsk != null)
            {
                askPrice = quoteAsk.AskPrice;
                askSize = quoteAsk.AskSize;
                time = quoteAsk.Time;
            }

            var quoteBid = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.BidPrice != 0);
            if (quoteBid != null)
            {
                bidPrice = quoteBid.BidPrice;
                bidSize = quoteBid.BidSize;
                if (quoteBid.Time > time)
                {
                    time = quoteBid.Time;
                }
            }

            return quoteAsk != null || quoteBid != null;
        }
    }
}