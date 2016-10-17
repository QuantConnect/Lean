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
using QuantConnect.Orders;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Option holdings implementation of the base securities class
    /// </summary>
    /// <seealso cref="SecurityHolding"/>
    public class OptionHolding : SecurityHolding
    {
        /// <summary>
        /// Option Holding Class constructor
        /// </summary>
        /// <param name="security">The option security being held</param>
        public OptionHolding(Security security)
            : base(security)
        {
        }

        /// <summary>
        /// Sets new option holding parameters (strike price, multipler, unit of size) in accordance with underlying split event details
        /// </summary>
        /// <param name="splitFactor">Split ratio of the underlying split</param>
        public void SetUnderlyingSplit(decimal splitFactor)
        {
            var optionSecurity = (Option)_security;
            var inverseFactor = 1.0m / splitFactor;

            // detect forward (even and odd) and reverse splits
            if (splitFactor > 1.0m)
            {
                // reverse split: we adjust units of trade for the security
                optionSecurity.ContractUnitOfTrade /= (int)splitFactor;

                // we have to update security symbol as it is changed after the split in this case
                AdjustIdentifierOnSplit();
            }

            if ((int)Math.Round(inverseFactor, 5) == (int)inverseFactor)
            {
                // even split (e.g. 2 for 1): we adjust position size and strike price
                _quantity = (int)((decimal)_quantity * inverseFactor);
                optionSecurity.StrikePrice = Math.Round(optionSecurity.StrikePrice / inverseFactor, 2);

                // we have to update security symbol as it is changed after the split in this case
                AdjustIdentifierOnSplit();
            }
            else
            {
                // odd split (e.g. 3 for 2): we adjust strike price, unit of trade, and multiplier
                optionSecurity.StrikePrice = Math.Round(optionSecurity.StrikePrice / inverseFactor, 2);
                optionSecurity.ContractUnitOfTrade *= (int)inverseFactor;
                optionSecurity.ContractMultiplier *= (int)inverseFactor;

                // we have to update security symbol as it is changed after the split in this case
                AdjustIdentifierOnSplit();
            }
        }

        /// <summary>
        /// Adjusts option ID on split if strike price has changed. Adjusts ID using rtike for now. Open question: need to change option name 
        /// </summary>
        private void AdjustIdentifierOnSplit()
        {
            var optionSecurity = (Option)_security;
            var id = _security.Symbol.ID;

            var newSymbol = Symbol.CreateOption(_security.Symbol.Underlying.Value, id.Market, id.OptionStyle, id.OptionRight, optionSecurity.StrikePrice, id.Date);

            optionSecurity.UpdateSymbol(newSymbol);
        }
    }
}