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
using Python.Runtime;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Creates a universe based on an ETF's holdings at a given date
    /// </summary>
    public class ETFConstituentsUniverseFactory : ConstituentsUniverse<ETFConstituentUniverse>
    {
        private const string _etfConstituentsUniverseIdentifier = "qc-universe-etf-constituents";

        /// <summary>
        /// Creates a new universe for the constituents of the ETF provided as <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">The ETF to load constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="constituentsFilter">The filter function used to filter out ETF constituents from the universe</param>
        public ETFConstituentsUniverseFactory(Symbol symbol, UniverseSettings universeSettings, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> constituentsFilter = null)
            : base(CreateConstituentUniverseETFSymbol(symbol), universeSettings, constituentsFilter ?? (constituents => constituents.Select(c => c.Symbol)))
        {
        }

        /// <summary>
        /// Creates a new universe for the constituents of the ETF provided as <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">The ETF to load constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="constituentsFilter">The filter function used to filter out ETF constituents from the universe</param>
        public ETFConstituentsUniverseFactory(Symbol symbol, UniverseSettings universeSettings, PyObject constituentsFilter)
            : this(symbol, universeSettings, constituentsFilter.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>())
        {
        }

        /// <summary>
        /// Creates a universe Symbol for constituent ETFs
        /// </summary>
        /// <param name="compositeSymbol">The Symbol of the ETF</param>
        /// <returns>Universe Symbol with ETF set as underlying</returns>
        private static Symbol CreateConstituentUniverseETFSymbol(Symbol compositeSymbol)
        {
            var guid = Guid.NewGuid().ToString();
            var universeTicker = _etfConstituentsUniverseIdentifier + '-' + guid;

            return new Symbol(
                SecurityIdentifier.GenerateConstituentIdentifier(
                    universeTicker,
                    compositeSymbol.SecurityType,
                    compositeSymbol.ID.Market),
                universeTicker,
                compositeSymbol);
        }
    }
}
