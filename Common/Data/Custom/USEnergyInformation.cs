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

using Newtonsoft.Json.Linq;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// US Energy Information Administration provides extensive data on energy usage, import, export,
    /// and forecasting across all US energy sectors.
    /// https://www.eia.gov/opendata/
    /// </summary>
    public class USEnergyInformation : BaseData
    {
        private string _previousContent = string.Empty;
        private DateTime _previousDate = DateTime.MinValue;
        
        /// <summary>
        /// The end time of this data.
        /// </summary>
        public override DateTime EndTime => Time + Period;

        /// <summary>
        /// The period of this data (hour, month, quarter, or annual)
        /// </summary>
        public TimeSpan Period { get; private set; }

        /// <summary>
        /// Gets the EIA API token.
        /// </summary>
        public static string AuthCode { get; private set; } = string.Empty;

        /// <summary>
        /// Returns true if the EIA API token has been set.
        /// </summary>
        public static bool IsAuthCodeSet { get; private set; }

        /// <summary>
        /// Sets the EIA API token.
        /// </summary>
        /// <param name="authCode">The EIA API token</param>
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
            return new SubscriptionDataSource(
                $"https://api.eia.gov/series/?api_key={AuthCode}&series_id={config.Symbol}",
                SubscriptionTransportMedium.Rest,
                FileFormat.Collection);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="content">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        /// Collection of USEnergyInformation objects
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            // Do not emit if the content did not change
            if (_previousContent == content) return null;
            _previousContent = content;

            try
            {
                var format = GetFormat(config.Symbol.Value);
                var series = JObject.Parse(content)["series"][0];

                // Do not emit if the end of the series did not change
                date = DateTimeConverter(series["end"], format);
                if (_previousDate == date) return null;
                _previousDate = date;

                var objectList = (
                    from jToken in series["data"]
                    where jToken[1].Type != JTokenType.Null
                    select new USEnergyInformation
                    {
                        Symbol = config.Symbol,
                        Period = Period,
                        Value = (decimal)jToken[1],
                        Time = DateTimeConverter(jToken[0], format)
                            .ConvertFromUtc(config.DataTimeZone) - Period
                    })
                    .OrderBy(x => x.EndTime);

                return new BaseDataCollection(date, config.Symbol, objectList);
            }
            catch
            {
                return null;
            }
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
                    return "yyyy";
                // Quarterly data has Period ~ 90 days
                case 'Q':
                    Period = TimeSpan.FromDays(90);
                    return DateFormat.EightCharacter;
                // Monthly data has Period ~ 30 days
                case 'M':
                    Period = TimeSpan.FromDays(30);
                    return DateFormat.YearMonth;
                // Daily has Period = 1 day
                case 'D':
                    Period = TimeSpan.FromDays(1);
                    return DateFormat.EightCharacter;
                // Hourly has period = 1 Hour
                case 'H':
                    Period = TimeSpan.FromHours(1);
                    return "yyyyMMdd'T'HHZ";
                default:
                    throw new Exception("Unsupported Period");
            }
        }

        /// <summary>
        /// Converts a <see cref="JToken"/> object containing raw date into a <see cref="DateTime"/> object.
        /// Handles special case for quarterly data (Series ID ends with Q1, Q2, etc)
        /// </summary>
        /// <param name="jToken">The raw date</param>
        /// <param name="format">The date format</param>
        /// <returns>
        /// <see cref="DateTime"/> corresponding to the <see cref="JToken"/>.
        /// </returns>
        private DateTime DateTimeConverter(JToken jToken, string format)
        {
            var dateData = jToken.ToString();

            if (dateData.Contains("Q"))
            {
                switch (dateData.Last())
                {
                    case '1':
                        dateData = dateData.Substring(0, 4) + "0101";
                        break;
                    case '2':
                        dateData = dateData.Substring(0, 4) + "0401";
                        break;
                    case '3':
                        dateData = dateData.Substring(0, 4) + "0701";
                        break;
                    case '4':
                        dateData = dateData.Substring(0, 4) + "1001";
                        break;
                    default:
                        throw (new Exception("invalid quarter input"));
                }
            }
            return DateTime.ParseExact(dateData, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }
    }
}