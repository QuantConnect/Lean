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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using NodaTime;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.Tiingo
{
    /// <summary>
    /// Tiingo daily price data
    /// https://api.tiingo.com/docs/tiingo/daily
    /// </summary>
    /// <remarks>Requires setting <see cref="Tiingo.AuthCode"/></remarks>
    public class TiingoPrice : TradeBar
    {
        private readonly ConcurrentDictionary<string, DateTime> _startDates = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        /// The end time of this data. Some data covers spans (trade bars) and as such we want
        /// to know the entire time span covered
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Time = value - Period; }
        }

        /// <summary>
        /// The period of this trade bar, (second, minute, daily, ect...)
        /// </summary>
        public override TimeSpan Period => QuantConnect.Time.OneDay;

        /// <summary>
        /// The date this data pertains to
        /// </summary>
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// The actual (not adjusted) open price of the asset on the specific date
        /// </summary>
        [JsonProperty("open")]
        public override decimal Open { get; set; }

        /// <summary>
        /// The actual (not adjusted) high price of the asset on the specific date
        /// </summary>
        [JsonProperty("high")]
        public override decimal High { get; set; }

        /// <summary>
        /// The actual (not adjusted) low price of the asset on the specific date
        /// </summary>
        [JsonProperty("low")]
        public override decimal Low { get; set; }

        /// <summary>
        /// The actual (not adjusted) closing price of the asset on the specific date
        /// </summary>
        [JsonProperty("close")]
        public override decimal Close { get; set; }

        /// <summary>
        /// The actual (not adjusted) number of shares traded during the day
        /// </summary>
        [JsonProperty("volume")]
        public override decimal Volume { get; set; }

        /// <summary>
        /// The adjusted opening price of the asset on the specific date. Returns null if not available.
        /// </summary>
        [JsonProperty("adjOpen")]
        public decimal AdjustedOpen { get; set; }

        /// <summary>
        /// The adjusted high price of the asset on the specific date. Returns null if not available.
        /// </summary>
        [JsonProperty("adjHigh")]
        public decimal AdjustedHigh { get; set; }

        /// <summary>
        /// The adjusted low price of the asset on the specific date. Returns null if not available.
        /// </summary>
        [JsonProperty("adjLow")]
        public decimal AdjustedLow { get; set; }

        /// <summary>
        /// The adjusted close price of the asset on the specific date. Returns null if not available.
        /// </summary>
        [JsonProperty("adjClose")]
        public decimal AdjustedClose { get; set; }

        /// <summary>
        /// The adjusted number of shares traded during the day - adjusted for splits. Returns null if not available
        /// </summary>
        [JsonProperty("adjVolume")]
        public long AdjustedVolume { get; set; }

        /// <summary>
        /// The dividend paid out on "date" (note that "date" will be the "exDate" for the dividend)
        /// </summary>
        [JsonProperty("divCash")]
        public decimal Dividend { get; set; }

        /// <summary>
        /// A factor used when a company splits or reverse splits. On days where there is ONLY a split (no dividend payment),
        /// you can calculate the adjusted close as follows: adjClose = "Previous Close"/splitFactor
        /// </summary>
        [JsonProperty("splitFactor")]
        public decimal SplitFactor { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="TiingoPrice"/> class.
        /// </summary>
        public TiingoPrice()
        {
            Symbol = Symbol.Empty;
            DataType = MarketDataType.Base;
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            DateTime startDate;
            if (!_startDates.TryGetValue(config.Symbol.Value, out startDate))
            {
                startDate = date;
                _startDates.TryAdd(config.Symbol.Value, startDate);
            }

            var tiingoTicker = TiingoSymbolMapper.GetTiingoTicker(config.Symbol);
            var source = Invariant($"https://api.tiingo.com/tiingo/daily/{tiingoTicker}/prices?startDate={startDate:yyyy-MM-dd}&token={Tiingo.AuthCode}");
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile, FileFormat.Collection);
        }

        /// <summary>
        ///     Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method,
        ///     and returns a new instance of the object
        ///     each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="content">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     Instance of the T:BaseData object generated by this line of the CSV
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var list = JsonConvert.DeserializeObject<List<TiingoPrice>>(content);

            foreach (var item in list)
            {
                item.Symbol = config.Symbol;
                item.Time = item.Date;
                item.Value = item.Close;
            }

            return new BaseDataCollection(date, config.Symbol, list);
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
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
