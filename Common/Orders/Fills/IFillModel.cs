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
using System.Linq.Expressions;
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Represents a base model that simulates order fill events
    /// </summary>
    public abstract class IFillModel : InterfaceFillModel
    {
        private readonly ConstantExpression _self;
        private readonly ParameterExpression _securityParameter;
        private bool _checkedMarketFill;
        private bool _checkedStopMarketFill;
        private bool _checkedStopLimitFill;
        private bool _checkedLimitFill;
        private bool _checkedMarketOnOpenFill;
        private bool _checkedMarketOnCloseFill;
        private Func<Security, MarketOrder, OrderEvent> _marketFill;
        private Func<Security, StopMarketOrder, OrderEvent> _stopMarketFill;
        private Func<Security, StopLimitOrder, OrderEvent> _stopLimitFill;
        private Func<Security, LimitOrder, OrderEvent> _limitFill;
        private Func<Security, MarketOnOpenOrder, OrderEvent> _marketOnOpenFill;
        private Func<Security, MarketOnCloseOrder, OrderEvent> _marketOnCloseFill;

        /// <summary>
        /// The <see cref="SubscriptionDataConfig"/> provider to use
        /// </summary>
        protected ISubscriptionDataConfigProvider SubscriptionDataConfigProvider;

        /// <summary>
        /// Initializes and creates a new instance
        /// </summary>
        protected IFillModel()
        {
            _self = Expression.Constant(this);
            _securityParameter = Expression.Parameter(typeof(Security), "security");
        }

        /// <summary>
        /// Return an order event with the fill details
        /// </summary>
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual OrderEvent Fill(FillModelContext context)
        {
            var order = context.Order;
            SubscriptionDataConfigProvider = context.ConfigProvider;

            switch (order.Type)
            {
                case OrderType.Market:
                    return MarketFill(context);
                case OrderType.Limit:
                    return LimitFill(context);
                case OrderType.StopMarket:
                    return StopMarketFill(context);
                case OrderType.StopLimit:
                    return StopLimitFill(context);
                case OrderType.MarketOnOpen:
                    return MarketOnOpenFill(context);
                case OrderType.MarketOnClose:
                    return MarketOnCloseFill(context);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Model the slippage on a market order: fixed percentage of order price
        /// </summary>
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        protected virtual OrderEvent MarketFill(FillModelContext context)
        {
            if (!_checkedMarketFill)
            {
                _checkedMarketFill = true;
                SetFunctionCall(context.Order.Type);
            }

            if (_marketFill != null)
            {
                return  _marketFill(context.Security, context.Order as MarketOrder);
            }

            return MarketFillImplementation(context);
        }

        /// <summary>
        /// This method should only be called by <see cref="FillModel"/> and <see cref="IFillModel"/>.
        /// This was created to maintain retro compatibility allowing old user code
        /// to access base.xxxxFill(asset, order) implementation
        /// </summary>
        protected OrderEvent MarketFillImplementation(FillModelContext context)
        {
            var asset = context.Security;
            var order = context.Order as MarketOrder;

            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, 0);

            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            //Order [fill]price for a market order model is the current security price
            fill.FillPrice = GetPrices(asset, order.Direction).Current;
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
        /// Stop Market Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        protected virtual OrderEvent StopMarketFill(FillModelContext context)
        {
            if (!_checkedStopMarketFill)
            {
                _checkedStopMarketFill = true;
                SetFunctionCall(context.Order.Type);
            }

            if (_stopMarketFill != null)
            {
                return _stopMarketFill(context.Security, context.Order as StopMarketOrder);
            }

            return StopMarketFillImplementation(context);
        }

        /// <summary>
        /// This method should only be called by <see cref="FillModel"/> and <see cref="IFillModel"/>.
        /// This was created to maintain retro compatibility allowing old user code
        /// to access base.xxxxFill(asset, order) implementation
        /// </summary>
        protected OrderEvent StopMarketFillImplementation(FillModelContext context)
        {
            var asset = context.Security;
            var order = context.Order as StopMarketOrder;

            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, 0);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            //Get the range of prices in the last bar:
            var prices = GetPrices(asset, order.Direction);
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
        /// Stop Limit Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <remarks>
        ///     There is no good way to model limit orders with OHLC because we never know whether the market has
        ///     gapped past our fill price. We have to make the assumption of a fluid, high volume market.
        ///
        ///     Stop limit orders we also can't be sure of the order of the H - L values for the limit fill. The assumption
        ///     was made the limit fill will be done with closing price of the bar after the stop has been triggered..
        /// </remarks>
        protected virtual OrderEvent StopLimitFill(FillModelContext context)
        {
            if (!_checkedStopLimitFill)
            {
                _checkedStopLimitFill = true;
                SetFunctionCall(context.Order.Type);
            }

            if (_stopLimitFill != null)
            {
                return _stopLimitFill(context.Security, context.Order as StopLimitOrder);
            }

            return StopLimitFillImplementation(context);
        }

        /// <summary>
        /// This method should only be called by <see cref="FillModel"/> and <see cref="IFillModel"/>.
        /// This was created to maintain retro compatibility allowing old user code
        /// to access base.xxxxFill(asset, order) implementation
        /// </summary>
        protected OrderEvent StopLimitFillImplementation(FillModelContext context)
        {
            var asset = context.Security;
            var order = context.Order as StopLimitOrder;

            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, 0);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(
                asset,
                SubscriptionDataConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            //Get the range of prices in the last bar:
            var prices = GetPrices(asset, order.Direction);
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
                        if (asset.Price < order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = order.LimitPrice;
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
                        if (asset.Price > order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = order.LimitPrice; // Fill at limit price not asset price.
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Limit Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        protected virtual OrderEvent LimitFill(FillModelContext context)
        {
            if (!_checkedLimitFill)
            {
                _checkedLimitFill = true;
                SetFunctionCall(context.Order.Type);
            }

            if (_limitFill != null)
            {
                return _limitFill(context.Security, context.Order as LimitOrder);
            }

            return LimitFillImplementation(context);
        }

        /// <summary>
        /// This method should only be called by <see cref="FillModel"/> and <see cref="IFillModel"/>.
        /// This was created to maintain retro compatibility allowing old user code
        /// to access base.xxxxFill(asset, order) implementation
        /// </summary>
        protected OrderEvent LimitFillImplementation(FillModelContext context)
        {
            var asset = context.Security;
            var order = context.Order as LimitOrder;
            //Initialise;
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, 0);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(asset,
                SubscriptionDataConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            //Get the range of prices in the last bar:
            var prices = GetPrices(asset, order.Direction);
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
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        protected virtual OrderEvent MarketOnOpenFill(FillModelContext context)
        {
            if (!_checkedMarketOnOpenFill)
            {
                _checkedMarketOnOpenFill = true;
                SetFunctionCall(context.Order.Type);
            }

            if (_marketOnOpenFill != null)
            {
                return _marketOnOpenFill(context.Security, context.Order as MarketOnOpenOrder);
            }

            return MarketOnOpenFillImplementation(context);
        }

        /// <summary>
        /// This method should only be called by <see cref="FillModel"/> and <see cref="IFillModel"/>.
        /// This was created to maintain retro compatibility allowing old user code
        /// to access base.xxxxFill(asset, order) implementation
        /// </summary>
        protected OrderEvent MarketOnOpenFillImplementation(FillModelContext context)
        {
            var asset = context.Security;
            var order = context.Order as MarketOnOpenOrder;

            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, 0);

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

            fill.FillPrice = GetPrices(asset, order.Direction).Open;
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
        /// <param name="context">A context object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        protected virtual OrderEvent MarketOnCloseFill(FillModelContext context)
        {
            if (!_checkedMarketOnCloseFill)
            {
                _checkedMarketOnCloseFill = true;
                SetFunctionCall(context.Order.Type);
            }

            if (_marketOnCloseFill != null)
            {
                return _marketOnCloseFill(context.Security, context.Order as MarketOnCloseOrder);
            }

            return MarketOnCloseFillImplementation(context);
        }

        /// <summary>
        /// This method should only be called by <see cref="FillModel"/> and <see cref="IFillModel"/>.
        /// This was created to maintain retro compatibility allowing old user code
        /// to access base.xxxxFill(asset, order) implementation
        /// </summary>
        protected OrderEvent MarketOnCloseFillImplementation(FillModelContext context)
        {
            var asset = context.Security;
            var order = context.Order as MarketOnCloseOrder;

            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, 0);

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

            fill.FillPrice = GetPrices(asset, order.Direction).Close;
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
            var subscriptionTypes = SubscriptionDataConfigProvider
                .GetSubscriptionDataConfigs(asset.Symbol)
                .Select(x => x.Type).ToList();

            // Tick
            var tick = asset.Cache.GetData<Tick>();
            if (subscriptionTypes.Contains(typeof(Tick)) && tick != null)
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
            if (subscriptionTypes.Contains(typeof(QuoteBar)) && quoteBar != null)
            {
                var bar = direction == OrderDirection.Sell ? quoteBar.Bid : quoteBar.Ask;
                if (bar != null)
                {
                    return new Prices(quoteBar.EndTime, bar);
                }
            }

            // Trade
            var tradeBar = asset.Cache.GetData<TradeBar>();
            if (subscriptionTypes.Contains(typeof(TradeBar)) && tradeBar != null)
            {
                return new Prices(tradeBar);
            }

            return new Prices(endTime, current, open, high, low, close);
        }

        /// <summary>
        /// Determines if the exchange is open using the current time of the asset
        /// </summary>
        private static bool IsExchangeOpen(Security asset, bool isExtendedMarketHours)
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

        /// <summary>
        /// Helper method that will set the retro compatible fill functions
        /// </summary>
        /// <param name="orderType">Used to determine the type of fill</param>
        private void SetFunctionCall(OrderType orderType)
        {
            string methodName;
            Type inputType;
            switch (orderType)
            {
                case OrderType.Market:
                    methodName = "MarketFill";
                    inputType = typeof(MarketOrder);
                    break;
                case OrderType.Limit:
                    methodName = "LimitFill";
                    inputType = typeof(LimitOrder);
                    break;
                case OrderType.StopMarket:
                    methodName = "StopMarketFill";
                    inputType = typeof(StopMarketOrder);
                    break;
                case OrderType.StopLimit:
                    methodName = "StopLimitFill";
                    inputType = typeof(StopLimitOrder);
                    break;
                case OrderType.MarketOnOpen:
                    methodName = "MarketOnOpenFill";
                    inputType = typeof(MarketOnOpenOrder);
                    break;
                case OrderType.MarketOnClose:
                    methodName = "MarketOnCloseFill";
                    inputType = typeof(MarketOnCloseOrder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var method = GetMethod(GetType(),
                methodName,
                typeof(OrderEvent),
                typeof(FillModel),
                new[] { typeof(Security), inputType });

            if (method != null)
            {
                // we've found the old method, now create a delegate so we can invoke it
                var orderParameter = Expression.Parameter(inputType, "order");
                var call = Expression.Call(_self, method, _securityParameter, orderParameter);
                switch (orderType)
                {
                    case OrderType.Market:
                        _marketFill = Expression.Lambda<Func<Security, MarketOrder, OrderEvent>>(
                            call,
                            _securityParameter,
                            orderParameter).Compile();
                        break;
                    case OrderType.Limit:
                        _limitFill = Expression.Lambda<Func<Security, LimitOrder, OrderEvent>>(
                            call,
                            _securityParameter,
                            orderParameter).Compile();
                        break;
                    case OrderType.StopMarket:
                        _stopMarketFill = Expression.Lambda<Func<Security, StopMarketOrder, OrderEvent>>(
                            call,
                            _securityParameter,
                            orderParameter).Compile();
                        break;
                    case OrderType.StopLimit:
                        _stopLimitFill = Expression.Lambda<Func<Security, StopLimitOrder, OrderEvent>>(
                            call,
                            _securityParameter,
                            orderParameter).Compile();
                        break;
                    case OrderType.MarketOnOpen:
                        _marketOnOpenFill = Expression.Lambda<Func<Security, MarketOnOpenOrder, OrderEvent>>(
                            call,
                            _securityParameter,
                            orderParameter).Compile();
                        break;
                    case OrderType.MarketOnClose:
                        _marketOnCloseFill = Expression.Lambda<Func<Security, MarketOnCloseOrder, OrderEvent>>(
                            call,
                            _securityParameter,
                            orderParameter).Compile();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Helper method to find a specific <see cref="MethodInfo"/> for a given Type
        /// </summary>
        /// <param name="type">The Type we want to search the method in</param>
        /// <param name="methodName">The method name</param>
        /// <param name="returnType">The return Type of the method to search</param>
        /// <param name="nonDeclaringType">A Type how should not be the declaring Type of the method.
        /// This is useful when wanting to detect if a method was overriden</param>
        /// <param name="inputTypes">The input Types of the method</param>
        /// <returns></returns>
        private static MethodInfo GetMethod(
            Type type,
            string methodName,
            Type returnType,
            Type nonDeclaringType,
            Type[] inputTypes
            )
        {
            var parameterCount = inputTypes.Length;
            var method = type.GetMethods()
                .Where(m => m.Name == methodName)
                .Where(m => m.ReturnType == returnType)
                .Where(m => m.DeclaringType != nonDeclaringType)
                .Where(m => m.GetParameters().Length == parameterCount)
                .SingleOrDefault(m => m.GetParameters()
                    .Select(x => x.ParameterType)
                    .All(inputTypes.Contains));
            return method;
        }
    }
}