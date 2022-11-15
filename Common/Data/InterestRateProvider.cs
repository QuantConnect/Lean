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
using System.Globalization;
using QuantConnect.Logging;

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
        public double InterestRate;

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

            double interestRate;
            if (!double.TryParse(line[1], out interestRate))
            {
                Log.Trace($"Couldn't parse primary credit rate while reading FED primary credit rate file. Line: {csvLine}");
            }

            return new InterestRateProvider
            {
                Date = date,
                InterestRate = interestRate / 100d
            };
        }
    }
}