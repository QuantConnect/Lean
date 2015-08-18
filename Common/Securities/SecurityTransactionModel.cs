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
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities 
{
    /// <summary>
    /// Default security transaction model for user defined securities.
    /// </summary>
    public class SecurityTransactionModel : ISecurityTransactionModel
    {
        /// <summary>
        /// Initialize the default transaction model class
        /// </summary>
        public SecurityTransactionModel() 
        {  }

        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
        public virtual OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var orderFee = GetOrderFee(asset, order);
            var fill = new OrderEvent(order, utcTime, orderFee);

            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling
            if (!IsExchangeOpen(asset)) return fill;

            try
            {
                //Order [fill]price for a market order model is the current security price.
                order.Price = asset.Price;
                order.Status = OrderStatus.Filled;

                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                //Apply slippage
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        order.Price += slip;
                        break;
                    case OrderDirection.Sell:
                        order.Price -= slip;
                        break;
                }

                //For backtesting, we assuming the order is 100% filled on first attempt.
                fill.FillPrice = order.Price;
                fill.FillQuantity = order.Quantity;
                fill.Status = order.Status;
            }
            catch (Exception err)
            {
                Log.Error("SecurityTransactionModel.MarketFill(): " + err.Message);
            }

            return fill;
        }

        /// <summary>
        /// Default stop fill model implementation in base class security. (Stop Market Order Type)
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
        public virtual OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var orderFee = GetOrderFee(asset, order);
            var fill = new OrderEvent(order, utcTime, orderFee);

            // make sure the exchange is open before filling
            if (!IsExchangeOpen(asset)) return fill;

            try
            {
                //If its cancelled don't need anymore checks:
                if (order.Status == OrderStatus.Canceled) return fill;

                //Get the range of prices in the last bar:
                decimal minimumPrice;
                decimal maximumPrice;
                DataMinMaxPrices(asset, out minimumPrice, out maximumPrice);

                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                //Check if the Stop Order was filled: opposite to a limit order
                switch (order.Direction)
                {
                    case OrderDirection.Sell:
                        //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                        if (minimumPrice < order.StopPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            // Assuming worse case scenario fill - fill at lowest of the stop & asset price.
                            order.Price = Math.Min(order.StopPrice, asset.Price - slip); 
                        }
                        break;

                    case OrderDirection.Buy:
                        //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                        if (maximumPrice > order.StopPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            // Assuming worse case scenario fill - fill at highest of the stop & asset price.
                            order.Price = Math.Max(order.StopPrice, asset.Price + slip);
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = order.Price;        //we picked the correct fill price above, just respect it here
                    fill.Status = order.Status;
                }
            }
            catch (Exception err)
            {
                Log.Error("SecurityTransactionModel.StopMarketFill(): " + err.Message);
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
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
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
            var orderFee = GetOrderFee(asset, order);
            var fill = new OrderEvent(order, utcTime, orderFee);

            try
            {
                //If its cancelled don't need anymore checks:
                if (order.Status == OrderStatus.Canceled) return fill;

                //Get the range of prices in the last bar:
                decimal minimumPrice;
                decimal maximumPrice;
                DataMinMaxPrices(asset, out minimumPrice, out maximumPrice);

                //Check if the Stop Order was filled: opposite to a limit order
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                        if (maximumPrice > order.StopPrice || order.StopTriggered)
                        {
                            order.StopTriggered = true;

                            // Fill the limit order, using closing price of bar:
                            // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                            if (asset.Price < order.LimitPrice)
                            {
                                order.Status = OrderStatus.Filled;
                                order.Price = order.LimitPrice;
                            }
                        }
                        break;

                    case OrderDirection.Sell:
                        //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                        if (minimumPrice < order.StopPrice || order.StopTriggered)
                        {
                            order.StopTriggered = true;

                            // Fill the limit order, using minimum price of the bar
                            // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                            if (asset.Price > order.LimitPrice)
                            {
                                order.Status = OrderStatus.Filled;
                                order.Price = order.LimitPrice; // Fill at limit price not asset price.
                            }
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = order.Price;
                    fill.Status = order.Status;
                }
            }
            catch (Exception err)
            {
                Log.Error("SecurityTransactionModel.StopLimitFill(): " + err.Message);
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
            var orderFee = GetOrderFee(asset, order);
            var fill = new OrderEvent(order, utcTime, orderFee);

            try
            {
                //If its cancelled don't need anymore checks:
                if (order.Status == OrderStatus.Canceled) return fill;

                //Get the range of prices in the last bar:
                decimal minimumPrice;
                decimal maximumPrice;
                DataMinMaxPrices(asset, out minimumPrice, out maximumPrice);

                //-> Valid Live/Model Order: 
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //Buy limit seeks lowest price
                        if (minimumPrice < order.LimitPrice)
                        {
                            //Set order fill:
                            order.Status = OrderStatus.Filled;
                            // fill at the worse price this bar or the limit price, this allows far out of the money limits
                            // to be executed properly
                            order.Price = Math.Min(maximumPrice, order.LimitPrice); 
                        }
                        break;
                    case OrderDirection.Sell:
                        //Sell limit seeks highest price possible
                        if (maximumPrice > order.LimitPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            // fill at the worse price this bar or the limit price, this allows far out of the money limits
                            // to be executed properly
                            order.Price = Math.Max(minimumPrice, order.LimitPrice);
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = order.Price;
                    fill.Status = order.Status;
                }
            }
            catch (Exception err)
            {
                Log.Error("SecurityTransactionModel.LimitFill(): " + err.Message);
            }

            return fill;
        }

        /// <summary>
        /// Market on Open Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
        {
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var orderFee = GetOrderFee(asset, order);
            var fill = new OrderEvent(order, utcTime, orderFee);

            if (order.Status == OrderStatus.Canceled) return fill;

            try
            {
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
                // make sure the exchange is open before filling
                if (!IsExchangeOpen(asset)) return fill;

                order.Price = asset.Open;
                order.Status = OrderStatus.Filled;

                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                //Apply slippage
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        order.Price += slip;
                        break;
                    case OrderDirection.Sell:
                        order.Price -= slip;
                        break;
                }

                //For backtesting, we assuming the order is 100% filled on first attempt.
                fill.FillPrice = order.Price;
                fill.FillQuantity = order.Quantity;
                fill.Status = order.Status;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return fill;
        }

        /// <summary>
        /// Market on Close Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
        {
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var orderFee = GetOrderFee(asset, order);
            var fill = new OrderEvent(order, utcTime, orderFee);

            if (order.Status == OrderStatus.Canceled) return fill;

            try
            {
                var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
                var nextMarketClose = asset.Exchange.Hours.GetNextMarketClose(localOrderTime, false);
                
                // wait until market closes after the order time 
                if (asset.LocalTime < nextMarketClose)
                {
                    return fill;
                }

                order.Price = asset.Close;
                order.Status = OrderStatus.Filled;

                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                //Apply slippage
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        order.Price += slip;
                        break;
                    case OrderDirection.Sell:
                        order.Price -= slip;
                        break;
                }

                //For backtesting, we assuming the order is 100% filled on first attempt.
                fill.FillPrice = order.Price;
                fill.FillQuantity = order.Quantity;
                fill.Status = order.Status;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return fill;
        }

        /// <summary>
        /// Get the slippage approximation for this order
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>decimal approximation for slippage</returns>
        public virtual decimal GetSlippageApproximation(Security security, Order order)
        {
            return 0m;
        }

        /// <summary>
        /// Default security transaction model - no fees.
        /// </summary>
        public virtual decimal GetOrderFee(decimal quantity, decimal price)
        {
            return 0m;
        }

        /// <summary>
        /// Default implementation returns 0 for fees.
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to compute fees for</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public virtual decimal GetOrderFee(Security security, Order order)
        {
            if (order.Quantity == 0)
            {
                return 0m;
            }

            var price = order.Status.IsFill() ? order.Price : security.Price;
            return GetOrderFee(order.Quantity, order.GetValue(price) / order.Quantity);
        }

        /// <summary>
        /// Get the minimum and maximum price for this security in the last bar:
        /// </summary>
        /// <param name="asset">Security asset we're checking</param>
        /// <param name="minimumPrice">Minimum price in the last data bar</param>
        /// <param name="maximumPrice">Minimum price in the last data bar</param>
        public virtual void DataMinMaxPrices(Security asset, out decimal minimumPrice, out decimal maximumPrice)
        {
            var marketData = asset.GetLastData();

            var tradeBar = marketData as TradeBar;
            if (tradeBar != null)
            {
                minimumPrice = tradeBar.Low;
                maximumPrice = tradeBar.High;
            }
            else
            {
                minimumPrice = marketData.Value;
                maximumPrice = marketData.Value;
            }
        }

        /// <summary>
        /// Determines if the exchange is open using the current time of the asset
        /// </summary>
        private static bool IsExchangeOpen(Security asset)
        {
            if (!asset.Exchange.DateTimeIsOpen(asset.LocalTime))
            {
                // if we're not open at the current time exactly, check the bar size, this handle large sized bars (hours/days)
                var currentBar = asset.GetLastData();
                if (asset.LocalTime.Date != currentBar.EndTime.Date || !asset.Exchange.IsOpenDuringBar(currentBar.Time, currentBar.EndTime, false))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Process an order to see if it has been filled and return the matching order event.
        /// </summary>
        /// <param name="vehicle">Asset we're working with</param>
        /// <param name="order">Order class to check if filled.</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        [Obsolete("Fill method has been made obsolete, use order type fill methods directly.")]
        public virtual OrderEvent Fill(Security vehicle, Order order)
        {
            var utcTime = vehicle.LocalTime.ConvertToUtc(vehicle.Exchange.TimeZone);
            var orderFee = GetOrderFee(vehicle, order);
            return new OrderEvent(order, utcTime, orderFee);
        }

        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
        [Obsolete("MarketFill(Security, Order) method has been made obsolete, use MarketFill(Security, MarketOrder) method instead.")]
        public virtual OrderEvent MarketFill(Security security, Order order)
        {
            return MarketFill(security, order as MarketOrder);
        }

        /// <summary>
        /// Default stop fill model implementation in base class security. (Stop Market Order Type)
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        [Obsolete("StopFill(Security, Order) method has been made obsolete, use StopMarketFill(Security, StopMarketOrder) method instead.")]
        public virtual OrderEvent StopFill(Security security, Order order)
        {
            return StopMarketFill(security, order as StopMarketOrder);
        }

        /// <summary>
        /// Default limit order fill model in the base security class.
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        [Obsolete("LimitFill(Security, Order) method has been made obsolete, use LimitFill(Security, LimitOrder) method instead.")]
        public virtual OrderEvent LimitFill(Security security, Order order)
        {
            return LimitFill(security, order as LimitOrder);
        }
    }
}
