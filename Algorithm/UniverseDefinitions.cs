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
using System.Collections;
using System.Collections.Generic;
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for defining universes in algorithms
    /// </summary>
    public class UniverseDefinitions
    {
        private readonly QCAlgorithm _algorithm;
        
        /// <summary>
        /// Specifies that universe selection should not make changes on this iteration
        /// </summary>
        public Universe.UnchangedUniverse Unchanged => Universe.Unchanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseDefinitions"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for obtaining the default <see cref="UniverseSettings"/></param>
        public UniverseDefinitions(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }
        
        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="etfTicker"/>
        /// </summary>
        /// <param name="etfTicker">Ticker of the ETF to get constituents for</param>
        /// <param name="market">Market of the ETF</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(
            string etfTicker, 
            string market = null, 
            UniverseSettings universeSettings = null, 
            Func<IEnumerable<ETFConstituentData>, IEnumerable<Symbol>> universeFilterFunc = null)
        {
            var etfSymbol = new Symbol(
                SecurityIdentifier.GenerateEquity(
                    etfTicker,
                    market ?? Market.USA,
                    true,
                    mappingResolveDate: _algorithm.Time.Date),
                etfTicker);

            return ETF(etfSymbol, universeSettings, universeFilterFunc);
        }


        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="etfTicker"/>
        /// </summary>
        /// <param name="etfTicker">Ticker of the ETF to get constituents for</param>
        /// <param name="market">Market of the ETF</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(
            string etfTicker,
            string market = null,
            UniverseSettings universeSettings = null,
            PyObject universeFilterFunc = null)
        {
            return ETF(etfTicker, market, universeSettings, universeFilterFunc.ConvertPythonUniverseFilterFunction<ETFConstituentData>());
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided ETF <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">ETF Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(
            Symbol symbol,
            UniverseSettings universeSettings = null, 
            Func<IEnumerable<ETFConstituentData>, IEnumerable<Symbol>> universeFilterFunc = null)
        {
            return new ETFConstituentsUniverse(symbol, universeSettings ?? _algorithm.UniverseSettings, universeFilterFunc);
        }
        
        /// <summary>
        /// Creates a universe for the constituents of the provided ETF <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">ETF Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(
            Symbol symbol,
            UniverseSettings universeSettings = null, 
            PyObject universeFilterFunc = null)
        {
            return ETF(symbol, universeSettings, universeFilterFunc.ConvertPythonUniverseFilterFunction<ETFConstituentData>());
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexTicker"/>
        /// </summary>
        /// <param name="indexTicker">Ticker of the index to get constituents for</param>
        /// <param name="market">Market of the index</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(
            string indexTicker,
            string market = null,
            UniverseSettings universeSettings = null,
            Func<IEnumerable<ETFConstituentData>, IEnumerable<Symbol>> universeFilterFunc = null)
        {
            return Index(
                Symbol.Create(indexTicker, SecurityType.Index, market ?? Market.USA),
                universeSettings, 
                universeFilterFunc);
        }
                
        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexTicker"/>
        /// </summary>
        /// <param name="indexTicker">Ticker of the index to get constituents for</param>
        /// <param name="market">Market of the index</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(
            string indexTicker,
            string market = null,
            UniverseSettings universeSettings = null,
            PyObject universeFilterFunc = null)
        {
            return Index(indexTicker, market, universeSettings, universeFilterFunc.ConvertPythonUniverseFilterFunction<ETFConstituentData>());
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexSymbol"/>
        /// </summary>
        /// <param name="indexSymbol">Index Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(
            Symbol indexSymbol,
            UniverseSettings universeSettings = null,
            Func<IEnumerable<ETFConstituentData>, IEnumerable<Symbol>> universeFilterFunc = null)
        {
            return new ETFConstituentsUniverse(indexSymbol, universeSettings ?? _algorithm.UniverseSettings, universeFilterFunc);
        }
        
        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexSymbol"/>
        /// </summary>
        /// <param name="indexSymbol">Index Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(
            Symbol indexSymbol,
            UniverseSettings universeSettings = null,
            PyObject universeFilterFunc = null)
        {
            return Index(indexSymbol, universeSettings, universeFilterFunc.ConvertPythonUniverseFilterFunction<ETFConstituentData>());
        }
    }
}
