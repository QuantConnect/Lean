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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Fed US Primary Credit Rate at given date
    /// </summary>
    public class InterestRateProvider
    {
        private Dictionary<DateTime, decimal> _riskFreeRateProvider;

        /// <summary>
        /// Create class instance of interest rate provider
        /// </summary>
        /// <param name="startDate">start date of loading rate data</param>
        /// <param name="endDate">end date of loading rate data</param>
        public InterestRateProvider(DateTime? startDate = null, DateTime? endDate = null)
        {
            LoadInterestRateProvider(startDate, endDate);
        }

        /// <summary>
        /// Get interest rate by a given datetime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>interest rate of the given date</returns>
        public decimal GetInterestRate(DateTime dateTime)
        {
            decimal interestRate;

            while (!_riskFreeRateProvider.TryGetValue(dateTime, out interestRate))
            {
                dateTime = dateTime.AddDays(-1);
            }

            return interestRate;
        }

        /// <summary>
        /// Generate the daily historical US primary credit rate
        /// data found in /option/usa/interest-rate.csv
        /// </summary>
        /// <param name="startDate">start date</param>
        /// <param name="endDate">end date</param>
        /// <param name="subDirectory">subdirectory of the file beyond global data folder</param>
        /// <param name="fileName">file name containing rate date</param>
        protected void LoadInterestRateProvider(DateTime? startDate = null, DateTime? endDate = null, string subDirectory = "option/usa", string fileName = "interest-rate.csv")
        {
            var directory = Path.Combine(Globals.DataFolder,
                                        subDirectory,
                                        fileName);
            _riskFreeRateProvider = InterestRateProvider.FromCsvFile(directory);

            startDate = startDate ?? (DateTime?)_riskFreeRateProvider.Keys.OrderBy(x => x).First();
            endDate = endDate ?? (DateTime?)DateTime.Today;
            
            // Sparse the discrete datapoints into continuous credit rate data for every day
            _riskFreeRateProvider.TryGetValue((DateTime)startDate, out var currentRate);
            for (var date = ((DateTime)startDate).AddDays(1); date <= endDate; date = date.AddDays(1))
            {
                // Skip Saturday and Sunday (non-trading day)
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                if (_riskFreeRateProvider.ContainsKey(date))
                {
                    currentRate = _riskFreeRateProvider[date];
                }
                else
                {
                    _riskFreeRateProvider[date] = currentRate;
                }
            }
        }

        /// <summary>
        /// Reads Fed primary credit rate file and returns a dictionary of historical rate changes
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <returns>Dictionary of historical credit rate change events</returns>
        public static Dictionary<DateTime, decimal> FromCsvFile(string file)
        {
            IDataProvider dataProvider =
            Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider",
                "DefaultDataProvider"));

            // skip the first header line, also skip #'s as these are comment lines
            var interestRateProvider = dataProvider.ReadLines(file)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Skip(1)
                .Select(x => Create(x))
                .ToDictionary(x => x.Key, x => x.Value);

            if(interestRateProvider.Count == 0)
            {
                Log.Trace($"Unable to locate FED primary credit rate file. Defaulting to 1%. File: {file}");

                return new Dictionary<DateTime, decimal>
                {
                    { DateTime.MinValue, 0.01m }
                };
            }
            return interestRateProvider;
        }

        /// <summary>
        /// Creates a new instance of <see cref="InterestRateProvider"/> from the specified csv line
        /// </summary>
        /// <param name="csvLine">The csv line to be parsed</param>
        /// <returns>A new <see cref="InterestRateProvider"/> for the specified csv line</returns>
        public static KeyValuePair<DateTime, decimal> Create(string csvLine)
        {
            var line = csvLine.Split(',');

            if (!DateTime.TryParseExact(line[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                Log.Error($"Couldn't parse date/time while reading FED primary credit rate file. Line: {csvLine}");
            }

            if (!decimal.TryParse(line[1], out var interestRate))
            {
                Log.Error($"Couldn't parse primary credit rate while reading FED primary credit rate file. Line: {csvLine}");
            }

            return new KeyValuePair<DateTime, decimal>(date, interestRate);
        }
    }
}
