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
using System.IO;
using NodaTime;
using ProtoBuf;
using QuantConnect.Data;

namespace QuantConnect.Data.Custom.IconicTypes
{
    /// <summary>
    /// Data source that is linked (tickers that can have renames or be delisted)
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class LinkedData : BaseData
    {
        /// <summary>
        /// Example data
        /// </summary>
        [ProtoMember(55)]
        public int Count { get; set; }

        public override SubscriptionDataSource GetSource(
            SubscriptionDataConfig config,
            DateTime date,
            bool isLiveMode
        )
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    "TestData",
                    "linked",
                    $"{config.Symbol.Underlying.Value.ToLowerInvariant()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        public override BaseData Reader(
            SubscriptionDataConfig config,
            string line,
            DateTime date,
            bool isLiveMode
        )
        {
            return new LinkedData
            {
                Count = 10,
                Symbol = config.Symbol,
                EndTime = date
            };
        }

        /// <summary>
        /// Indicates whether the data source is sparse.
        /// If false, it will disable missing file logging.
        /// </summary>
        /// <returns>true</returns>
        public override bool IsSparseData()
        {
            return true;
        }

        /// <summary>
        /// Indicates whether the data source can undergo
        /// rename events/is tied to equities.
        /// </summary>
        /// <returns>true</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Set the data time zone to UTC
        /// </summary>
        /// <returns>Time zone as UTC</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }

        /// <summary>
        /// Sets the default resolution to Second
        /// </summary>
        /// <returns>Resolution.Second</returns>
        public override Resolution DefaultResolution()
        {
            return Resolution.Daily;
        }

        /// <summary>
        /// Gets a list of all the supported Resolutions
        /// </summary>
        /// <returns>All resolutions</returns>
        public override List<Resolution> SupportedResolutions()
        {
            return DailyResolution;
        }
    }
}
