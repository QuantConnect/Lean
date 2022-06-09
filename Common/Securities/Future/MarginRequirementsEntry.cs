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

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// POCO class for modeling margin requirements at given date
    /// </summary>
    public class MarginRequirementsEntry
    {
        /// <summary>
        /// Date of margin requirements change
        /// </summary>
        public DateTime Date { get; init; }

        /// <summary>
        /// Initial overnight margin for the contract effective from the date of change
        /// </summary>
        public decimal InitialOvernight { get; init; }

        /// <summary>
        /// Maintenance overnight margin for the contract effective from the date of change
        /// </summary>
        public decimal MaintenanceOvernight { get; init; }

        /// <summary>
        /// Initial intraday margin for the contract effective from the date of change
        /// </summary>
        public decimal InitialIntraday { get; init; }

        /// <summary>
        /// Maintenance intraday margin for the contract effective from the date of change
        /// </summary>
        public decimal MaintenanceIntraday { get; init; }

        /// <summary>
        /// Creates a new instance of <see cref="MarginRequirementsEntry"/> from the specified csv line
        /// </summary>
        /// <param name="csvLine">The csv line to be parsed</param>
        /// <returns>A new <see cref="MarginRequirementsEntry"/> for the specified csv line</returns>
        public static MarginRequirementsEntry Create(string csvLine)
        {
            var line = csvLine.Split(',');

            DateTime date;
            if (!DateTime.TryParseExact(line[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Log.Trace($"Couldn't parse date/time while reading future margin requirement file. Line: {csvLine}");
            }

            decimal initialOvernight;
            if (!decimal.TryParse(line[1], out initialOvernight))
            {
                Log.Trace($"Couldn't parse Initial Overnight margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            decimal maintenanceOvernight;
            if (!decimal.TryParse(line[2], out maintenanceOvernight))
            {
                Log.Trace($"Couldn't parse Maintenance Overnight margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            // default value, if present in file we try to parse
            decimal initialIntraday = initialOvernight * 0.4m;
            if (line.Length >= 4
                && !decimal.TryParse(line[3], out initialIntraday))
            {
                Log.Trace($"Couldn't parse Initial Intraday margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            // default value, if present in file we try to parse
            decimal maintenanceIntraday = maintenanceOvernight * 0.4m;
            if (line.Length >= 5
                && !decimal.TryParse(line[4], out maintenanceIntraday))
            {
                Log.Trace($"Couldn't parse Maintenance Intraday margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            return new MarginRequirementsEntry
            {
                Date = date,
                InitialOvernight = initialOvernight,
                MaintenanceOvernight = maintenanceOvernight,
                InitialIntraday = initialIntraday,
                MaintenanceIntraday = maintenanceIntraday
            };
        }
    }
}
