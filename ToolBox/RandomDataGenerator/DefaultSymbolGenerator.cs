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
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random <see cref="Symbol"/> object of the specified security type.
    /// All returned symbols have a matching entry in the Symbol properties database.
    /// </summary>
    /// <remarks>
    /// A valid implementation will keep track of generated Symbol objects to ensure duplicates
    /// are not generated.
    /// </remarks>
    public class DefaultSymbolGenerator : BaseSymbolGenerator
    {
        private readonly string _market;
        private readonly SecurityType _securityType;

        /// <summary>
        /// Creates <see cref="DefaultSymbolGenerator"/> instance
        /// </summary>
        /// <param name="settings">random data generation run settings</param>
        /// <param name="random">produces random values for use in random data generation</param>
        public DefaultSymbolGenerator(
            RandomDataGeneratorSettings settings,
            IRandomValueGenerator random
        )
            : base(settings, random)
        {
            _market = settings.Market;
            _securityType = settings.SecurityType;
        }

        /// <summary>
        /// Generates a single-item list at a time using base random implementation
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Symbol> GenerateAsset(string ticker = null)
        {
            yield return NextSymbol(Settings.SecurityType, Settings.Market, ticker);
        }

        /// <summary>
        /// Returns the number of symbols with the specified parameters can be generated.
        /// Returns int.MaxValue if there is no limit for the given parameters.
        /// </summary>
        /// <returns>The number of available symbols for the given parameters, or int.MaxValue if no limit</returns>
        public override int GetAvailableSymbolCount()
        {
            // check the Symbol properties database to determine how many symbols we can generate
            // if there is a wildcard entry, we can generate as many symbols as we want
            // if there is no wildcard entry, we can only generate as many symbols as there are entries
            return SymbolPropertiesDatabase.ContainsKey(
                _market,
                SecurityDatabaseKey.Wildcard,
                _securityType
            )
                ? int.MaxValue
                : SymbolPropertiesDatabase.GetSymbolPropertiesList(_market, _securityType).Count();
        }
    }
}
