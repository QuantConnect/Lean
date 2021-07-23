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
using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Custom.IconicTypes
{
    /// <summary>
    /// Data source that is unlinked (no mapping) and takes any ticker when calling AddData
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class UnlinkedDataTradeBar : TradeBar
    {
        /// <summary>
        /// If true, we accept any ticker from the AddData call
        /// </summary>
        public static bool AnyTicker { get; set; }

        public UnlinkedDataTradeBar()
        {
            DataType = MarketDataType.Base;
            Period = TimeSpan.FromDays(1);
        }
        
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    "TestData",
                    "unlinkedtradebar",
                    AnyTicker ? "data.csv" : $"{config.Symbol.Value.ToLowerInvariant()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new UnlinkedDataTradeBar
            {
                Open = 1m,
                High = 2m,
                Low = 1m,
                Close = 1.5m,
                Volume = 0m,
                
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
            return false;
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
