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
        private readonly Func<string, SecurityType, string, string, IEnumerable<Symbol>> _lookupSymbolsFunction;
        private readonly Func<SecurityType, bool> _canAdvanceTimeFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataQueueHandlerUniverseProvider"/> class
        /// </summary>
        /// <param name="getNextTicksFunction">The functional implementation for the <see cref="FuncDataQueueHandler.GetNextTicks"/> function</param>
        /// <param name="lookupSymbolsFunction">The functional implementation for the <see cref="IDataQueueUniverseProvider.LookupSymbols"/> function</param>
        /// <param name="canAdvanceTimeFunction">The functional implementation for the <see cref="IDataQueueUniverseProvider.CanAdvanceTime"/> function</param>
        public FuncDataQueueHandlerUniverseProvider(
            Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction,
            Func<string, SecurityType, string, string, IEnumerable<Symbol>> lookupSymbolsFunction,
            Func<SecurityType, bool> canAdvanceTimeFunction)
            : base(getNextTicksFunction)
        {
            _lookupSymbolsFunction = lookupSymbolsFunction;
            _canAdvanceTimeFunction = canAdvanceTimeFunction;
        }

        /// <summary>
        /// Method returns a collection of Symbols that are available at the data source.
        /// </summary>
        /// <param name="lookupName">String representing the name to lookup</param>
        /// <param name="securityType">Expected security type of the returned symbols (if any)</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <param name="securityExchange">Expected security exchange name(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, string securityCurrency = null, string securityExchange = null)
        {
            return _lookupSymbolsFunction(lookupName, securityType, securityCurrency, securityExchange);
        }

        /// <summary>
        /// Returns whether the time can be advanced or not.
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>true if the time can be advanced</returns>
        public bool CanAdvanceTime(SecurityType securityType)
        {
            return _canAdvanceTimeFunction(securityType);
        }
    }
}