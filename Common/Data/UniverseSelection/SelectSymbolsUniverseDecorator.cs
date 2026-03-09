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

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides a univese decoration that replaces the implementation of <see cref="SelectSymbols"/>
    /// </summary>
    public class SelectSymbolsUniverseDecorator : UniverseDecorator
    {
        private readonly SelectSymbolsDelegate _selectSymbols;

        /// <summary>
        /// Delegate type for the <see cref="SelectSymbols"/> method
        /// </summary>
        /// <param name="utcTime">The current utc frontier time</param>
        /// <param name="data">The universe selection data</param>
        /// <returns>The symbols selected by the universe</returns>
        public delegate IEnumerable<Symbol> SelectSymbolsDelegate(DateTime utcTime, BaseDataCollection data);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectSymbolsUniverseDecorator"/> class
        /// </summary>
        /// <param name="universe">The universe to be decorated</param>
        /// <param name="selectSymbols">The new implementation of <see cref="SelectSymbols"/></param>
        public SelectSymbolsUniverseDecorator(Universe universe, SelectSymbolsDelegate selectSymbols)
            : base(universe)
        {
            _selectSymbols = selectSymbols;
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return _selectSymbols(utcTime, data);
        }
    }
}