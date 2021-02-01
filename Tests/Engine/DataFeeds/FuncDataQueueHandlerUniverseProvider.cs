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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataQueueHandler"/> and <see cref="IDataQueueUniverseProvider"/>
    /// that can be specified via functions
    /// </summary>
    public class FuncDataQueueHandlerUniverseProvider : FuncDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly Func<Symbol, bool, string, IEnumerable<Symbol>> _lookupSymbolsFunction;
        private readonly Func<bool> _canPerformSelectionFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataQueueHandlerUniverseProvider"/> class
        /// </summary>
        /// <param name="getNextTicksFunction">The functional implementation for the <see cref="FuncDataQueueHandler.GetNextTicks"/> function</param>
        /// <param name="lookupSymbolsFunction">The functional implementation for the <see cref="IDataQueueUniverseProvider.LookupSymbols"/> function</param>
        /// <param name="canPerformSelectionFunction">The functional implementation for the <see cref="IDataQueueUniverseProvider.CanPerformSelection"/> function</param>
        /// <param name="timeProvider">The time provider instance to use</param>
        public FuncDataQueueHandlerUniverseProvider(
            Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction,
            Func<Symbol, bool, string, IEnumerable<Symbol>> lookupSymbolsFunction,
            Func<bool> canPerformSelectionFunction,
            ITimeProvider timeProvider)
            : base(getNextTicksFunction, timeProvider)
        {
            _lookupSymbolsFunction = lookupSymbolsFunction;
            _canPerformSelectionFunction = canPerformSelectionFunction;
        }

        /// <summary>
        /// Method returns a collection of Symbols that are available at the data source.
        /// </summary>
        /// <param name="symbol">Symbol to lookup</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
        {
            return _lookupSymbolsFunction(symbol, includeExpired, securityCurrency);
        }

        /// <summary>
        /// Returns whether selection can take place or not.
        /// </summary>
        /// <returns>True if selection can take place</returns>
        public bool CanPerformSelection()
        {
            return _canPerformSelectionFunction();
        }
    }
}
