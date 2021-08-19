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
 *
*/

using System;
using NodaTime;
using ProtoBuf;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Orders;

namespace QuantConnect.Data.Custom.AlphaStreams
{
    /// <summary>
    /// Snapshot of an algorithms portfolio state
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class AlphaStreamsOrderEvent : BaseData
    {
        /// <summary>
        /// The deployed alpha id. This is the id generated upon submission to the alpha marketplace
        /// </summary>
        [JsonProperty("alphaId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [ProtoMember(10)]
        public string AlphaId { get; set; }

        /// <summary>
        /// The algorithm's unique deploy identifier
        /// </summary>
        [JsonProperty("algorithmId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [ProtoMember(11)]
        public string AlgorithmId { get; set; }

        /// <summary>
        /// The source of this data point, 'live trading' or in sample
        /// </summary>
        [ProtoMember(12)]
        public string Source { get; set; }

        /// <summary>
        /// The order event
        /// </summary>
        [ProtoMember(13)]
        public OrderEvent OrderEvent { get; set; }

        /// <summary>
        /// Return the Subscription Data Source
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Subscription Data Source.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "alphastreams",
                "orderevent",
                config.Symbol.Value.ToLowerInvariant(),
                $"{date:yyyyMMdd}.json"
            );
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>New data point object</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var dataPoint = JsonConvert.DeserializeObject<AlphaStreamsOrderEvent>(line);
            dataPoint.Symbol = config.Symbol;
            return dataPoint;
        }

        /// <summary>
        /// Specifies the data time zone for this data type
        /// </summary>
        /// <remarks>Will throw <see cref="InvalidOperationException"/> for security types
        /// other than <see cref="SecurityType.Base"/></remarks>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return DateTimeZone.Utc;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        public override BaseData Clone()
        {
            return new AlphaStreamsOrderEvent
            {
                Time = Time,
                Symbol = Symbol,
                Source = Source,
                AlphaId = AlphaId,
                DataType = DataType,
                OrderEvent = OrderEvent,
                AlgorithmId = AlgorithmId,
            };
        }

        /// <summary>
        /// Indicates that the data set is expected to be sparse
        /// </summary>
        public override bool IsSparseData()
        {
            return true;
        }
    }
}
