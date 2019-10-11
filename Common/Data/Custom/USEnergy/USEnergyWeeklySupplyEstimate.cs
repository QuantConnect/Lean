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
using System.IO;

namespace QuantConnect.Data.Custom.USEnergy
{
    /// <summary>
    /// United States Energy Information Administration Weekly Supply Estimates
    /// </summary>
    public class USEnergyWeeklySupplyEstimate : BaseData
    {
        /// <summary>
        /// Creates an instance of the object
        /// </summary>
        public USEnergyWeeklySupplyEstimate()
        {
        }

        /// <summary>
        /// Gets the source location of the data
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="date">Date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Class with location of data and method to poll for it</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                return new SubscriptionDataSource($"http://cache.quantconnect.com/alternative/usenergy/{config.Symbol.Value.ToLowerInvariant()}.csv", SubscriptionTransportMedium.RemoteFile);
            }

            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "usenergy",
                    $"{config.Symbol.Value.ToLowerInvariant()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile
            );
        }

        /// <summary>
        /// Read the data from the source, parsing it into a BaseData derived instance
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>BaseData derived instance</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var csv = line.Split(',');

            DateTime time;
            decimal value;

            if (!Parse.TryParseExact(csv[0], "yyyyMMdd", DateTimeStyles.None, out time))
            {
                return null;
            }
            if (!Parse.TryParse(csv[1], NumberStyles.Float, out value))
            {
                return null;
            }

            return new USEnergyWeeklySupplyEstimate
            {
                Symbol = config.Symbol,
                // Add a day to prevent us from reading the data at 00:00 of the same day
                // even though the report would have come out later in the day.
                Time = time.AddDays(1),
                Value = value
            };
        }

        /// <summary>
        /// Indicates that the data set requires mapping
        /// </summary>
        /// <returns>false</returns>
        public override bool RequiresMapping()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether the data set is sparse
        /// </summary>
        /// <returns>false</returns>
        public override bool IsSparseData()
        {
            return false;
        }

        /// <summary>
        /// Converts instance to string
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol} - {Value}";
        }
    }
}
