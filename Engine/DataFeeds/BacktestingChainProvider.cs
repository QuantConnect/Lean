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
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Base backtesting cache provider which will source symbols from local zip files
    /// </summary>
    public abstract class BacktestingChainProvider
    {
        /// <summary>
        /// The map file provider instance to use
        /// </summary>
        protected IMapFileProvider MapFileProvider { get; private set; }

        /// <summary>
        /// The history provider instance to use
        /// </summary>
        protected IHistoryProvider HistoryProvider { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestingChainProvider"/> class
        /// </summary>
        protected BacktestingChainProvider()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestingChainProvider"/> class
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        // TODO: This should be in the chain provider interfaces.
        // They might be even be unified in a single interface (futures and options chains providers)
        public void Initialize(ChainProviderInitializeParameters parameters)
        {
            HistoryProvider = parameters.HistoryProvider;
            MapFileProvider = parameters.MapFileProvider;
        }

        /// <summary>
        /// Get the contract symbols associated with the given canonical symbol and date
        /// </summary>
        /// <param name="canonicalSymbol">The canonical symbol</param>
        /// <param name="date">The date to search for</param>
        protected IEnumerable<Symbol> GetSymbols(Symbol canonicalSymbol, DateTime date)
        {
            var marketHoursDataBase = MarketHoursDatabase.FromDataFolder();
            var universeType = canonicalSymbol.SecurityType.IsOption() ? typeof(OptionUniverse) : typeof(FutureUniverse);
            // Use this GetEntry extension method since it's data type dependent, so we get the correct entry for the option universe
            var marketHoursEntry = marketHoursDataBase.GetEntry(canonicalSymbol, new[] { universeType });

            // We will add a safety measure in case the universe file for the current time is not available:
            // we will use the latest available universe file within the last 3 trading dates.
            // This is useful in cases like live trading when the algorithm is deployed at a time of day when
            // the universe file is not available yet.
            var history = (List<Slice>)null;
            var periods = 1;
            while ((history == null || history.Count == 0) && periods <= 3)
            {
                var startDate = Time.GetStartTimeForTradeBars(marketHoursEntry.ExchangeHours, date, Time.OneDay, periods++,
                    extendedMarketHours: false, marketHoursEntry.DataTimeZone);
                var request = new HistoryRequest(
                    startDate.ConvertToUtc(marketHoursEntry.ExchangeHours.TimeZone),
                    date.ConvertToUtc(marketHoursEntry.ExchangeHours.TimeZone),
                    universeType,
                    canonicalSymbol,
                    Resolution.Daily,
                    marketHoursEntry.ExchangeHours,
                    marketHoursEntry.DataTimeZone,
                    null,
                    false,
                    false,
                    DataNormalizationMode.Raw,
                    TickType.Quote);
                history = HistoryProvider.GetHistory([request], marketHoursEntry.DataTimeZone)?.ToList();
            }

            var symbols = history == null || history.Count == 0
                ? Enumerable.Empty<Symbol>()
                : history.Take(1).GetUniverseData().SelectMany(x => x.Values.Single()).Select(x => x.Symbol);

            if (canonicalSymbol.SecurityType.IsOption())
            {
                symbols = symbols.Where(symbol => symbol.SecurityType.IsOption());
            }

            return symbols.Where(symbol => symbol.ID.Date >= date.Date);
        }

        /// <summary>
        /// Helper method to determine if a contract is expired for the requested date
        /// </summary>
        protected static bool IsContractExpired(Symbol symbol, DateTime date)
        {
            return symbol.ID.Date.Date < date.Date;
        }
    }
}
