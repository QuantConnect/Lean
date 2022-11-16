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
        /// <summary>
        /// Date of interest rate change
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// US Primary Credit Rate
        /// </summary>
        public decimal InterestRate;

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
                .Select(InterestRateProvider.Create)
                .ToDictionary(x => x.Date, x => x.InterestRate);

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
        public static InterestRateProvider Create(string csvLine)
        {
            var line = csvLine.Split(',');

            DateTime date;
            if (!DateTime.TryParseExact(line[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Log.Trace($"Couldn't parse date/time while reading FED primary credit rate file. Line: {csvLine}");
            }

            decimal interestRate;
            if (!decimal.TryParse(line[1], out interestRate))
            {
                Log.Trace($"Couldn't parse primary credit rate while reading FED primary credit rate file. Line: {csvLine}");
            }

            return new InterestRateProvider
            {
                Date = date,
                InterestRate = interestRate / 100m
            };
        }
    }
}