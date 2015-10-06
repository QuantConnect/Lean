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
using QuantConnect.Orders;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities.Forex
{
    /// <summary>
    /// Forex Transaction Model Class: Specific transaction fill models for FOREX orders
    /// </summary>
    /// <seealso cref="SecurityTransactionModel"/>
    /// <seealso cref="ISecurityTransactionModel"/>
    public class ForexTransactionModel : SecurityTransactionModel
    {
        private readonly decimal _commissionRate;
        private readonly decimal _minimumOrderFee;

        /// <summary>
        /// Initialise the transaction model class
        /// </summary>
        public ForexTransactionModel(decimal monthlyTradeAmountInUSDollars = 0)
        {
            /*            Interactive Brokers Forex Commisions as of 2015.04.15

                Monthly Trade Amount                   Commissions                          Minimum per Order
                <=USD 1,000,000,000                    0.20basis point * Trade Value        USD 2.00
                USD 1,000,000,001 - 2,000,000,000      0.15basis point * Trade Value        USD 1.50
                USD 2,000,000,001 - 5,000,000,000      0.10basis point * Trade Value        USD 1.25
                >USD 5,000,000,000                     0.08basis point * Trade Value        USD 1.00
             * 
            */

            const decimal bp = 0.0001m;
            if (monthlyTradeAmountInUSDollars <= 1000000000) // 1 billion
            {
                _commissionRate = 0.20m*bp;
                _minimumOrderFee = 2.00m;
            }
            else if (monthlyTradeAmountInUSDollars <= 2000000000) // 2 billion
            {
                _commissionRate = 0.15m*bp;
                _minimumOrderFee = 1.50m;
            }
            else if (monthlyTradeAmountInUSDollars <= 5000000000) // 5 billion
            {
                _commissionRate = 0.20m*bp;
                _minimumOrderFee = 1.25m;
            }
            else
            {
                _commissionRate = 0.20m*bp;
                _minimumOrderFee = 1.00m;
            }
        }

        /// <summary>
        /// Get the slippage approximation for this order
        /// </summary>
        /// <returns>Decimal value of the slippage approximation</returns>
        /// <seealso cref="Order"/>
        public override decimal GetSlippageApproximation(Security security, Order order)
        {
            //Return 0 by default
            decimal slippage = 0;
            //For FOREX, the slippage is the Bid/Ask Spread for Tick, and an approximation for the 
            switch (security.Resolution)
            {
                case Resolution.Minute:
                case Resolution.Second:
                    //Get the last data packet:
                    //Assume slippage is 1/10,000th of the price
                    slippage = security.GetLastData().Value * 0.0001m;
                    break;

                case Resolution.Tick:
                    var lastTick = (Tick) security.GetLastData();
                    switch (order.Direction)
                    {
                        case OrderDirection.Buy:
                            //We're buying, assume slip to Asking Price.
                            slippage = Math.Abs(order.Price - lastTick.AskPrice);
                            break;

                        case OrderDirection.Sell:
                            //We're selling, assume slip to the bid price.
                            slippage = Math.Abs(order.Price - lastTick.BidPrice);
                            break;
                    }
                    break;
            }
            return slippage;
        }

        /// <summary>
        /// Get the fees from this order
        /// </summary>
        /// <param name="quantity">Quantity of purchase</param>
        /// <param name="price">Price of the currency</param>
        /// <remarks>
        ///     FXCM now uses a flat fee per trade instead of a spread model. This spread model is 
        ///     out of date but the data has the spread built into historical data. >> New data source needed.
        /// </remarks>
        /// <returns>Decimal value of the order fee</returns>
        public override decimal GetOrderFee(decimal quantity, decimal price)
        {
            var fee = _commissionRate*quantity*price;
            return Math.Max(_minimumOrderFee, fee);
        }

        /// <summary>
        /// Default implementation returns 0 for fees.
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to compute fees for</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override decimal GetOrderFee(Security security, Order order)
        {
            var forex = (Forex) security;

            // get the total order value in the account currency
            var price = order.Status.IsFill() ? order.Price : security.Price;
            var totalOrderValue = order.GetValue(price)*forex.QuoteCurrency.ConversionRate;
            var fee = _commissionRate*totalOrderValue;
            return Math.Max(_minimumOrderFee, fee);
        }
    }
}