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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities.Equity
{
    /********************************************************
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Transaction model for equity security trades.
    /// </summary>
    /// <seealso cref="SecurityTransactionModel"/>
    /// <seealso cref="ISecurityTransactionModel"/>
    public class EquityTransactionModel : SecurityTransactionModel, ISecurityTransactionModel
    {
        /********************************************************
        * CLASS PRIVATE VARIABLES
        *********************************************************/

        /********************************************************
        * CLASS PUBLIC VARIABLES
        *********************************************************/

        /********************************************************
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Initialise the transaction model class
        /// </summary>
        public EquityTransactionModel()
        {
        }

        /********************************************************
        * CLASS PROPERTIES
        *********************************************************/


        /********************************************************
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Default equity transaction model for a market fill on this order
        /// </summary>
        /// <param name="asset">Asset we're working with</param>
        /// <param name="order">Order to update</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        public override OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            var fill = new OrderEvent(order);
            try
            {
                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        order.Price = asset.Price;
                        order.Price += slip;
                        break;
                    case OrderDirection.Sell:
                        order.Price = asset.Price;
                        order.Price -= slip;
                        break;
                }

                //Market orders fill instantly.
                order.Status = OrderStatus.Filled;
                order.Price = Math.Round(order.Price, 3);

                //Fill Order:
                fill.Status = order.Status;
                fill.FillQuantity = order.Quantity;
                fill.FillPrice = order.Price;
            }
            catch (Exception err)
            {
                Log.Error("EquityTransactionModel.MarketFill(): " + err.Message);
            }
            return fill;
        }

        /// <summary>
        /// Check if the model has stopped out our position yet:
        /// </summary>
        /// <param name="asset">Asset we're working with</param>
        /// <param name="order">Stop Order to Check, return filled if true</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            var fill = new OrderEvent(order);
            try
            {
                //If its cancelled don't need anymore checks:
                if (order.Status == OrderStatus.Canceled) return fill;

                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                //Check if the Stop Order was filled: opposite to a limit order
                switch (order.Direction)
                {
                    case OrderDirection.Sell:
                        //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                        if (asset.Price < order.StopPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = Math.Round(asset.Price, 3);
                            order.Price -= slip;
                        }
                        break;
                    case OrderDirection.Buy:
                        //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                        if (asset.Price > order.StopPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = Math.Round(asset.Price, 3);
                            order.Price += slip;
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
                Log.Error("EquityTransactionModel.StopMarketFill(): " + err.Message);
            }
            return fill;
        }

        /// <summary>
        /// Check if the price MarketDataed to our limit price yet:
        /// </summary>
        /// <param name="asset">Asset we're working with</param>
        /// <param name="order">Limit order in market</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        public override OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            //Initialise;
            var fill = new OrderEvent(order);

            try
            {
                //If its cancelled don't need anymore checks:
                if (fill.Status == OrderStatus.Canceled) return fill;

                //Calculate the model slippage: e.g. 0.01c
                var slip = GetSlippageApproximation(asset, order);

                //Depending on the resolution, return different data types:
                var marketData = asset.GetLastData();

                decimal marketDataMinPrice;
                decimal marketDataMaxPrice;
                if (marketData.DataType == MarketDataType.TradeBar)
                {
                    marketDataMinPrice = ((TradeBar)marketData).Low;
                    marketDataMaxPrice = ((TradeBar)marketData).High;
                }
                else
                {
                    marketDataMinPrice = marketData.Value;
                    marketDataMaxPrice = marketData.Value;
                }

                //-> Valid Live/Model Order:
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //Buy limit seeks lowest price
                        if (marketDataMinPrice < order.LimitPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = Math.Round(asset.Price, 3);
                            order.Price += slip;
                        }
                        break;
                    case OrderDirection.Sell:
                        //Sell limit seeks highest price possible
                        if (marketDataMaxPrice > order.LimitPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = Math.Round(asset.Price, 3);
                            order.Price -= slip;
                        }
                        break;
                }

                //Set fill:
                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    //Assuming 100% fill in models:
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = order.Price;
                    fill.Status = order.Status;
                }
            }
            catch (Exception err)
            {
                Log.Error("EquityTransactionModel.LimitFill(): " + err.Message);
            }
            return fill;
        }

        /// <summary>
        /// Get the slippage approximation for this order as a decimal value
        /// </summary>
        /// <param name="security">Security object we're working with</param>
        /// <param name="order">Order to approximate the slippage</param>
        /// <returns>Decimal value for he approximate slippage</returns>
        public override decimal GetSlippageApproximation(Security security, Order order)
        {
            return 0;
        }

        /// <summary>
        /// Get the fees from one order
        /// </summary>
        /// <param name="quantity">Quantity of shares processed</param>
        /// <param name="price">Price of the orders filled</param>
        /// <remarks>Default implementation uses the Interactive Brokers fee model of 1c per share with a maximum of 0.5% per order.</remarks>
        /// <returns>Decimal value of the order fee given this quantity and order price</returns>
        public override decimal GetOrderFee(decimal quantity, decimal price)
        {
            decimal tradeFee;
            quantity = Math.Abs(quantity);
            var tradeValue = (price * quantity);

            //Per share fees
            if (quantity < 500)
            {
                tradeFee = quantity * 0.013m;
            }
            else
            {
                tradeFee = quantity * 0.008m;
            }

            //Maximum Per Order: 0.5%
            //Minimum per order. $1.0
            if (tradeFee < 1)
            {
                tradeFee = 1;
            }
            else if (tradeFee > (0.005m * tradeValue))
            {
                tradeFee = 0.005m * tradeValue;
            }

            //Always return a positive fee.
            return Math.Abs(tradeFee);
        }

        /// <summary>
        /// Process a order fill with the supplied security and order.
        /// </summary>
        /// <param name="vehicle">Asset we're working with</param>
        /// <param name="order">Order class to check if filled.</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        [Obsolete("Fill(Security, Order) method has been made obsolete, use fill methods directly instead (e.g. MarketFill(security, marketOrder)).")]
        public override OrderEvent Fill(Security vehicle, Order order)
        {
            return new OrderEvent(order);
        }

        /// <summary>
        /// Default equity transaction model for a market fill on this order
        /// </summary>
        /// <param name="security">Asset we're working with</param>
        /// <param name="order">Order to update</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        [Obsolete("MarketFill(Security, Order) method has been made obsolete, use MarketFill(Security, MarketOrder) method instead.")]
        public override OrderEvent MarketFill(Security security, Order order)
        {
            return MarketFill(security, order as MarketOrder);
        }

        /// <summary>
        /// Check if the model has stopped out our position yet:
        /// </summary>
        /// <param name="security">Asset we're working with</param>
        /// <param name="order">Stop Order to Check, return filled if true</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        [Obsolete("StopFill(Security, Order) method has been made obsolete, use StopMarketFill(Security, StopMarketOrder) method instead.")]
        public override OrderEvent StopFill(Security security, Order order)
        {
            return StopMarketFill(security, order as StopMarketOrder);
        }

        /// <summary>
        /// Check if the price MarketDataed to our limit price yet:
        /// </summary>
        /// <param name="security">Asset we're working with</param>
        /// <param name="order">Limit order in market</param>
        /// <returns>OrderEvent packet with the full or partial fill information</returns>
        [Obsolete("LimitFill(Security, Order) method has been made obsolete, use LimitFill(Security, LimitOrder) method instead.")]
        public override OrderEvent LimitFill(Security security, Order order)
        {
            return LimitFill(security, order as LimitOrder);
        }
    } // End Algorithm Transaction Filling Classes

} // End QC Namespace
