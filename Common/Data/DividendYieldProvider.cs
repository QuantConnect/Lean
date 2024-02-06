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

using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.Data
{
    /// <summary>
    /// Estimated annualized continuous dividend yield at given date
    /// </summary>
    public class DividendYieldProvider : IDividendYieldModel
    {
        private Symbol _symbol;
        private static DateTime _firstDividendYieldDate = new DateTime(1998, 1, 1);
        private static DateTime _lastDividendYieldDate;
        private static Dictionary<DateTime, decimal> _dividendYieldRateProvider;
        private static readonly object _lock = new();

        /// <summary>
        /// Default no dividend payout
        /// </summary>
        public static readonly decimal DefaultDividendYieldRate = 0.0m;

        /// <summary>
        /// Instantiates a <see cref="DividendYieldProvider"/> with the specified Symbol
        /// </summary>
        public DividendYieldProvider(Symbol symbol)
        {
            _symbol = symbol;
        }

        /// <summary>
        /// Lazily loads the dividend provider from disk and returns it
        /// </summary>
        private IReadOnlyDictionary<DateTime, decimal> DividendYieldRateProvider
        {
            get
            {
                // let's not lock if the provider is already loaded
                if (_dividendYieldRateProvider != null)
                {
                    return _dividendYieldRateProvider;
                }

                lock (_lock)
                {
                    if (_dividendYieldRateProvider == null)
                    {
                        LoadDividendYieldProvider();
                    }
                    return _dividendYieldRateProvider;
                }
            }
        }

        /// <summary>
        /// Get dividend yield by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Dividend yield on the given date</returns>
        public decimal GetDividendYield(DateTime date)
        {
            if (!DividendYieldRateProvider.TryGetValue(date.Date, out var dividendYield))
            {
                return date < _firstDividendYieldDate
                    ? DefaultDividendYieldRate
                    : DividendYieldRateProvider[_lastDividendYieldDate];
            }

            return dividendYield;
        }

        /// <summary>
        /// Generate the daily historical dividend yield
        /// </summary>
        protected void LoadDividendYieldProvider()
        {
            var directory = Path.Combine(Globals.DataFolder, "equity", "usa", "factor_files", $"{_symbol.Value.ToLowerInvariant()}.csv");
            _dividendYieldRateProvider = FromCsvFile(directory);

            _lastDividendYieldDate = DateTime.UtcNow.Date;

            // Sparse the discrete data points into continuous data for every day
            var previousDividendYield = DefaultDividendYieldRate;
            for (var date = _firstDividendYieldDate; date <= _lastDividendYieldDate; date = date.AddDays(1))
            {
                if (!_dividendYieldRateProvider.TryGetValue(date, out var currentRate))
                {
                    _dividendYieldRateProvider[date] = previousDividendYield;
                    continue;
                }

                previousDividendYield = currentRate;
            }
        }

        /// <summary>
        /// Reads factor data and returns a dictionary of historical dividend yield
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <returns>Dictionary of historical annualized continuous dividend yield data</returns>
        public static Dictionary<DateTime, decimal> FromCsvFile(string file)
        {
            var dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(
                Config.Get("data-provider", "DefaultDataProvider"));

            // skip the first header line, also skip #'s as these are comment lines
            var dividendYieldProvider = new Dictionary<DateTime, decimal>();
            var lines = dataProvider.ReadLines(file).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            // calculate the dividend rate from each payout
            var previousRate = 0m;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (TryParse(lines[i], previousRate, out var date, out var dividendYield))
                {
                    dividendYieldProvider[date] = dividendYield;
                    previousRate = dividendYield;
                }
            }

            // cumulative sum by year, since we'll use yearly payouts for estimation
            var yearlyDividendYieldProvider = new Dictionary<DateTime, decimal>();
            foreach (var date in dividendYieldProvider.Keys.OrderBy(x => x))
            {
                // 15 days window from 1y to avoid overestimation from last year value
                var yearlyDividend = dividendYieldProvider.Where(kvp => kvp.Key <= date && kvp.Key > date.AddDays(-350)).Sum(kvp => kvp.Value);
                yearlyDividendYieldProvider[date] = Convert.ToDecimal(Math.Log(1d + (double)yearlyDividend));
            }

            if (yearlyDividendYieldProvider.Count == 0)
            {
                Log.Error($"DividendYieldProvider.FromCsvFile(): no dividend were loaded, please make sure the file is present '{file}'");
            }

            return yearlyDividendYieldProvider;
        }

        /// <summary>
        /// Parse the string into the factoring date and value
        /// </summary>
        /// <param name="csvLine">The csv line to be parsed</param>
        /// <param name="nextPayouts">Dividend payout rate of all subsequent payouts</param>
        /// <param name="date">Parsed dividend date</param>
        /// <param name="dividendYield">Parsed dividend value</param>
        public static bool TryParse(string csvLine, decimal nextPayouts, out DateTime date, out decimal dividendYield)
        {
            var line = csvLine.Split(',');

            if (!DateTime.TryParseExact(line[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Log.Error($"Couldn't parse date/time while reading factor file. Line: {csvLine}");
                dividendYield = DefaultDividendYieldRate;
                return false;
            }

            if (!decimal.TryParse(line[1], NumberStyles.Any, CultureInfo.InvariantCulture, out dividendYield))
            {
                Log.Error($"Couldn't parse discounted shares multiplier while reading factor file. Line: {csvLine}");
                return false;
            }

            // payout rate
            dividendYield = 1 / dividendYield - 1 - nextPayouts;
            return true;
        }
    }
}
