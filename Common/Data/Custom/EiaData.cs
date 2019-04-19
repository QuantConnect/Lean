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
using System.Linq;
using System.Data;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// US Energy Information Association provides extensive data on energy usage, import, export,
    /// and forecasting across all US energy sectors.
    /// https://www.eia.gov/opendata/
    /// </summary>

    public class EiaData : BaseData
    {
        /// <summary>
        /// Declare string representations of different periods and the variable
        /// to hold our chosen date format
        /// </summary>
        private const string _hourly = "yyyyMMdd'T'HH'Z'";
        private const string _daily = "yyyyMMdd";
        private const string _monthly = "yyyyMM";
        private const string _quarterly = "yyyyqq";
        private const string _annual = "yyyy";
        private string _dateFormatChosen;

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
        /// The period of this data (hour, month, quarter, or annual)
        /// </summary>
        public TimeSpan Period
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Eia API token.
        /// </summary>
        public static string AuthCode { get; private set; } = string.Empty;

        /// <summary>
        /// Returns true if the Tiingo API token has been set.
        /// </summary>
        public static bool IsAuthCodeSet { get; private set; }

        /// <summary>
        /// Sets the Tiingo API token.
        /// </summary>
        /// <param name="authCode">The Tiingo API token</param>
        public static void SetAuthCode(string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode)) return;

            AuthCode = authCode;
            IsAuthCodeSet = true;
        }

        /// <summary>
        /// Return the Subscription Data Source gained from the URL
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Subscription Data Source.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var source = $"https://api.eia.gov/series/?api_key={EiaData.AuthCode}&series_id={config.Symbol}";
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.Rest, FileFormat.Collection);
        }

        /// <summary>
        /// Takes the Series ID of the data and assigns the appropriate Period and string format of the date
        /// </summary>
        /// <param name="seriesId">The string appended at the end of URL to retrieve dataset</param>
        /// <returns></returns>
        private string GetFormat(string seriesId)
        {
            switch (seriesId.Last())
            {
                // Periods are closest approximation possible in days, except hourly
                // Annual data has Period ~ 365 days
                case 'A':
                    Period = TimeSpan.FromDays(365);
                    _dateFormatChosen = _annual;
                    break;
                // Quarterly data has Period ~ 90 days
                case 'Q':
                    Period = TimeSpan.FromDays(90);
                    _dateFormatChosen = _quarterly;
                    break;
                // Monthly data has Period ~ 30 days
                case 'M':
                    Period = TimeSpan.FromDays(30);
                    _dateFormatChosen = _monthly;
                    break;
                // Daily has Period = 1 day
                case 'D':
                    Period = TimeSpan.FromDays(1);
                    _dateFormatChosen = _daily;
                    break;
                // Hourly has period = 1 Hour
                case 'H':
                    Period = TimeSpan.FromHours(1);
                    _dateFormatChosen = _hourly;
                    break;
                default:
                    throw new Exception("Unsupported Period");
            }

            return _dateFormatChosen;
        }

        /// <summary>
        /// Special case handler for quarterly data (Series ID ends with Q1, Q2, etc)
        /// </summary>
        /// <param name="dateData">String containing raw date format</param>
        /// <returns>
        ///     Properly formatted datestring
        /// </returns>
        private string QuarterDateHandler(string dateData)
        {
            string dateString;
            if (dateData.Contains("Q"))
            {
                switch (dateData.Last())
                {
                    case '1':
                        dateString = dateData.Substring(0, 4) + "0331";
                        break;
                    case '2':
                        dateString = dateData.Substring(0, 4) + "0630";
                        break;
                    case '3':
                        dateString = dateData.Substring(0, 4) + "0930";
                        break;
                    case '4':
                        dateString = dateData.Substring(0, 4) + "1231";
                        break;
                    default:
                        throw (new Exception("invalid quarter input"));
                }
            }
            else
            {
                dateString = dateData;
            }
            return dateString;
        }

        /// <summary>
        ///     Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="content">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     Collection of BaseData objects
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var rawData = JObject.Parse(content)["series"][0]["data"];
            var objectList = new List<EiaData>();
            var format = GetFormat(config.Symbol.Value);

            objectList = ((JArray)rawData).Select(element => new EiaData
            {
                Time = DateTime.ParseExact(QuarterDateHandler(element[0].ToString()), format, CultureInfo.InvariantCulture),
                Value = Convert.ToDecimal(element[1]),
                Period = Period,
                Symbol = config.Symbol
            }).OrderBy(element => element.Time).ToList();

            return new BaseDataCollection(date, config.Symbol, objectList);
        }
    }
}