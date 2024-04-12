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
using System.Linq;
using Python.Runtime;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
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
        /// Gets a helper that provides methods for creating universes based on daily dollar volumes
        /// </summary>
        public DollarVolumeUniverseDefinitions DollarVolume { get; set; }

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
            DollarVolume = new DollarVolumeUniverseDefinitions(algorithm);
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
            string market,
            UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            market ??= _algorithm.BrokerageModel.DefaultMarkets.TryGetValue(SecurityType.Equity, out var defaultMarket)
                ? defaultMarket
                : throw new Exception("No default market set for security type: Equity");

            var etfSymbol = new Symbol(
                SecurityIdentifier.GenerateEquity(
                    etfTicker,
                    market,
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
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(string etfTicker, string market, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return ETF(etfTicker, market, null, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="etfTicker"/>
        /// </summary>
        /// <param name="etfTicker">Ticker of the ETF to get constituents for</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(string etfTicker, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return ETF(etfTicker, null, null, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="etfTicker"/>
        /// </summary>
        /// <param name="etfTicker">Ticker of the ETF to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(
            string etfTicker,
            UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return ETF(etfTicker, null, universeSettings, universeFilterFunc);
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
            return ETF(etfTicker, market, universeSettings, universeFilterFunc?.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>());
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="etfTicker"/>
        /// </summary>
        /// <param name="etfTicker">Ticker of the ETF to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(
            string etfTicker,
            UniverseSettings universeSettings,
            PyObject universeFilterFunc)
        {
            return ETF(etfTicker, null, universeSettings, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided ETF <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">ETF Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(Symbol symbol, UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return new ETFConstituentsUniverseFactory(symbol, universeSettings ?? _algorithm.UniverseSettings, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided ETF <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">ETF Symbol to get constituents for</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(Symbol symbol, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return ETF(symbol, null, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided ETF <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">ETF Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New ETF constituents Universe</returns>
        public Universe ETF(Symbol symbol, UniverseSettings universeSettings = null, PyObject universeFilterFunc = null)
        {
            return ETF(symbol, universeSettings ?? _algorithm.UniverseSettings,
                universeFilterFunc?.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>());
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexTicker"/>
        /// </summary>
        /// <param name="indexTicker">Ticker of the index to get constituents for</param>
        /// <param name="market">Market of the index</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(string indexTicker, string market, UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            market ??= _algorithm.BrokerageModel.DefaultMarkets.TryGetValue(SecurityType.Index, out var defaultMarket)
                ? defaultMarket
                : throw new Exception("No default market set for security type: Index");

            return Index(
                Symbol.Create(indexTicker, SecurityType.Index, market),
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
        public Universe Index(string indexTicker, string market, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return Index(indexTicker, market, null, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexTicker"/>
        /// </summary>
        /// <param name="indexTicker">Ticker of the index to get constituents for</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(string indexTicker, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return Index(indexTicker, null, null, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexTicker"/>
        /// </summary>
        /// <param name="indexTicker">Ticker of the index to get constituents for</param>
        /// <param name="market">Market of the index</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(string indexTicker, UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return Index(indexTicker, null, universeSettings, universeFilterFunc);
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
            return Index(indexTicker, market, universeSettings, universeFilterFunc?.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>());
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexTicker"/>
        /// </summary>
        /// <param name="indexTicker">Ticker of the index to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(
            string indexTicker,
            UniverseSettings universeSettings,
            PyObject universeFilterFunc)
        {
            return Index(indexTicker, null, universeSettings, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexSymbol"/>
        /// </summary>
        /// <param name="indexSymbol">Index Symbol to get constituents for</param>
        /// <param name="universeSettings">Universe settings</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(Symbol indexSymbol, UniverseSettings universeSettings,
            Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return new ETFConstituentsUniverseFactory(indexSymbol, universeSettings, universeFilterFunc);
        }

        /// <summary>
        /// Creates a universe for the constituents of the provided <paramref name="indexSymbol"/>
        /// </summary>
        /// <param name="indexSymbol">Index Symbol to get constituents for</param>
        /// <param name="universeFilterFunc">Function to filter universe results</param>
        /// <returns>New index constituents Universe</returns>
        public Universe Index(Symbol indexSymbol, Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> universeFilterFunc)
        {
            return Index(indexSymbol, null, universeFilterFunc);
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
            return Index(indexSymbol, universeSettings ?? _algorithm.UniverseSettings,
                universeFilterFunc?.ConvertPythonUniverseFilterFunction<ETFConstituentUniverse>());
        }

        /// <summary>
        /// Creates a new fine universe that contains the constituents of QC500 index based onthe company fundamentals
        /// The algorithm creates a default tradable and liquid universe containing 500 US equities
        /// which are chosen at the first trading day of each month.
        /// </summary>
        /// <returns>A new coarse universe for the top count of stocks by dollar volume</returns>
        public Universe QC500
        {
            get
            {
                return ETF(Symbol.Create("SPY", SecurityType.Equity, Market.USA));
            }
        }

        /// <summary>
        /// Creates a new coarse universe that contains the top count of stocks
        /// by daily dollar volume
        /// </summary>
        /// <param name="count">The number of stock to select</param>
        /// <param name="universeSettings">The settings for stocks added by this universe.
        /// Defaults to <see cref="QCAlgorithm.UniverseSettings"/></param>
        /// <returns>A new coarse universe for the top count of stocks by dollar volume</returns>
        public Universe Top(int count, UniverseSettings universeSettings = null)
        {
            universeSettings ??= _algorithm.UniverseSettings;

            var symbol = Symbol.Create("us-equity-dollar-volume-top-" + count, SecurityType.Equity, Market.USA);
            return FundamentalUniverse.USA(selectionData => (
                from c in selectionData
                orderby c.DollarVolume descending
                select c.Symbol).Take(count),
                universeSettings);
        }
    }
}
