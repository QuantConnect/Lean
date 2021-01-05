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
using NodaTime;

namespace QuantConnect.Data.Custom.TradingEconomics
{
    /// <summary>
    /// Represents the Trading Economics Earnings information.
    /// https://docs.tradingeconomics.com/#earnings
    /// </summary>
    public class TradingEconomicsEarnings : BaseData
    {
        /// <summary>
        /// Release time and date in UTC
        /// </summary>
        [JsonProperty(PropertyName = "Date"), JsonConverter(typeof(TradingEconomicsDateTimeConverter))]
        public override DateTime EndTime { get; set; }

        /// <summary>
        /// Unique symbol used by Trading Economics
        /// </summary>
        [JsonProperty(PropertyName = "Symbol")]
        public string Symbol { get; set; }

        /// <summary>
        /// Earnings type: earnings, ipo, dividends
        /// </summary>
        [JsonProperty(PropertyName = "Type")]
        public EarningsType EarningsType { get; set; }

        /// <summary>
        /// Company name
        /// </summary>
        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Earnings per share
        /// </summary>
        [JsonProperty(PropertyName = "Actual")]
        public decimal? Actual { get; set; }

        /// <summary>
        /// Earnings per share
        /// </summary>
        public override decimal Value => Actual ?? 0m;

        /// <summary>
        /// Average forecast among a representative group of analysts
        /// </summary>
        [JsonProperty(PropertyName = "Forecast")]
        public decimal? Forecast { get; set; }

        /// <summary>
        /// Fiscal year and quarter
        /// </summary>
        [JsonProperty(PropertyName = "FiscalTag")]
        public string FiscalTag { get; set; }

        /// <summary>
        /// Fiscal year and quarter in different format
        /// </summary>
        [JsonProperty(PropertyName = "FiscalReference")]
        public string FiscalReference { get; set; }

        /// <summary>
        /// Calendar quarter for the release
        /// </summary>
        [JsonProperty(PropertyName = "CalendarReference")]
        public string CalendarReference { get; set; }

        /// <summary>
        /// Country name
        /// </summary>
        [JsonProperty(PropertyName = "Country")]
        public string Country { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        [JsonProperty(PropertyName = "Currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Time when new data was inserted or changed
        /// </summary>
        [JsonProperty(PropertyName = "LastUpdate"), JsonConverter(typeof(TradingEconomicsDateTimeConverter))]
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }
    }

    /// <summary>
    /// Earnings type: earnings, ipo, dividends
    /// </summary>
    public enum EarningsType
    {
        /// <summary>
        /// Earnings
        /// </summary>
        [JsonProperty(PropertyName = "earnings")]
        Earnings,

        /// <summary>
        /// IPO
        /// </summary>
        [JsonProperty(PropertyName = "ipo")]
        IPO,

        /// <summary>
        /// Dividends
        /// </summary>
        [JsonProperty(PropertyName = "dividends")]
        Dividends,

        /// <summary>
        /// Stock Splits
        /// </summary>
        [JsonProperty(PropertyName = "Stock Splits")]
        Split
    }
}