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
using QuantConnect.Logging;
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
        // see https://github.com/QuantConnect/Lean/issues/6384
        private static readonly TickType[] DataTypes = new[] { TickType.Quote, TickType.OpenInterest, TickType.Trade };
        private static readonly Resolution[] Resolutions = new[] { Resolution.Minute, Resolution.Hour, Resolution.Daily };
        private bool _loggedPreviousTradableDate;

        /// <summary>
        /// The data cache instance to use
        /// </summary>
        protected IDataCacheProvider DataCacheProvider { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected BacktestingChainProvider(IDataCacheProvider dataCacheProvider)
        {
            DataCacheProvider = dataCacheProvider;
        }

        /// <summary>
        /// Get the contract symbols associated with the given canonical symbol and date
        /// </summary>
        /// <param name="canonicalSymbol">The canonical symbol</param>
        /// <param name="date">The date to search for</param>
        protected IEnumerable<Symbol> GetSymbols(Symbol canonicalSymbol, DateTime date)
        {
            // TODO: This will be removed when all chains (including Futures and FOPs) are file-based instead of zip-entry based
            if (canonicalSymbol.SecurityType.IsOption())
            {
                return GetOptionSymbols(canonicalSymbol, date);
            }

            return GetFutureSymbols(canonicalSymbol, date);
        }

        private static IEnumerable<Symbol> GetOptionSymbols(Symbol canonicalSymbol, DateTime date)
        {
            return GetChainSymbols(canonicalSymbol, date).Where(symbol => symbol.SecurityType.IsOption());
        }

        private static IEnumerable<Symbol> GetFutureSymbols(Symbol canonicalSymbol, DateTime date)
        {
            return GetChainSymbols(canonicalSymbol, date);
        }

        private static IEnumerable<Symbol> GetChainSymbols(Symbol canonicalSymbol, DateTime date)
        {
            var historyProvider = Composer.Instance.GetPart<IHistoryProvider>();
            var marketHoursDataBase = MarketHoursDatabase.FromDataFolder();
            var universeType = canonicalSymbol.SecurityType.IsOption() ? typeof(OptionUniverse) : typeof(FutureUniverse);
            // Use this GetEntry extension method since it's data type dependent, so we get the correct entry for the option universe
            var marketHoursEntry = marketHoursDataBase.GetEntry(canonicalSymbol, new[] { universeType });

            var previousTradingDate = Time.GetStartTimeForTradeBars(marketHoursEntry.ExchangeHours, date, Time.OneDay, 1,
                extendedMarketHours: false, marketHoursEntry.DataTimeZone);
            var request = new HistoryRequest(
                previousTradingDate.ConvertToUtc(marketHoursEntry.ExchangeHours.TimeZone),
                date.ConvertToUtc(marketHoursEntry.ExchangeHours.TimeZone),
                universeType,
                canonicalSymbol,
                Resolution.Daily,
                marketHoursEntry.ExchangeHours,
                marketHoursEntry.DataTimeZone,
                Resolution.Daily,
                false,
                false,
                DataNormalizationMode.Raw,
                TickType.Quote);
            var history = historyProvider.GetHistory(new[] { request }, marketHoursEntry.DataTimeZone).ToList();

            if (history == null || history.Count == 0)
            {
                return Enumerable.Empty<Symbol>();
            }

            return history.GetUniverseData().SelectMany(x => x.Values.Single()).Select(x => x.Symbol);
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
