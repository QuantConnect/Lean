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
using System.Linq;
using QuantConnect.Util;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Data
{
    /// <summary>
    /// Estimated annualized continuous dividend yield at given date
    /// </summary>
    public class DividendYieldProvider : IDividendYieldModel
    {
        private static MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        /// <summary>
        /// The default symbol to use as a dividend yield provider
        /// </summary>
        /// <remarks>This is useful for index and future options which do not have an underlying that yields dividends.
        /// Defaults to SPY</remarks>
        public static Symbol DefaultSymbol { get; set; } = Symbol.Create("SPY", SecurityType.Equity, QuantConnect.Market.USA);

        /// <summary>
        /// The dividends by symbol
        /// </summary>
        protected static Dictionary<Symbol, List<BaseData>> _corporateEventsCache;

        /// <summary>
        /// Task to clear the cache
        /// </summary>
        protected static Task _cacheClearTask;
        private static readonly object _lock = new();

        private readonly Symbol _symbol;
        private readonly SecurityExchangeHours _exchangeHours;

        /// <summary>
        /// Default no dividend payout
        /// </summary>
        public static readonly decimal DefaultDividendYieldRate = 0.0m;

        /// <summary>
        /// The cached refresh period for the dividend yield rate
        /// </summary>
        /// <remarks>Exposed for testing</remarks>
        protected virtual TimeSpan CacheRefreshPeriod
        {
            get
            {
                var dueTime = Time.GetNextLiveAuxiliaryDataDueTime();
                if (dueTime > TimeSpan.FromMinutes(10))
                {
                    // Clear the cache before the auxiliary due time to avoid race conditions with consumers
                    return dueTime - TimeSpan.FromMinutes(10);
                }
                return dueTime;
            }
        }

        /// <summary>
        /// Creates a new instance using the default symbol
        /// </summary>
        public DividendYieldProvider() : this(DefaultSymbol)
        {
        }

        /// <summary>
        /// Instantiates a <see cref="DividendYieldProvider"/> with the specified Symbol
        /// </summary>
        public DividendYieldProvider(Symbol symbol)
        {
            _symbol = symbol;
            _exchangeHours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.ID.SecurityType);

            if (_cacheClearTask == null)
            {
                lock (_lock)
                {
                    // only the first triggers the expiration task check
                    if (_cacheClearTask == null)
                    {
                        StartExpirationTask(CacheRefreshPeriod);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new instance for the given option symbol
        /// </summary>
        public static DividendYieldProvider CreateForOption(Symbol optionSymbol)
        {
            if (!optionSymbol.SecurityType.IsOption() || optionSymbol.SecurityType == SecurityType.Option)
            {
                return new DividendYieldProvider(optionSymbol.Underlying);
            }

            return new DividendYieldProvider();
        }

        /// <summary>
        /// Helper method that will clear any cached dividend rate in a daily basis, this is useful for live trading
        /// </summary>
        private static void StartExpirationTask(TimeSpan cacheRefreshPeriod)
        {
            lock (_lock)
            {
                // we clear the dividend yield rate cache so they are reloaded
                _corporateEventsCache = new();
            }
            _cacheClearTask = Task.Delay(cacheRefreshPeriod).ContinueWith(_ => StartExpirationTask(cacheRefreshPeriod));
        }

        /// <summary>
        /// Get dividend yield by a given date of a given symbol.
        /// It will get the dividend yield at the time of the most recent dividend since no price is provided.
        /// In order to get more accurate dividend yield, provide the security price at the given date to
        /// the <see cref="GetDividendYield(DateTime, decimal)"/> or <see cref="GetDividendYield(IBaseData)"/> methods.
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Dividend yield on the given date of the given symbol</returns>
        public decimal GetDividendYield(DateTime date)
        {
            return GetDividendYieldImpl(date, null);
        }

        /// <summary>
        /// Gets the dividend yield at the date of the specified data, using the data price as the security price
        /// </summary>
        /// <param name="priceData">Price data instance</param>
        /// <returns>Dividend yield on the given date of the given symbol</returns>
        /// <remarks>Price data must be raw (<see cref="DataNormalizationMode.Raw"/>)</remarks>
        public decimal GetDividendYield(IBaseData priceData)
        {
            if (priceData.Symbol != _symbol)
            {
                throw new ArgumentException($"Trying to get {priceData.Symbol} dividend yield using the {_symbol} dividend yield provider.");
            }

            return GetDividendYield(priceData.EndTime, priceData.Value);
        }

        /// <summary>
        /// Get dividend yield at given date and security price
        /// </summary>
        /// <param name="date">The date</param>
        /// <param name="securityPrice">The security price at the given date</param>
        /// <returns>Dividend yield on the given date of the given symbol</returns>
        /// <remarks>Price data must be raw (<see cref="DataNormalizationMode.Raw"/>)</remarks>
        public decimal GetDividendYield(DateTime date, decimal securityPrice)
        {
            return GetDividendYieldImpl(date, securityPrice);
        }

        /// <summary>
        /// Get dividend yield at given date and security price.
        /// </summary>
        /// <remarks>
        /// <paramref name="securityPrice"/> is nullable for backwards compatibility, so <see cref="GetDividendYield(DateTime)"/> is usable.
        /// If dividend yield is requested at a given date without a price, the dividend yield at the time of the most recent dividend is returned.
        /// Price data must be raw (<see cref="DataNormalizationMode.Raw"/>).
        /// </remarks>
        private decimal GetDividendYieldImpl(DateTime date, decimal? securityPrice)
        {
            List<BaseData> symbolCorporateEvents;
            lock (_lock)
            {
                if (!_corporateEventsCache.TryGetValue(_symbol, out symbolCorporateEvents))
                {
                    // load the symbol factor if it is the first encounter
                    symbolCorporateEvents = _corporateEventsCache[_symbol] = LoadCorporateEvents(_symbol);
                }
            }

            if (symbolCorporateEvents == null)
            {
                return DefaultDividendYieldRate;
            }

            // We need both corporate event types, so we get the most recent one, either dividend or split
            var mostRecentCorporateEventIndex = symbolCorporateEvents.FindLastIndex(x => x.EndTime <= date.Date);
            if (mostRecentCorporateEventIndex == -1)
            {
                return DefaultDividendYieldRate;
            }

            // Now we get the most recent dividend in order to get the end of the trailing twelve months period for the dividend yield
            var mostRecentCorporateEvent = symbolCorporateEvents[mostRecentCorporateEventIndex];
            var mostRecentDividend = mostRecentCorporateEvent as Dividend;
            if (mostRecentDividend == null)
            {
                for (var i = mostRecentCorporateEventIndex - 1; i >= 0; i--)
                {
                    if (symbolCorporateEvents[i] is Dividend dividend)
                    {
                        mostRecentDividend = dividend;
                        break;
                    }
                }
            }

            // If there is no dividend in the past year, we return the default dividend yield rate
            if (mostRecentDividend == null)
            {
                return DefaultDividendYieldRate;
            }

            securityPrice ??= mostRecentDividend.ReferencePrice;
            if (securityPrice == 0)
            {
                throw new ArgumentException("Security price cannot be zero.");
            }

            // The dividend yield is the sum of the dividends in the past year (ending in the most recent dividend date,
            // not on the price quote date) divided by the last close price:

            // 15 days window from 1y to avoid overestimation from last year value
            var trailingYearStartDate = mostRecentDividend.EndTime.AddDays(-350);

            var yearlyDividend = 0m;
            var currentSplitFactor = 1m;
            for (var i = mostRecentCorporateEventIndex; i >= 0; i--)
            {
                var corporateEvent = symbolCorporateEvents[i];
                if (corporateEvent.EndTime < trailingYearStartDate)
                {
                    break;
                }

                if (corporateEvent is Dividend dividend)
                {
                    yearlyDividend += dividend.Distribution * currentSplitFactor;
                }
                else
                {
                    // Update the split factor to adjust the dividend value per share
                    currentSplitFactor *= ((Split)corporateEvent).SplitFactor;
                }
            }

            return yearlyDividend / securityPrice.Value;
        }

        /// <summary>
        /// Generate the corporate events from the corporate factor file for the specified symbol
        /// </summary>
        /// <remarks>Exposed for testing</remarks>
        protected virtual List<BaseData> LoadCorporateEvents(Symbol symbol)
        {
            var factorFileProvider = Composer.Instance.GetPart<IFactorFileProvider>();
            var corporateFactors = factorFileProvider
                .Get(symbol)
                .Select(factorRow => factorRow as CorporateFactorRow)
                .Where(corporateFactor => corporateFactor != null);

            var symbolCorporateEvents = FromCorporateFactorRows(corporateFactors, symbol).ToList();
            if (symbolCorporateEvents.Count == 0)
            {
                return null;
            }

            return symbolCorporateEvents;
        }

        /// <summary>
        /// Generates the splits and dividends from the corporate factor rows
        /// </summary>
        private IEnumerable<BaseData> FromCorporateFactorRows(IEnumerable<CorporateFactorRow> corporateFactors, Symbol symbol)
        {
            var dividends = new List<Dividend>();

            // Get all dividends from the corporate actions
            var rows = corporateFactors.OrderBy(corporateFactor => corporateFactor.Date).ToArray();
            for (var i = 0; i < rows.Length - 1; i++)
            {
                var row = rows[i];
                var nextRow = rows[i + 1];
                if (row.PriceFactor != nextRow.PriceFactor)
                {
                    yield return row.GetDividend(nextRow, symbol, _exchangeHours, decimalPlaces: 3);
                }
                else
                {
                    yield return row.GetSplit(nextRow, symbol, _exchangeHours);
                }

            }
        }
    }
}
