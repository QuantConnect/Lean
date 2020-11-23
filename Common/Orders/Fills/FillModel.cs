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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Provides a base class for all fill models
    /// </summary>
    public class FillModel : IFillModel
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

            var prices = GetPricesCheckingPythonWrapper(asset, order.Direction);
            var pricesEndTimeUtc = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // if the order is filled on stale (fill-forward) data, set a warning message on the order event
            if (pricesEndTimeUtc.Add(Parameters.StalePriceTimeSpan) < order.Time)
            {
                fill.Message = $"Warning: fill at stale price ({prices.EndTime.ToStringInvariant()} {asset.Exchange.TimeZone})";
            }

            //Order [fill]price for a market order model is the current security price
            fill.FillPrice = prices.Current;
            fill.Status = OrderStatus.Filled;

            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    fill.FillPrice += slip;
                    break;
                case OrderDirection.Sell:
                    fill.FillPrice -= slip;
                    break;
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
            var prices = GetPricesCheckingPythonWrapper(asset, order.Direction);
            var pricesEndTime = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Sell:
                    //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                    if (prices.Low < order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // Assuming worse case scenario fill - fill at lowest of the stop & asset price.
                        fill.FillPrice = Math.Min(order.StopPrice, prices.Current - slip);
                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;

                case OrderDirection.Buy:
                    //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                    if (prices.High > order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // Assuming worse case scenario fill - fill at highest of the stop & asset price.
                        fill.FillPrice = Math.Max(order.StopPrice, prices.Current + slip);
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
            var prices = GetPricesCheckingPythonWrapper(asset, order.Direction);
            var pricesEndTime = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            //Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                    if (prices.High > order.StopPrice || order.StopTriggered)
                    {
                        order.StopTriggered = true;

                        // Fill the limit order, using closing price of bar:
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (prices.Current < order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Min(prices.High, order.LimitPrice);
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;

                case OrderDirection.Sell:
                    //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                    if (prices.Low < order.StopPrice || order.StopTriggered)
                    {
                        order.StopTriggered = true;

                        // Fill the limit order, using minimum price of the bar
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (prices.Current > order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Max(prices.Low, order.LimitPrice);
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
            var prices = GetPricesCheckingPythonWrapper(asset, order.Direction);
            var pricesEndTime = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            //-> Valid Live/Model Order:
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //Buy limit seeks lowest price
                    if (prices.Low < order.LimitPrice)
                    {
                        //Set order fill:
                        fill.Status = OrderStatus.Filled;
                        // fill at the worse price this bar or the limit price, this allows far out of the money limits
                        // to be executed properly
                        fill.FillPrice = Math.Min(prices.High, order.LimitPrice);
                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;
                case OrderDirection.Sell:
                    //Sell limit seeks highest price possible
                    if (prices.High > order.LimitPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // fill at the worse price this bar or the limit price, this allows far out of the money limits
                        // to be executed properly
                        fill.FillPrice = Math.Max(prices.Low, order.LimitPrice);
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
            var currentBar = asset.GetLastData();
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            if (currentBar == null || localOrderTime >= currentBar.EndTime) return fill;

            // if the MOO was submitted during market the previous day, wait for a day to turn over
            if (asset.Exchange.DateTimeIsOpen(localOrderTime) && localOrderTime.Date == asset.LocalTime.Date)
            {
                return fill;
            }

            // wait until market open
            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            fill.FillPrice = GetPricesCheckingPythonWrapper(asset, order.Direction).Open;
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

            fill.FillPrice = GetPricesCheckingPythonWrapper(asset, order.Direction).Close;
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
        /// This is required due to a limitation in PythonNet to resolved
        /// overriden methods. <see cref="GetPrices"/>
        /// </summary>
        private Prices GetPricesCheckingPythonWrapper(Security asset, OrderDirection direction)
        {
            if (PythonWrapper != null)
            {
                return PythonWrapper.GetPrices(asset, direction);
            }
            return GetPrices(asset, direction);
        }

        /// <summary>
        /// Get the minimum and maximum price for this security in the last bar:
        /// </summary>
        /// <param name="asset">Security asset we're checking</param>
        /// <param name="direction">The order direction, decides whether to pick bid or ask</param>
        protected virtual Prices GetPrices(Security asset, OrderDirection direction)
        {
            var low = asset.Low;
            var high = asset.High;
            var open = asset.Open;
            var close = asset.Close;
            var current = asset.Price;
            var endTime = asset.Cache.GetData()?.EndTime ?? DateTime.MinValue;

            if (direction == OrderDirection.Hold)
            {
                return new Prices(endTime, current, open, high, low, close);
            }

            // Only fill with data types we are subscribed to
            var subscriptionTypes = Parameters.ConfigProvider
                .GetSubscriptionDataConfigs(asset.Symbol)
                .Select(x => x.Type).ToList();
            // Tick
            var tick = asset.Cache.GetData<Tick>();
            if (tick != null && subscriptionTypes.Contains(typeof(Tick)))
            {
                var price = direction == OrderDirection.Sell ? tick.BidPrice : tick.AskPrice;
                if (price != 0m)
                {
                    return new Prices(tick.EndTime, price, 0, 0, 0, 0);
                }

                // If the ask/bid spreads are not available for ticks, try the price
                price = tick.Price;
                if (price != 0m)
                {
                    return new Prices(tick.EndTime, price, 0, 0, 0, 0);
                }
            }

            // Quote
            var quoteBar = asset.Cache.GetData<QuoteBar>();
            if (quoteBar != null && subscriptionTypes.Contains(typeof(QuoteBar)))
            {
                var bar = direction == OrderDirection.Sell ? quoteBar.Bid : quoteBar.Ask;
                if (bar != null)
                {
                    return new Prices(quoteBar.EndTime, bar);
                }
            }

            // Trade
            var tradeBar = asset.Cache.GetData<TradeBar>();
            if (tradeBar != null && subscriptionTypes.Contains(typeof(TradeBar)))
            {
                return new Prices(tradeBar);
            }

            return new Prices(endTime, current, open, high, low, close);
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

        public class Prices
        {
            public readonly DateTime EndTime;
            public readonly decimal Current;
            public readonly decimal Open;
            public readonly decimal High;
            public readonly decimal Low;
            public readonly decimal Close;

            public Prices(IBaseDataBar bar)
                : this(bar.EndTime, bar.Close, bar.Open, bar.High, bar.Low, bar.Close)
            {
            }

            public Prices(DateTime endTime, IBar bar)
                : this(endTime, bar.Close, bar.Open, bar.High, bar.Low, bar.Close)
            {
            }

            public Prices(DateTime endTime, decimal current, decimal open, decimal high, decimal low, decimal close)
            {
                EndTime = endTime;
                Current = current;
                Open = open == 0 ? current : open;
                High = high == 0 ? current : high;
                Low = low == 0 ? current : low;
                Close = close == 0 ? current : close;
            }
        }
    }
}