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

using NodaTime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace QuantConnect.Data.Custom.Robintrack
{
    /// <summary>
    /// Aggregate unique user stock holdings
    ///
    /// Data sourced from Robintrack - https://robintrack.net
    /// </summary>
    public class RobintrackHoldings : BaseData
    {
        private static List<Resolution> _supportedResolutions = new List<Resolution>
        {
            Resolution.Second,
            Resolution.Minute,
            Resolution.Hour,
            Resolution.Daily
        };

        /// <summary>
        /// Number of unique users holding a given stock
        /// </summary>
        public int UsersHolding { get; set; }

        /// <summary>
        /// Total number of unique holdings across all stocks by users
        /// </summary>
        public decimal TotalUniqueHoldings { get; set; }

        /// <summary>
        /// Percentage of the U.S. equities universe that unique users hold stock for
        /// </summary>
        public decimal UniverseHoldingPercent => UsersHolding / TotalUniqueHoldings;

        /// <summary>
        /// Alias for <see cref="UsersHolding"/>
        /// </summary>
        public override decimal Value
        {
            get { return UsersHolding; }
        }

        /// <summary>
        /// Gets the source of the file
        /// </summary>
        /// <param name="config">Subscription config</param>
        /// <param name="date">Data for the given date to read</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Subscription data source</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                throw new NotImplementedException("Live trading currently is not implemented for this data set");
            }

            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "robintrack",
                    $"{config.Symbol.Underlying.Value.ToLowerInvariant()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Reads the data from the source provided in <see cref="GetSource(SubscriptionDataConfig, DateTime, bool)"/>
        /// and converts it into an instance of this class (<see cref="RobintrackHoldings"/>).
        /// </summary>
        /// <param name="config">Subscription config</param>
        /// <param name="line">Line to parse</param>
        /// <param name="date">Date that the data is being read</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Instance of <see cref="RobintrackHoldings"/> casted as <see cref="BaseData"/></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                throw new NotImplementedException("Live trading currently is not implemented for this data set");
            }

            var instance = Read(line);
            instance.Symbol = config.Symbol;

            return instance;
        }

        /// <summary>
        /// Creates a clone of the current object
        /// </summary>
        /// <returns>Copy of the current object</returns>
        public override BaseData Clone()
        {
            return new RobintrackHoldings
            {
                EndTime = EndTime,
                Symbol = Symbol,
                UsersHolding = UsersHolding,
                TotalUniqueHoldings = TotalUniqueHoldings,
            };
        }

        /// <summary>
        /// Parses a line of Robintrack data with an optional dateFormat
        /// </summary>
        /// <param name="line">Line to parse</param>
        /// <param name="dateFormat">Date format to parse timestamp in</param>
        /// <returns>Instance of this class</returns>
        public static RobintrackHoldings Read(string line, string dateFormat = "yyyyMMdd HH:mm:ss")
        {
            var i = 0;
            var csv = line.ToCsvData(size: 3);
            var timestamp = Parse.DateTimeExact(csv[i++], dateFormat, DateTimeStyles.AdjustToUniversal);
            var usersHolding = Parse.Int(csv[i++]);
            var totalUniqueHoldings = Parse.Decimal(csv[i]);

            return new RobintrackHoldings
            {
                EndTime = timestamp,
                UsersHolding = usersHolding,
                TotalUniqueHoldings = totalUniqueHoldings
            };
        }


        /// <summary>
        /// Sets the data timezone
        /// </summary>
        /// <returns>Timezone</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }

        /// <summary>
        /// Enables mapping of the underlying symbol
        /// </summary>
        /// <returns>true</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Provides list of supported resolutions
        /// </summary>
        /// <returns>List of supported resolutions</returns>
        public override List<Resolution> SupportedResolutions()
        {
            return _supportedResolutions;
        }
    }
}
