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
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents a request for historical data using pandas
    /// </summary>
    public class PandasHistoryRequest : HistoryRequest
    {
        public DateTime Date { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public string Source { get; }
        public double PriceScaleFactor { get; }
        public string[] Names { get; }

        public TimeSpan Period => Resolution.ToTimeSpan();

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRequest"/> class from the specified config and exchange hours
        /// </summary>
        /// <param name="config">The subscription data config used to initalize this request</param>
        /// <param name="hours">The exchange hours used for fill forward processing</param>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        /// <param name="date"></param>
        public PandasHistoryRequest(SubscriptionDataConfig config,
            SecurityExchangeHours hours,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            Resolution resolution,
            DateTime date)
            : base(config, hours, startTimeUtc, endTimeUtc)
        {
            Date = date;
            Resolution = resolution;
            
            Source = LeanData.GenerateZipFilePath(Globals.DataFolder, Symbol, Date, Resolution, TickType);

            StartTime = StartTimeUtc.ConvertFromUtc(config.DataTimeZone);
            EndTime = EndTimeUtc.ConvertFromUtc(config.DataTimeZone);

            if (Resolution != Resolution.Daily)
            {
                if (IncludeExtendedMarketHours)
                {
                    FillForwardResolution = Resolution;
                }

                var marketHours = ExchangeHours.GetMarketHours(Date);
                var marketOpen = marketHours.GetMarketOpen(TimeSpan.Zero, IncludeExtendedMarketHours);
                var marketClose = marketHours.GetMarketClose(marketOpen.Value, IncludeExtendedMarketHours);

                var startTime = Date.Add(marketOpen.Value);
                StartTime = startTime > StartTime ? startTime : StartTime;

                var endTime = Date.Add(marketClose.Value);
                EndTime = endTime < EndTime ? endTime : EndTime;
            }

            DataType = IsCustomData ? config.Type : LeanData.GetDataType(resolution, TickType);

            PriceScaleFactor = (double)config.PriceScaleFactor;

            // Include TradeBar._scaleFactor
            if (DataType == typeof(TradeBar))
            {
                PriceScaleFactor /= 10000;
            }

            var namesByType = new Dictionary<Type, string[]>
            {
                { typeof(TradeBar), new[]{ "open", "hight", "low", "close", "volume" } },
                { typeof(QuoteBar), new[]{ "bidopen", "bidhigh", "bidlow", "bidclose", "bidsize", "askopen", "askhigh", "asklow", "askclose", "asksize", } },
            };
            
            string[] names;
            if (namesByType.TryGetValue(DataType, out names))
            {
                Names = names;
            }
        }
    }
}
