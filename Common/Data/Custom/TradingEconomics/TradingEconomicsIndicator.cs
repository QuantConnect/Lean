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
using System;
using System.Collections.Generic;
using System.IO;
using NodaTime;
using QuantConnect.Data.UniverseSelection;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.TradingEconomics
{
    /// <summary>
    /// Represents the Trading Economics Indicator information.
    /// https://docs.tradingeconomics.com/#indicators
    /// </summary>
    public class TradingEconomicsIndicator : BaseData
    {
        /// <summary>
        /// Country name
        /// </summary>
        [JsonProperty(PropertyName = "Country")]
        public string Country { get; set; }

        /// <summary>
        /// Indicator category name
        /// </summary>
        [JsonProperty(PropertyName = "Category")]
        public string Category { get; set; }

        /// <summary>
        /// Release time and date in UTC
        /// </summary>
        [JsonProperty(PropertyName = "DateTime"), JsonConverter(typeof(TradingEconomicsDateTimeConverter))]
        public override DateTime EndTime { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty(PropertyName = "Value")]
        public override decimal Value { get; set; }

        /// <summary>
        /// Frequency of the indicator
        /// </summary>
        [JsonProperty(PropertyName = "Frequency")]
        public string Frequency { get; set; }

        /// <summary>
        /// Time when new data was inserted or changed
        /// </summary>
        [JsonProperty(PropertyName = "LastUpdate"), JsonConverter(typeof(TradingEconomicsDateTimeConverter))]
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Unique symbol used by Trading Economics
        /// </summary>
        [JsonProperty(PropertyName = "HistoricalDataSymbol")]
        public string HistoricalDataSymbol { get; set; }

        /// <summary>
        /// Return the Subscription Data Source gained from the URL
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Subscription Data Source.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var symbol = config.Symbol.Value.ToLowerInvariant();
            var source = Path.Combine(Globals.DataFolder, "alternative", "trading-economics", "indicator", symbol, Invariant($"{date:yyyyMMdd}.zip"));
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Collection);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="content">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        /// Collection of TradingEconomicsIndicator objects
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var objectList = JsonConvert.DeserializeObject<List<TradingEconomicsIndicator>>(content);
            foreach (var obj in objectList)
            {
                obj.Symbol = config.Symbol;
                if (obj.LastUpdate > obj.EndTime)
                {
                    obj.EndTime = obj.LastUpdate;
                }
            }
            return new BaseDataCollection(date, config.Symbol, objectList);
        }

        /// <summary>
        /// Clones the data. This is required for some custom data
        /// </summary>
        /// <returns>A new cloned instance</returns>
        public override BaseData Clone()
        {
            return new TradingEconomicsIndicator
            {
                Country = Country,
                Category = Category,
                EndTime = EndTime,
                Value = Value,
                Frequency = Frequency,
                LastUpdate = LastUpdate,
                HistoricalDataSymbol = HistoricalDataSymbol,

                Symbol = Symbol,
                Time = Time,
            };
        }

        /// <summary>
        /// Formats a string with the Trading Economics Indicator information.
        /// </summary>
        public override string ToString() => Invariant($"{HistoricalDataSymbol} ({Country} - {Category}): {Value}");

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }
    }
}