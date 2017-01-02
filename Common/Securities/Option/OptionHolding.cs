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
        /// Option Holding Class constructor
        /// </summary>
        /// <param name="security">The option security being held</param>
        /// <param name="holding">The option security holding</param>
        public OptionHolding(Security security, OptionHolding holding)
            : base(holding)
        {
        }

        /// <summary>
        /// Sets new option holding parameters (strike price, multiplier, unit of size) in accordance with underlying split event details
        /// </summary>
        /// <param name="splitFactor">Split ratio of the underlying split</param>
        public void SplitUnderlying(decimal splitFactor)
        {
            var optionSecurity = (Option)Security;
            var inverseFactor = 1.0m / splitFactor;

            // detect forward (even and odd) and reverse splits
            if (splitFactor > 1.0m)
            {
                // reverse split: we adjust units of trade for the security
                optionSecurity.ContractUnitOfTrade /= (int)splitFactor;
            }

            // check if the split is even or odd
            if (inverseFactor.RoundToSignificantDigits(5) % 1 == 0)
            {
                // even split (e.g. 2 for 1): we adjust position size and strike price
                Quantity = (int)(Quantity * inverseFactor);
                AveragePrice *= splitFactor;
            }
            else
            {
                // odd split (e.g. 3 for 2): we adjust strike price, unit of trade, and multiplier
                optionSecurity.ContractUnitOfTrade *= (int)inverseFactor;
                optionSecurity.ContractMultiplier *= (int)inverseFactor;
            }
        }
    }
}