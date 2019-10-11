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
    public class USEnergyAPI : BaseData
    {
        private TimeSpan _period = TimeSpan.Zero;
        private string _previousContent = string.Empty;
        private DateTime _previousDate = DateTime.MinValue;

        /// <summary>
        /// Represents the date/time when the analysis period stops
        /// </summary>
        public DateTime EnergyDataPointCloseTime { get; set; }

        /// <summary>
        /// Analysis period (see <see cref="EnergyDataPointCloseTime"/>) plus a delay to make the data lag emit times realistic
        /// </summary>
        public override DateTime EndTime { get; set; }

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
        /// Collection of USEnergyAPI objects
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var format = GetFormat(config.Symbol.Value);

            // Do not emit if the content did not change
            if (string.IsNullOrWhiteSpace(format) || _previousContent == content)
            {
                return GetEmptyCollection(config.Symbol);
            }
            _previousContent = content;

            try
            {
                // Fix invalid json before we parse it
                var index = content.IndexOfInvariant("\"series\":");
                content = "{" + content.Substring(index);
                var series = JObject.Parse(content)["series"][0];

                // Do not emit if the end of the series did not change
                date = (DateTime)series["updated"];
                if (_previousDate == date)
                {
                    return GetEmptyCollection(config.Symbol);
                }
                _previousDate = date;

                var last = series.Value<string>("lastHistoricalPeriod") ?? series["end"];
                var offset = date - DateTimeConverter(last, format);

                var objectList = (
                    from jToken in series["data"]
                    where jToken[1].Type != JTokenType.Null
                    let closeTime = DateTimeConverter(jToken[0], format).ConvertFromUtc(config.DataTimeZone)
                    select new USEnergyAPI
                    {
                        Symbol = config.Symbol,
                        Time = closeTime - _period,
                        EnergyDataPointCloseTime = closeTime,
                        EndTime = closeTime + offset,
                        Value = (decimal)jToken[1]
                    }
                    ).OrderBy(x => x.EndTime);

                return new BaseDataCollection(date, config.Symbol, objectList);
            }
            catch
            {
                return GetEmptyCollection(config.Symbol);
            }
        }

        private BaseDataCollection GetEmptyCollection(Symbol symbol)
        {
            return new BaseDataCollection(_previousDate, symbol);
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
                    _period = TimeSpan.FromDays(365);
                    return "yyyy";
                // Quarterly data has Period ~ 90 days
                case 'Q':
                    _period = TimeSpan.FromDays(90);
                    return DateFormat.EightCharacter;
                // Monthly data has Period ~ 30 days
                case 'M':
                    _period = TimeSpan.FromDays(30);
                    return DateFormat.YearMonth;
                // Daily has Period = 1 day
                case 'D':
                    _period = TimeSpan.FromDays(1);
                    return DateFormat.EightCharacter;
                // Hourly has period = 1 Hour
                case 'H':
                    _period = TimeSpan.FromHours(1);
                    return "yyyyMMdd'T'HHZ";
                default:
                    return string.Empty;
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

            if (!dateData.Contains("Q"))
            {
                return DateTime.ParseExact(dateData, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }

            string[] range;
            var year = Parse.Int(dateData.Substring(0, 4));

            switch (dateData.Last())
            {
                case '1':
                    range = new[] { $"{year}0101", $"{year}0401" };
                    break;
                case '2':
                    range = new[] { $"{year}0401", $"{year}0701" };
                    break;
                case '3':
                    range = new[] { $"{year}0701", $"{year}1001" };
                    break;
                case '4':
                    range = new[] { $"{year}1001", $"{1 + year}0101" };
                    break;
                default:
                    throw (new ArgumentException("invalid quarter input"));
            }

            var dates = range
                .Select(x => DateTime.ParseExact(x, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                .ToArray();

            _period = dates[1] - dates[0];

            return dates[1];
        }
    }
}