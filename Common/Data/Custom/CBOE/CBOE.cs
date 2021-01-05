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

using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.Data.Custom.CBOE
{
    public class CBOE : TradeBar
    {
        /// <summary>
        /// Creates a new instance of the object
        /// </summary>
        public CBOE()
        {
            DataType = MarketDataType.Base;
        }

        /// <summary>
        /// Gets the source location of the CBOE file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="date"></param>
        /// <param name="isLiveMode"></param>
        /// <returns></returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                return new SubscriptionDataSource($"http://cache.quantconnect.com/alternative/cboe/{config.Symbol.Value.ToLowerInvariant()}.csv", SubscriptionTransportMedium.RemoteFile);
            }

            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "cboe",
                    $"{config.Symbol.Value.ToLowerInvariant()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile
            );
        }

        /// <summary>
        /// Reads the data from the source and creates a BaseData instance
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date we're requesting data for</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>New BaseData instance to be used in the algorithm</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // Return null if we don't have a valid date for the first entry
            if (!char.IsNumber(line.FirstOrDefault()))
            {
                return null;
            }

            var csv = line.Split(',')
                .Select(x => x.Trim())
                .ToList();

            decimal open;
            decimal high;
            decimal low;
            decimal close;

            QuantConnect.Parse.TryParse(csv[1], NumberStyles.Any, out open);
            QuantConnect.Parse.TryParse(csv[2], NumberStyles.Any, out high);
            QuantConnect.Parse.TryParse(csv[3], NumberStyles.Any, out low);
            QuantConnect.Parse.TryParse(csv[4], NumberStyles.Any, out close);

            return new CBOE
            {
                // Add a day delay to the data so that we LEAN doesn't assume that we get the data at 00:00 the day of
                Time = QuantConnect.Parse.DateTime(csv[0]).AddDays(1),
                Symbol = config.Symbol,
                Open = open,
                High = high,
                Low = low,
                Close = close
            };
        }

        /// <summary>
        /// Determines whether the data source requires mapping
        /// </summary>
        /// <returns>false</returns>
        public override bool RequiresMapping()
        {
            return false;
        }

        /// <summary>
        /// Determines if data source is sparse
        /// </summary>
        /// <returns>false</returns>
        public override bool IsSparseData()
        {
            return false;
        }

        /// <summary>
        /// Converts the instance to a string
        /// </summary>
        /// <returns>String containing open, high, low, close</returns>
        public override string ToString()
        {
            return $"{Symbol} - O: {Open}, H: {High}, L: {Low}, C: {Close}";
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }

        /// <summary>
        /// Gets the supported resolution for this data and security type
        /// </summary>
        public override List<Resolution> SupportedResolutions()
        {
            return DailyResolution;
        }
    }
}
