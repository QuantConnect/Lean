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
        protected FillModelPythonWrapper PythonWrapper { get; set; }

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

            var orderEvents = new List<OrderEvent>(1);
            switch (parameters.Order.Type)
            {
                case OrderType.Market:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.MarketFill(parameters.Security, parameters.Order as MarketOrder)
                        : MarketFill(parameters.Security, parameters.Order as MarketOrder));
                    break;
                case OrderType.Limit:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.LimitFill(parameters.Security, parameters.Order as LimitOrder)
                        : LimitFill(parameters.Security, parameters.Order as LimitOrder));
                    break;
                case OrderType.LimitIfTouched:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.LimitIfTouchedFill(parameters.Security, parameters.Order as LimitIfTouchedOrder)
                        : LimitIfTouchedFill(parameters.Security, parameters.Order as LimitIfTouchedOrder));
                    break;
                case OrderType.StopMarket:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.StopMarketFill(parameters.Security, parameters.Order as StopMarketOrder)
                        : StopMarketFill(parameters.Security, parameters.Order as StopMarketOrder));
                    break;
                case OrderType.TrailingStop:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.TrailingStopFill(parameters.Security, parameters.Order as TrailingStopOrder)
                        : TrailingStopFill(parameters.Security, parameters.Order as TrailingStopOrder));
                    break;
                case OrderType.StopLimit:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.StopLimitFill(parameters.Security, parameters.Order as StopLimitOrder)
                        : StopLimitFill(parameters.Security, parameters.Order as StopLimitOrder));
                    break;
                case OrderType.MarketOnOpen:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.MarketOnOpenFill(parameters.Security, parameters.Order as MarketOnOpenOrder)
                        : MarketOnOpenFill(parameters.Security, parameters.Order as MarketOnOpenOrder));
                    break;
                case OrderType.MarketOnClose:
                    orderEvents.Add(PythonWrapper != null
                        ? PythonWrapper.MarketOnCloseFill(parameters.Security, parameters.Order as MarketOnCloseOrder)
                        : MarketOnCloseFill(parameters.Security, parameters.Order as MarketOnCloseOrder));
                    break;
                case OrderType.ComboMarket:
                    orderEvents = PythonWrapper != null
                        ? PythonWrapper.ComboMarketFill(parameters.Order, parameters)
                        : ComboMarketFill(parameters.Order, parameters);
                    break;
                case OrderType.ComboLimit:
                    orderEvents = PythonWrapper != null
                        ? PythonWrapper.ComboLimitFill(parameters.Order, parameters)
                        : ComboLimitFill(parameters.Order, parameters);
                    break;
                case OrderType.ComboLegLimit:
                    orderEvents = PythonWrapper != null
                        ? PythonWrapper.ComboLegLimitFill(parameters.Order, parameters)
                        : ComboLegLimitFill(parameters.Order, parameters);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new Fill(orderEvents);
        }


        /// <summary>
        /// Default combo market fill model for the base security class. Fills at the last traded price for each leg.
        /// </summary>
        /// <param name="order">Order to fill</param>
        /// <param name="parameters">Fill parameters for the order</param>
        /// <returns>Order fill information detailing the average price and quantity filled for each leg. If any of the fills fails, none of the orders will be filled and the returned list will be empty</returns>
        public virtual List<OrderEvent> ComboMarketFill(Order order, FillModelParameters parameters)
        {
            var fills = new List<OrderEvent>(parameters.SecuritiesForOrders.Count);
            foreach (var kvp in parameters.SecuritiesForOrders.OrderBy(x => x.Key.Id))
            {
                var targetOrder = kvp.Key;
                var security = kvp.Value;
                var fill = InternalMarketFill(security, targetOrder, targetOrder.Quantity);
                if (fill.Status != OrderStatus.Filled)
                {
                    return new List<OrderEvent>();
                }

                fills.Add(fill);
            }

            return fills;
        }

        /// <summary>
        /// Default combo limit fill model for the base security class. Fills at the sum of prices for the assets of every leg.
        /// </summary>
        /// <param name="order">Order to fill</param>
        /// <param name="parameters">Fill parameters for the order</param>
        /// <returns>Order fill information detailing the average price and quantity filled for each leg. If any of the fills fails, none of the orders will be filled and the returned list will be empty</returns>
        public virtual List<OrderEvent> ComboLimitFill(Order order, FillModelParameters parameters)
        {
            // aggregate the prices from all the securities
            var fillParameters = new List<ComboLimitOrderLegParameters>(parameters.SecuritiesForOrders.Count);
            foreach (var kvp in parameters.SecuritiesForOrders.OrderBy(x => x.Key.Id))
            {
                var targetOrder = kvp.Key;
                var security = kvp.Value;
                var prices = GetPricesCheckingPythonWrapper(security, targetOrder.Direction);

                if (prices.EndTime.ConvertToUtc(security.Exchange.TimeZone) < targetOrder.Time)
                {
                    // do not fill on stale data
                    return new List<OrderEvent>();
                }

                fillParameters.Add(new ComboLimitOrderLegParameters
                {
                    Security = security,
                    Order = targetOrder,
                    Prices = prices
                });
            }

            var currentPrice = fillParameters.Aggregate(0m, (accumulatedPrice, p) => accumulatedPrice + p.Price);
            var limitPrice = order.GroupOrderManager.LimitPrice;

            var fills = new List<OrderEvent>(fillParameters.Count);

            switch (order.GroupOrderManager.Direction)
            {
                case OrderDirection.Buy:
                    //Buy limit seeks lowest price
                    if (currentPrice < limitPrice)
                    {
                        for (var i = 0; i < fillParameters.Count; i++)
                        {
                            var targetParameters = fillParameters[i];
                            var utcTime = targetParameters.Security.LocalTime.ConvertToUtc(targetParameters.Security.Exchange.TimeZone);
                            var fill = new OrderEvent(targetParameters.Order, utcTime, OrderFee.Zero);

                            //Set order fill:
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = targetParameters.Prices.Low;
                            // assume the order completely filled
                            fill.FillQuantity = targetParameters.Order.Quantity;

                            fills.Add(fill);
                        }
                    }
                    break;

                case OrderDirection.Sell:
                    //Sell limit seeks highest price possible
                    if (currentPrice > limitPrice)
                    {
                        for (var i = 0; i < fillParameters.Count; i++)
                        {
                            var targetParameters = fillParameters[i];
                            var utcTime = targetParameters.Security.LocalTime.ConvertToUtc(targetParameters.Security.Exchange.TimeZone);
                            var fill = new OrderEvent(targetParameters.Order, utcTime, OrderFee.Zero);

                            //Set order fill:
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = targetParameters.Prices.High;
                            // assume the order completely filled
                            fill.FillQuantity = targetParameters.Order.Quantity;

                            fills.Add(fill);
                        }
                    }
                    break;
            }

            return fills;
        }

        /// <summary>
        /// Default combo limit fill model for the base security class. Fills at the limit price for each leg
        /// </summary>
        /// <param name="order">Order to fill</param>
        /// <param name="parameters">Fill parameters for the order</param>
        /// <returns>Order fill information detailing the average price and quantity filled for each leg. If any of the fills fails, none of the orders will be filled and the returned list will be empty</returns>
        public virtual List<OrderEvent> ComboLegLimitFill(Order order, FillModelParameters parameters)
        {
            var fills = new List<OrderEvent>(order.GroupOrderManager.OrderIds.Count);

            foreach (var kvp in parameters.SecuritiesForOrders.OrderBy(x => x.Key.Id))
            {
                var targetOrder = kvp.Key;
                var security = kvp.Value;

                var fill = InternalLimitFill(security, targetOrder, (targetOrder as ComboLegLimitOrder).LimitPrice,
                    targetOrder.Quantity);

                if (fill.Status != OrderStatus.Filled)
                {
                    return new List<OrderEvent>();
                }

                fills.Add(fill);
            }

            return fills;
        }

        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            return InternalMarketFill(asset, order, order.Quantity);
        }


        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        private OrderEvent InternalMarketFill(Security asset, Order order, decimal quantity)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            var orderDirection = order.Direction;
            var prices = GetPricesCheckingPythonWrapper(asset, orderDirection);
            var pricesEndTimeUtc = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // if the order is filled on stale (fill-forward) data, set a warning message on the order event
            if (pricesEndTimeUtc.Add(Parameters.StalePriceTimeSpan) < order.Time)
            {
                fill.Message = Messages.FillModel.FilledAtStalePrice(asset, prices);
            }

            //Order [fill]price for a market order model is the current security price
            fill.FillPrice = prices.Current;
            fill.Status = OrderStatus.Filled;

            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            //Apply slippage
            switch (orderDirection)
            {
                case OrderDirection.Buy:
                    fill.FillPrice += slip;
                    break;
                case OrderDirection.Sell:
                    fill.FillPrice -= slip;
                    break;
            }

            // assume the order completely filled
            fill.FillQuantity = quantity;

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
        /// Default trailing stop fill model implementation in base class security. (Trailing Stop Order Type)
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public virtual OrderEvent TrailingStopFill(Security asset, TrailingStopOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            // If its canceled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // Make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            // Get the range of prices in the last bar:
            var prices = GetPricesCheckingPythonWrapper(asset, order.Direction);
            var pricesEndTime = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // Do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            switch (order.Direction)
            {
                case OrderDirection.Sell:
                    // Fill sell if market price drops below stop price
                    if (prices.Low <= order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // Assuming worse case scenario fill - fill at lowest of the stop & asset price.
                        fill.FillPrice = Math.Min(order.StopPrice, prices.Current - slip);
                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;

                case OrderDirection.Buy:
                    // Fill buy if market price rises above stop price
                    if (prices.High >= order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // Assuming worse case scenario fill - fill at highest of the stop & asset price.
                        fill.FillPrice = Math.Max(order.StopPrice, prices.Current + slip);
                        // assume the order completely filled
                        fill.FillQuantity = order.Quantity;
                    }
                    break;
            }

            // Update the stop price:
            // NOTE: Doing this after attempting to fill the order in the following cases:
            //  - Sell: if low < stop price, order is filled. If we were to update the stop price before and it is moved towards the high price
            //          placing the stop price above the low price, it will not trigger a fill.
            //  - Buy: if high > stop price, order is filled. If we were to update the stop price before and it is moved towards the low price
            //         placing the stop price below the high price, it will not trigger a fill.
            if (fill.Status != OrderStatus.Filled &&
                TrailingStopOrder.TryUpdateStopPrice(order.Direction == OrderDirection.Sell ? prices.High : prices.Low, order.StopPrice,
                    order.TrailingAmount, order.TrailingAsPercentage, order.Direction, out var updatedStopPrice))
            {
                order.StopPrice = updatedStopPrice;
                Parameters.OnOrderUpdated(order);
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
            if (!IsExchangeOpen(asset))
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
                        if (!order.StopTriggered)
                        {
                            order.StopTriggered = true;
                            Parameters.OnOrderUpdated(order);
                        }

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
                        if (!order.StopTriggered)
                        {
                            order.StopTriggered = true;
                            Parameters.OnOrderUpdated(order);
                        }

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
        /// Default limit if touched fill model implementation in base class security. (Limit If Touched Order Type)
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="order"></param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <remarks>
        ///     There is no good way to model limit orders with OHLC because we never know whether the market has
        ///     gapped past our fill price. We have to make the assumption of a fluid, high volume market.
        ///
        ///     With Limit if Touched orders, whether or not a trigger is surpassed is determined by the high (low)
        ///     of the previous tradebar when making a sell (buy) request. Following the behaviour of
        ///     <see cref="StopLimitFill"/>, current quote information is used when determining fill parameters
        ///     (e.g., price, quantity) as the tradebar containing the incoming data is not yet consolidated.
        ///     This conservative approach, however, can lead to trades not occuring as would be expected when
        ///     compared to future consolidated data.
        /// </remarks>
        public virtual OrderEvent LimitIfTouchedFill(Security asset, LimitIfTouchedOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // Fill only if open or extended
            if (!IsExchangeOpen(asset))
            {
                return fill;
            }

            // Get the range of prices in the last bar:
            var tradeHigh = 0m;
            var tradeLow = 0m;
            var pricesEndTime = DateTime.MinValue;

            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var trade = GetPricesCheckingPythonWrapper(asset, order.Direction);

                if (trade != null)
                {
                    tradeHigh = trade.Current;
                    tradeLow = trade.Current;
                    pricesEndTime = trade.EndTime.ConvertToUtc(asset.Exchange.TimeZone);
                }
            }

            else if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null)
                {
                    tradeHigh = tradeBar.High;
                    tradeLow = tradeBar.Low;
                    pricesEndTime = tradeBar.EndTime.ConvertToUtc(asset.Exchange.TimeZone);
                }
            }

            // do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            switch (order.Direction)
            {
                case OrderDirection.Sell:
                    if (tradeHigh >= order.TriggerPrice || order.TriggerTouched)
                    {
                        order.TriggerTouched = true;

                        //-> 1.1 Limit surpassed: Sell.
                        if (GetAskPrice(asset, out pricesEndTime) >= order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = order.LimitPrice;
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;

                case OrderDirection.Buy:
                    if (tradeLow <= order.TriggerPrice || order.TriggerTouched)
                    {
                        order.TriggerTouched = true;

                        //-> 1.2 Limit surpassed: Buy.
                        if (GetBidPrice(asset, out pricesEndTime) <= order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = order.LimitPrice;
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
            return InternalLimitFill(asset, order, order.LimitPrice, order.Quantity);
        }

        /// <summary>
        /// Default limit order fill model in the base security class.
        /// </summary>
        private OrderEvent InternalLimitFill(Security asset, Order order, decimal limitPrice, decimal quantity)
        {
            //Initialise;
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(asset))
            {
                return fill;
            }
            //Get the range of prices in the last bar:
            var orderDirection = order.Direction;
            var prices = GetPricesCheckingPythonWrapper(asset, orderDirection);
            var pricesEndTime = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            //-> Valid Live/Model Order:
            switch (orderDirection)
            {
                case OrderDirection.Buy:
                    //Buy limit seeks lowest price
                    if (prices.Low < limitPrice)
                    {
                        //Set order fill:
                        fill.Status = OrderStatus.Filled;
                        // fill at the worse price this bar or the limit price, this allows far out of the money limits
                        // to be executed properly
                        fill.FillPrice = Math.Min(prices.High, limitPrice);
                        // assume the order completely filled
                        fill.FillQuantity = quantity;
                    }
                    break;
                case OrderDirection.Sell:
                    //Sell limit seeks highest price possible
                    if (prices.High > limitPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        // fill at the worse price this bar or the limit price, this allows far out of the money limits
                        // to be executed properly
                        fill.FillPrice = Math.Max(prices.Low, limitPrice);
                        // assume the order completely filled
                        fill.FillQuantity = quantity;
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
            if (asset.Exchange.Hours.IsMarketAlwaysOpen)
            {
                throw new InvalidOperationException(Messages.FillModel.MarketNeverCloses(asset, OrderType.MarketOnOpen));
            }

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
            if (asset.Exchange.Hours.IsMarketAlwaysOpen)
            {
                throw new InvalidOperationException(Messages.FillModel.MarketNeverCloses(asset, OrderType.MarketOnClose));
            }

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

            // LocalTime has reached or passed market close, proceed to fill

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
        /// Get current ask price for subscribed data
        /// This method will try to get the most recent ask price data, so it will try to get tick quote first, then quote bar.
        /// If no quote, tick or bar, is available (e.g. hourly data), use trade data with preference to tick data.
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

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice > 0);
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
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price > 0);
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

            throw new InvalidOperationException(Messages.FillModel.NoMarketDataToGetAskPriceForFilling(asset));
        }

        /// <summary>
        /// Get current bid price for subscribed data
        /// This method will try to get the most recent bid price data, so it will try to get tick quote first, then quote bar.
        /// If no quote, tick or bar, is available (e.g. hourly data), use trade data with preference to tick data.
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

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.BidPrice > 0);
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
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price > 0);
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

            throw new InvalidOperationException(Messages.FillModel.NoMarketDataToGetBidPriceForFilling(asset));
        }

        /// <summary>
        /// Get data types the Security is subscribed to
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        protected virtual HashSet<Type> GetSubscribedTypes(Security asset)
        {
            var subscribedTypes = Parameters
                .ConfigProvider
                // even though data from internal configurations are not sent to the algorithm.OnData they still drive security cache and data
                // this is specially relevant for the continuous contract underlying mapped contracts which are internal configurations
                .GetSubscriptionDataConfigs(asset.Symbol, includeInternalConfigs: true)
                .ToHashSet(x => x.Type);

            if (subscribedTypes.Count == 0)
            {
                throw new InvalidOperationException(Messages.FillModel.NoDataSubscriptionFoundForFilling(asset));
            }

            return subscribedTypes;
        }

        /// <summary>
        /// Helper method to determine if the exchange is open before filling. Will allow pre/post market fills to occur based on configuration
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private bool IsExchangeOpen(Security asset)
        {
            // even though data from internal configurations are not sent to the algorithm.OnData they still drive security cache and data
            // this is specially relevant for the continuous contract underlying mapped contracts which are internal configurations
            var configs = Parameters.ConfigProvider.GetSubscriptionDataConfigs(asset.Symbol, includeInternalConfigs: true);
            if (configs.Count == 0)
            {
                throw new InvalidOperationException(Messages.FillModel.NoDataSubscriptionFoundForFilling(asset));
            }

            var hasNonInternals = false;
            var exchangeOpenNonInternals = false;
            var exchangeOpenInternals = false;
            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];

                if (config.IsInternalFeed)
                {
                    exchangeOpenInternals |= config.ExtendedMarketHours;
                }
                else
                {
                    hasNonInternals = true;
                    exchangeOpenNonInternals |= config.ExtendedMarketHours;
                }
            }

            if (hasNonInternals)
            {
                // give priority to non internals if any
                return IsExchangeOpen(asset, exchangeOpenNonInternals);
            }
            return IsExchangeOpen(asset, exchangeOpenInternals);
        }

        /// <summary>
        /// This is required due to a limitation in PythonNet to resolved
        /// overriden methods. <see cref="GetPrices"/>
        /// </summary>
        protected virtual Prices GetPricesCheckingPythonWrapper(Security asset, OrderDirection direction)
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
            var subscriptionTypes = GetSubscribedTypes(asset);
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
        protected virtual bool IsExchangeOpen(Security asset, bool isExtendedMarketHours)
        {
            if (!asset.Exchange.Hours.IsOpen(asset.LocalTime, isExtendedMarketHours))
            {
                // if we're not open at the current time exactly, check the bar size, this handle large sized bars (hours/days)
                var currentBar = asset.GetLastData();
                if (currentBar == null)
                {
                    return false;
                }

                var barSpan = currentBar.EndTime - currentBar.Time;
                var isOnCurrentBar = barSpan > Time.OneHour
                    // for fill purposes we consider the market open for daily bars if we are in the same day
                    ? asset.LocalTime.Date == currentBar.EndTime.Date
                    // for other resolution bars, market is considered open if we are within the bar time
                    : asset.LocalTime <= currentBar.EndTime;

                return isOnCurrentBar && asset.Exchange.IsOpenDuringBar(currentBar.Time, currentBar.EndTime, isExtendedMarketHours);
            }

            return true;
        }

        private class ComboLimitOrderLegParameters
        {
            public Security Security { get; set; }
            public Order Order { get; set; }
            public Prices Prices { get; set; }

            /// <summary>
            /// Gets the current price that would be paid/received for this leg based on the security price and the leg quantity
            /// </summary>
            public decimal Price
            {
                get
                {
                    // we use the same, either low or high, for every leg depending on the combo direction
                    var price = Order.GroupOrderManager.Direction == OrderDirection.Buy ? Prices.Low : Prices.High;

                    // the limit price should be calculated using the ratios instead of the group quantities, like IB does
                    var quantity = Order.Quantity.GetOrderLegRatio(Order.GroupOrderManager);
                    if (Security.Symbol.SecurityType == SecurityType.Equity)
                    {
                        quantity /= 100;
                    }

                    return price * quantity;
                }
            }
        }
    }
}
