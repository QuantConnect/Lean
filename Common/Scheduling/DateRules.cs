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
 *
*/

using System;
using QuantConnect.Securities;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining date rules
    /// </summary>
    public class DateRules
    {
        private readonly SecurityManager _securities;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        public DateRules(SecurityManager securities)
        {
            _securities = securities;
        }

        /// <summary>
        /// Specifies an event should fire every day
        /// </summary>
        /// <returns>A date rule that fires every day</returns>
        public IDateRule EveryDay()
        {
            return EveryDayDateRule.Instance;
        }

        /// <summary>
        /// Specifies an event should fire every day the symbol is trading
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine tradeable dates</param>
        /// <returns>A date rule that fires every day the specified symbol trades</returns>
        public IDateRule EveryDay(string symbol)
        {
            return new EveryTradeableDayDateRule(GetSecurity(symbol));
        }

        /// <summary>
        /// Specifies an event should fire on the first of each month
        /// </summary>
        /// <returns>A date rule that fires on the first of each month</returns>
        public IDateRule MonthStart()
        {
            return MonthStartDateRule.Instance;
        }

        /// <summary>
        /// Specifies an event should fire on the first tradeable date for the specified
        /// symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first 
        /// tradeable date of the month</param>
        /// <returns>A date rule that fires on the first tradeable date for the specified security each month</returns>
        public IDateRule MonthStart(string symbol)
        {
            return new MonthStartDateRule(GetSecurity(symbol));
        }

        /// <summary>
        /// Gets the security with the specified symbol, or throws an exception if the symbol is not found
        /// </summary>
        /// <param name="symbol">The security's symbol to search for</param>
        /// <returns>The security object matching the given symbol</returns>
        private Security GetSecurity(string symbol)
        {
            symbol = symbol.ToUpper();

            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new Exception(symbol + " not found in portfolio. Request this data when initializing the algorithm.");
            }
            return security;
        }
    }
}
