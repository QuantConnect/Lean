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

using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    /// <summary>
    /// Estimated annualized continuous dividend yield at given date
    /// </summary>
    public class DividendYieldProvider : IDividendYieldModel
    {
        private static DateTime _firstDividendYieldDate = Time.Start;
        private static decimal _lastDividendYield;
        private static List<Symbol> _symbols = new();
        private static Dictionary<Symbol, Dictionary<DateTime, decimal>> _dividendYieldRateProvider = new();
        private static readonly object _lock = new();

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
        /// Instantiates a <see cref="DividendYieldProvider"/> with the specified Symbol
        /// </summary>
        public DividendYieldProvider(Symbol symbol)
        {
            _symbols.Add(symbol);
            StartExpirationTask();
        }

        /// <summary>
        /// Helper method that will clear any cached dividend rate in a daily basis, this is useful for live trading
        /// </summary>
        protected virtual void StartExpirationTask()
        {
            lock (_lock)
            {
                // we clear the dividend yield rate cache so they are reloaded
                _dividendYieldRateProvider.Clear();
            }
            _ = Task.Delay(CacheRefreshPeriod).ContinueWith(_ => StartExpirationTask());
        }

        /// <summary>
        /// Lazily loads the dividend provider from disk and returns it
        /// </summary>
        private IReadOnlyDictionary<Symbol, Dictionary<DateTime, decimal>> DividendYieldRateProvider
        {
            get
            {
                // let's not lock if the provider is already loaded
                if (!_dividendYieldRateProvider.IsNullOrEmpty())
                {
                    return _dividendYieldRateProvider;
                }

                lock (_lock)
                {
                    LoadDividendYieldProvider();
                    return _dividendYieldRateProvider;
                }
            }
        }

        /// <summary>
        /// Get dividend yield by a given date of a given symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="date">The date</param>
        /// <returns>Dividend yield on the given date of the given symbol</returns>
        public decimal GetDividendYield(Symbol symbol, DateTime date)
        {
            if (!DividendYieldRateProvider.TryGetValue(symbol, out var symbolDividend))
            {
                // load the symbol factor if it is the first encounter
                LoadDividendYieldProvider(symbol);
                symbolDividend = DividendYieldRateProvider[symbol];
            }

            if (!symbolDividend.TryGetValue(date.Date, out var dividendYield))
            {
                return date < _firstDividendYieldDate
                    ? DefaultDividendYieldRate
                    : _lastDividendYield;
            }

            return dividendYield;
        }

        /// <summary>
        /// Generate the daily historical dividend yield
        /// </summary>
        private void LoadDividendYieldProvider()
        {
            foreach (var symbol in _symbols)
            {
                LoadDividendYieldProvider(symbol);
            }
        }

        /// <summary>
        /// Generate the daily historical dividend yield
        /// </summary>
        /// <remarks>Exposed for testing</remarks>
        protected virtual void LoadDividendYieldProvider(Symbol symbol)
        {
            var factorFileProvider = Composer.Instance.GetPart<IFactorFileProvider>();
            var corporateFactors = factorFileProvider
                .Get(symbol)
                .Select(factorRow => factorRow as CorporateFactorRow)
                .Where(corporateFactor => corporateFactor != null);

            var symbolDividends = FromCorporateFactorRow(corporateFactors);
            if (symbolDividends.Count == 0)
            {
                _dividendYieldRateProvider[symbol] = new();
                return;
            }

            // Sparse the discrete data points into continuous data for every day
            var previousDividendYield = DefaultDividendYieldRate;
            for (var date = _firstDividendYieldDate; date <= symbolDividends.Keys.Max(); date = date.AddDays(1))
            {
                if (!symbolDividends.TryGetValue(date, out var currentRate))
                {
                    symbolDividends[date] = previousDividendYield;
                    continue;
                }

                previousDividendYield = currentRate;
            }

            _dividendYieldRateProvider[symbol] = symbolDividends;
        }

        /// <summary>
        /// Returns a dictionary of historical dividend yield from collection of corporate factor rows
        /// </summary>
        /// <param name="corporateFactors">The corporate factor rows containing factor data</param>
        /// <returns>Dictionary of historical annualized continuous dividend yield data</returns>
        public static Dictionary<DateTime, decimal> FromCorporateFactorRow(IEnumerable<CorporateFactorRow> corporateFactors)
        {
            var dividendYieldProvider = new Dictionary<DateTime, decimal>();

            // calculate the dividend rate from each payout
            var subsequentRate = 0m;
            foreach (var row in corporateFactors.OrderByDescending(corporateFactor => corporateFactor.Date))
            {
                var dividendYield = 1 / row.PriceFactor - 1 - subsequentRate;
                dividendYieldProvider[row.Date] = dividendYield;
                subsequentRate = dividendYield;
            }

            // cumulative sum by year, since we'll use yearly payouts for estimation
            var yearlyDividendYieldProvider = new Dictionary<DateTime, decimal>();
            foreach (var date in dividendYieldProvider.Keys.OrderBy(x => x))
            {
                // 15 days window from 1y to avoid overestimation from last year value
                var yearlyDividend = dividendYieldProvider.Where(kvp => kvp.Key <= date && kvp.Key > date.AddDays(-350)).Sum(kvp => kvp.Value);
                // discrete to continuous: LN(1 + i)
                yearlyDividendYieldProvider[date] = Convert.ToDecimal(Math.Log(1d + (double)yearlyDividend));
            }

            if (yearlyDividendYieldProvider.Count == 0)
            {
                Log.Error($"DividendYieldProvider.FromCsvFile(): no dividend were loaded");
            }

            return yearlyDividendYieldProvider;
        }
    }
}
