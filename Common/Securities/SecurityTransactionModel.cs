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


namespace QuantConnect.Securities 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Default security transaction model for user defined securities.
    /// </summary>
    public class SecurityTransactionModel : ISecurityTransactionModel 
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
        /// Initialize the default transaction model class
        /// </summary>
        public SecurityTransactionModel() 
        {  }

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
        public OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            //Default order event to return.
            var fill = new OrderEvent(order);
            try
            {
                //Order [fill]price for a market order model is the current security price.
                order.Price = asset.Price;
                order.Status = OrderStatus.Filled;

                //For backtesting, we assuming the order is 100% filled on first attempt.
                fill.FillPrice = asset.Price;
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
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        /// <seealso cref="LimitFill(Security, LimitOrder)"/>
        public OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            //Default order event to return.
            var fill = new OrderEvent(order);

            try
            {
                //If its cancelled don't need anymore checks:
                if (fill.Status == OrderStatus.Canceled) return fill;

                //Check if the Stop Order was filled: opposite to a limit order
                switch (order.Direction)
                {
                    case OrderDirection.Sell:
                        //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                        if (asset.Price < order.StopPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = asset.Price;
                        }
                        break;
                    case OrderDirection.Buy:
                        //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                        if (asset.Price > order.StopPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = asset.Price;
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = asset.Price;        //Stop price as security price because can gap past stop price.
                    fill.Status = order.Status;
                }
            }
            catch (Exception err)
            {
                Log.Error("SecurityTransactionModel.TransOrderDirection.StopFill(): " + err.Message);
            }

            return fill;
        }


        /// <summary>
        /// Default limit order fill model in the base security class.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            //Initialise;
            var fill = new OrderEvent(order);

            try
            {
                //If its cancelled don't need anymore checks:
                if (fill.Status == OrderStatus.Canceled) return fill;

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
                            //Set order fill:
                            order.Status = OrderStatus.Filled;
                            order.Price = asset.Price;
                        }
                        break;
                    case OrderDirection.Sell:
                        //Sell limit seeks highest price possible
                        if (marketDataMaxPrice > order.LimitPrice)
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = asset.Price;
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = asset.Price;
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
        /// Get the slippage approximation for this order
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>decimal approximation for slippage</returns>
        public virtual decimal GetSlippageApproximation(Security security, Order order)
        {
            return 0;
        }


        /// <summary>
        /// Default security transaction model - no fees.
        /// </summary>
        public virtual decimal GetOrderFee(decimal quantity, decimal price)
        {
            return 0;
        }


        /// <summary>
        /// Process an order to see if it has been filled and return the matching order event.
        /// </summary>
        /// <param name="vehicle">Asset we're working with</param>
        /// <param name="order">Order class to check if filled.</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        [Obsolete("Fill method has been made obsolete, use order type fill methods directly.")]
        public virtual OrderEvent Fill(Security vehicle, Order order)
        {
            return new OrderEvent(order);
        }


        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
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
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
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
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        [Obsolete("LimitFill(Security, Order) method has been made obsolete, use LimitFill(Security, LimitOrder) method instead.")]
        public virtual OrderEvent LimitFill(Security security, Order order)
        {
            return LimitFill(security, order as LimitOrder);
        }

    } // End Algorithm Transaction Filling Classes

} // End QC Namespace
