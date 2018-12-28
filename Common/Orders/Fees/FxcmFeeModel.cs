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
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models FXCM order fees
    /// </summary>
    public class FxcmFeeModel : FeeModel
    {
        private readonly string _currency;

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

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="currency">The currency of the order fee, for FXCM this is the account currency</param>
        public FxcmFeeModel(string currency = "USD")
        {
            _currency = currency;
        }

        /// <summary>
        /// Get the fee for this order in units of the account currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            // From http://www.fxcm.com/forex/forex-pricing/ (on Oct 6th, 2015)
            // Forex: $0.04 per side per 1k lot for EURUSD, GBPUSD, USDJPY, USDCHF, AUDUSD, EURJPY, GBPJPY
            //        $0.06 per side per 1k lot for other instruments

            // From https://www.fxcm.com/uk/markets/cfds/frequently-asked-questions/
            // CFD: no commissions

            decimal fee = 0;
            if (parameters.Security.Type == SecurityType.Forex)
            {
                var commissionRate = _groupCommissionSchedule1.Contains(parameters.Security.Symbol)
                    ? 0.04m : 0.06m;

                fee = Math.Abs(commissionRate * parameters.Order.AbsoluteQuantity / 1000);
            }
            return new OrderFee(new CashAmount(fee,
                _currency));
        }
    }
}