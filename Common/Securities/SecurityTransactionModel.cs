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
        /// Process an order to see if it has been filled and return the matching order event.
        /// </summary>
        /// <param name="vehicle">Asset we're working with</param>
        /// <param name="order">Order class to check if filled.</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        public virtual OrderEvent Fill(Security vehicle, Order order)
        {
            //Default order event to return.
            var fill = new OrderEvent(order);

            try 
            {
                switch (order.Type) 
                {
                    case OrderType.Limit:
                        fill = LimitFill(vehicle, order);
                        break;
                    case OrderType.StopMarket:
                        fill = StopFill(vehicle, order);
                        break;
                    case OrderType.Market:
                        fill = MarketFill(vehicle, order);
                        break;
                }
            } catch (Exception err) {
                Log.Error("SecurityTransactionModel.TransOrderDirection.Fill(): " + err.Message);
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
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopFill"/>
        /// <seealso cref="LimitFill"/>
        public virtual OrderEvent MarketFill(Security security, Order order)
        {
            //Default order event to return.
            var fill = new OrderEvent(order);
            try {
                
                //Set the order price 
                order.Price = security.Price;
                order.Status = OrderStatus.Filled;

                //Set the order event fill: - Assuming 100% fill
                fill.FillPrice = security.Price;
                fill.FillQuantity = order.Quantity;
                fill.Status = order.Status;

            } catch (Exception err) {
                Log.Error("SecurityTransactionModel.TransOrderDirection.MarketFill(): " + err.Message);
            }
            return fill;
        }




        /// <summary>
        /// Default stop fill model implementation in base class security.
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="MarketFill"/>
        /// <seealso cref="LimitFill"/>
        public virtual OrderEvent StopFill(Security security, Order order)
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
                        if (security.Price < order.Price) 
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = security.Price;
                        }
                        break;
                    case OrderDirection.Buy:
                        //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                        if (security.Price > order.Price) 
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = security.Price;
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = security.Price;        //Stop price as security price because can gap past stop price.
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
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill informaton detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopFill"/>
        /// <seealso cref="MarketFill"/>
        public virtual OrderEvent LimitFill(Security security, Order order)
        {
            //Initialise;
            var fill = new OrderEvent(order);

            try
            {
                //If its cancelled don't need anymore checks:
                if (fill.Status == OrderStatus.Canceled) return fill;

                //Depending on the resolution, return different data types:
                var marketData = security.GetLastData();

                decimal marketDataMinPrice = 0;
                decimal marketDataMaxPrice = 0;
                if (marketData.DataType == MarketDataType.TradeBar)
                {
                    marketDataMinPrice = ((TradeBar)marketData).Low;
                    marketDataMaxPrice = ((TradeBar)marketData).High;
                } else {
                    marketDataMinPrice = marketData.Value;
                    marketDataMaxPrice = marketData.Value;
                }

                //-> Valid Live/Model Order: 
                switch (order.Direction)
                {
                    case OrderDirection.Buy:
                        //Buy limit seeks lowest price
                        if (marketDataMinPrice < order.Price) 
                        {   
                            //Set order fill:
                            order.Status = OrderStatus.Filled;
                            order.Price = security.Price;
                        }
                        break;
                    case OrderDirection.Sell:
                        //Sell limit seeks highest price possible
                        if (marketDataMaxPrice > order.Price) 
                        {
                            order.Status = OrderStatus.Filled;
                            order.Price = security.Price;
                        }
                        break;
                }

                if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled)
                {
                    fill.FillQuantity = order.Quantity;
                    fill.FillPrice = security.Price;
                    fill.Status = order.Status;
                }
            } 
            catch (Exception err) 
            {
                Log.Error("SecurityTransactionModel.TransOrderDirection.LimitFill(): " + err.Message);
            }
            return fill;
        }


        /// <summary>
        /// Default security transaction model - no fees.
        /// </summary>
        public virtual decimal GetOrderFee(decimal quantity, decimal price)
        {
            return 0;
        }

    } // End Algorithm Transaction Filling Classes

} // End QC Namespace
