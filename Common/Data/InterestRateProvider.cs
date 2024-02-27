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
    /// Fed US Primary Credit Rate at given date
    /// </summary>
    public class InterestRateProvider : IRiskFreeInterestRateModel
    {
        private static readonly DateTime _firstInterestRateDate = new DateTime(1998, 1, 1);
        private static DateTime _lastInterestRateDate;
        private static Dictionary<DateTime, decimal> _riskFreeRateProvider;
        private static readonly object _lock = new();

        /// <summary>
        /// Default Risk Free Rate of 1%
        /// </summary>
        public static readonly decimal DefaultRiskFreeRate = 0.01m;

        /// <summary>
        /// Lazily loads the interest rate provider from disk and returns it
        /// </summary>
        private IReadOnlyDictionary<DateTime, decimal> RiskFreeRateProvider
        {
            get
            {
                // let's not lock if the provider is already loaded
                if (_riskFreeRateProvider != null)
                {
                    return _riskFreeRateProvider;
                }

                lock (_lock)
                {
                    if (_riskFreeRateProvider == null)
                    {
                        LoadInterestRateProvider();
                    }
                    return _riskFreeRateProvider;
                }
            }
        }

        /// <summary>
        /// Get interest rate by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Interest rate on the given date</returns>
        public decimal GetInterestRate(DateTime date)
        {
            if (!RiskFreeRateProvider.TryGetValue(date.Date, out var interestRate))
            {
                return date < _firstInterestRateDate
                    ? RiskFreeRateProvider[_firstInterestRateDate]
                    : RiskFreeRateProvider[_lastInterestRateDate];
            }

            return interestRate;
        }

        /// <summary>
        /// Generate the daily historical US primary credit rate
        /// </summary>
        protected void LoadInterestRateProvider()
        {
            var directory = Path.Combine(Globals.DataFolder, "alternative", "interest-rate", "usa",
                "interest-rate.csv");
            _riskFreeRateProvider = FromCsvFile(directory, out var previousInterestRate);

            _lastInterestRateDate = DateTime.UtcNow.Date;

            // Sparse the discrete data points into continuous credit rate data for every day
            for (var date = _firstInterestRateDate; date <= _lastInterestRateDate; date = date.AddDays(1))
            {
                if (!_riskFreeRateProvider.TryGetValue(date, out var currentRate))
                {
                    _riskFreeRateProvider[date] = previousInterestRate;
                    continue;
                }

                previousInterestRate = currentRate;
            }
        }

        /// <summary>
        /// Reads Fed primary credit rate file and returns a dictionary of historical rate changes
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <param name="firstInterestRate">The first interest rate on file</param>
        /// <returns>Dictionary of historical credit rate change events</returns>
        public static Dictionary<DateTime, decimal> FromCsvFile(string file, out decimal firstInterestRate)
        {
            var dataProvider = Composer.Instance.GetPart<IDataProvider>();

            var firstInterestRateSet = false;
            firstInterestRate = DefaultRiskFreeRate;

            // skip the first header line, also skip #'s as these are comment lines
            var interestRateProvider = new Dictionary<DateTime, decimal>();
            foreach (var line in dataProvider.ReadLines(file).Skip(1)
                         .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (TryParse(line, out var date, out var interestRate))
                {
                    if (!firstInterestRateSet)
                    {
                        firstInterestRate = interestRate;
                        firstInterestRateSet = true;
                    }

                    interestRateProvider[date] = interestRate;
                }
            }

            if (interestRateProvider.Count == 0)
            {
                Log.Error($"InterestRateProvider.FromCsvFile(): no interest rates were loaded, please make sure the file is present '{file}'");
            }

            return interestRateProvider;
        }

        /// <summary>
        /// Parse the string into the interest rate date and value
        /// </summary>
        /// <param name="csvLine">The csv line to be parsed</param>
        /// <param name="date">Parsed interest rate date</param>
        /// <param name="interestRate">Parsed interest rate value</param>
        public static bool TryParse(string csvLine, out DateTime date, out decimal interestRate)
        {
            var line = csvLine.Split(',');

            if (!DateTime.TryParseExact(line[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Log.Error($"Couldn't parse date/time while reading FED primary credit rate file. Line: {csvLine}");
                interestRate = DefaultRiskFreeRate;
                return false;
            }

            if (!decimal.TryParse(line[1], NumberStyles.Any, CultureInfo.InvariantCulture, out interestRate))
            {
                Log.Error($"Couldn't parse primary credit rate while reading FED primary credit rate file. Line: {csvLine}");
                return false;
            }

            // Unit conversion from %
            interestRate /= 100;
            return true;
        }
    }
}
