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
    /// Data type that is indexed, i.e. a file that points to another file containing the contents
    /// we're looking for in a Symbol.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class IndexedLinkedData2 : IndexedBaseData
    {
        /// <summary>
        /// Example data property
        /// </summary>
        [ProtoMember(55)]
        public int Count { get; set; }

        /// <summary>
        /// Determines the actual source from an index contained within a ticker folder
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="date">Date</param>
        /// <param name="index">File to load data from</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>SubscriptionDataSource pointing to the article</returns>
        public override SubscriptionDataSource GetSourceForAnIndex(
            SubscriptionDataConfig config,
            DateTime date,
            string index,
            bool isLiveMode
        )
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    "TestData",
                    "indexlinked2",
                    "content",
                    $"{date.ToStringInvariant(DateFormat.EightCharacter)}.zip#{index}"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Gets the source of the index file
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>SubscriptionDataSource indicating where data is located and how it's stored</returns>
        public override SubscriptionDataSource GetSource(
            SubscriptionDataConfig config,
            DateTime date,
            bool isLiveMode
        )
        {
            if (isLiveMode)
            {
                throw new NotImplementedException("Live mode not supported");
            }

            return new SubscriptionDataSource(
                Path.Combine(
                    "TestData",
                    "indexlinked2",
                    config.Symbol.Value.ToLowerInvariant(),
                    $"{date.ToStringInvariant(DateFormat.EightCharacter)}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Index
            );
        }

        /// <summary>
        /// Creates an instance from a line of JSON containing article information read from the `content` directory
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date</param>
        /// <param name="isLiveMode">Is live mode</param>
        public override BaseData Reader(
            SubscriptionDataConfig config,
            string line,
            DateTime date,
            bool isLiveMode
        )
        {
            return new IndexedLinkedData2
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
