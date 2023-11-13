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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data.Serialization
{
    /// <summary>
    /// Data transfer object used for serializing a <see cref="SubscriptionDataConfig"/>
    /// </summary>
    public class SerializedSubscriptionDataConfig
    {
        /// <summary>
        /// Easy access to the order symbol associated with this event.
        /// </summary>
        [JsonProperty("symbol")]
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Security type
        /// </summary>
        [JsonProperty("security-type"), JsonConverter(typeof(StringEnumConverter), true)]
        public SecurityType SecurityType { get; set; }

        /// <summary>
        /// Subscription resolution
        /// </summary>
        [JsonProperty("resolution"), JsonConverter(typeof(StringEnumConverter), true)]
        public Resolution Resolution { get; set; }

        /// <summary>
        /// Extended market hours
        /// </summary>
        [JsonProperty("extended-market-hours")]
        public bool ExtendedMarketHours { get; set; }

        /// <summary>
        /// Data normalization mode
        /// </summary>
        [JsonProperty("data-normalization-mode"), JsonConverter(typeof(StringEnumConverter), true)]
        public DataNormalizationMode DataNormalizationMode { get; set; }

        /// <summary>
        /// Data mapping mode
        /// </summary>
        [JsonProperty("data-mapping-mode"), JsonConverter(typeof(StringEnumConverter), true)]
        public DataMappingMode DataMappingMode { get; set; }

        /// <summary>
        /// Contract depth offset
        /// </summary>
        [JsonProperty("contract-depth-offset")]
        public uint ContractDepthOffset { get; set; }

        /// <summary>
        /// Whether the subscription configuration is for a custom data type
        /// </summary>
        [JsonProperty("is-custom-data")]
        public bool IsCustomData { get; set; }

        /// <summary>
        /// The subscription data configuration tick type
        /// </summary>
        [JsonProperty("tick-types", ItemConverterType = typeof(StringEnumConverter))]
        public List<TickType> TickTypes { get; set; }

        /// <summary>
        /// The data type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Empty constructor required for JSON converter.
        /// </summary>
        protected SerializedSubscriptionDataConfig()
        {
        }

        /// <summary>
        /// Creates a new instance based on the provided config
        /// </summary>
        public SerializedSubscriptionDataConfig(SubscriptionDataConfig config)
        {
            Symbol = config.Symbol;
            SecurityType = config.SecurityType;
            Resolution = config.Resolution;
            ExtendedMarketHours = config.ExtendedMarketHours;
            DataNormalizationMode = config.DataNormalizationMode;
            DataMappingMode = config.DataMappingMode;
            ContractDepthOffset = config.ContractDepthOffset;
            IsCustomData = config.IsCustomData;
            TickTypes = new() { config.TickType };
            Type = config.Type.ToString();
        }

        /// <summary>
        /// Creates a new instance based on the provided configs for the same symbol
        /// </summary>
        public SerializedSubscriptionDataConfig(IEnumerable<SubscriptionDataConfig> configs)
            : this(configs.First())
        {
            var tickTypes = configs.Select(config => config.TickType);
            if (SubscriptionManager.DefaultDataTypes().TryGetValue(SecurityType, out var dataTypes))
            {
                // Sort tick types by the order of the default data types.
                // Using IndexOf is acceptably here because the number of tick types is quite small.
                tickTypes = tickTypes.OrderBy(tickType =>
                {
                    var index = dataTypes.IndexOf(tickType);
                    return index != -1 ? index : int.MaxValue;
                });
            }

            TickTypes = tickTypes.ToList();
        }
    }
}
