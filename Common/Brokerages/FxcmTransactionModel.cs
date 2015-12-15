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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Fxcm Transaction Model Class: Specific transaction fill models for FXCM orders
    /// </summary>
    /// <seealso cref="SecurityTransactionModel"/>
    /// <seealso cref="ISecurityTransactionModel"/>
    public class FxcmTransactionModel : SecurityTransactionModel
    {
        private static readonly ISlippageModel SlippageModel = new ConstantSlippageModel(0.0001m);

        private readonly HashSet<Symbol> _groupCommissionSchedule1 = new HashSet<Symbol>
        {
            Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM),
            Symbol.Create("GBPUSD", SecurityType.Forex, Market.FXCM),
            Symbol.Create("USDJPY", SecurityType.Forex, Market.FXCM),
            Symbol.Create("USDCHF", SecurityType.Forex, Market.FXCM),
            Symbol.Create("AUDUSD", SecurityType.Forex, Market.FXCM),
            Symbol.Create("EURJPY", SecurityType.Forex, Market.FXCM),
            Symbol.Create("GBPJPY", SecurityType.Forex, Market.FXCM),
        };

        ///// <summary>
        ///// Initialise the transaction model class
        ///// </summary>
        //public FxcmTransactionModel()
        //    : base(new DefaultFillModel(SlippageModel), new 
        //{
        //}

        /// <summary>
        /// Get the fee for this order
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to compute fees for</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override decimal GetOrderFee(Security security, Order order)
        {
            // From http://www.fxcm.com/forex/forex-pricing/ (on Oct 6th, 2015)
            // Forex: $0.04 per side per 1k lot for EURUSD, GBPUSD, USDJPY, USDCHF, AUDUSD, EURJPY, GBPJPY
            //        $0.06 per side per 1k lot for other instruments

            // From https://www.fxcm.com/uk/markets/cfds/frequently-asked-questions/
            // CFD: no commissions

            if (security.Type != SecurityType.Forex)
                return 0m;

            var commissionRate = _groupCommissionSchedule1.Contains(security.Symbol) ? 0.04m : 0.06m;

            return commissionRate * order.AbsoluteQuantity / 1000;
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
            //For FOREX, the slippage is the Bid/Ask Spread for Tick, and an approximation for TradeBars
            switch (security.Resolution)
            {
                case Resolution.Minute:
                case Resolution.Second:
                    //Get the last data packet:
                    //Assume slippage is 1/10,000th of the price
                    slippage = security.GetLastData().Value * 0.0001m;
                    break;

                case Resolution.Tick:
                    var lastTick = (Tick)security.GetLastData();
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

    }
}
