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
using System.IO;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents a request for historical data using pandas
    /// </summary>
    public class PandasHistoryRequest : HistoryRequest
    {
        private static readonly IMapFileProvider _mapFileProvider = new LocalDiskMapFileProvider();
        private static readonly IFactorFileProvider _factorFileProvider = new LocalDiskFactorFileProvider(_mapFileProvider);

        /// <summary>
        /// Date in the local file path
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// List of date/time with the hours that the market was open
        /// </summary>
        public List<DateTime> MarketOpenHours { get; }

        /// <summary>
        /// Source file path
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Factor used to create adjusted prices from raw prices
        /// </summary>
        public double PriceScaleFactor { get; } = 1;

        /// <summary>
        /// Columns names
        /// </summary>
        public string[] Names { get; }

        /// <summary>
        /// Requested data resolution in TimeSpan
        /// </summary>
        public TimeSpan Period => Resolution.ToTimeSpan();

        /// <summary>
        /// Gets a flag indicating whether the file exists
        /// </summary>
        public bool IsValid => File.Exists(Source);

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

            var id = config.Symbol.ID;
            if (id.SecurityType == SecurityType.Equity)
            {
                var resolver = _mapFileProvider.Get(id.Market);
                var mapFile = resolver.ResolveMapFile(id.Symbol, id.Date);
                Symbol.UpdateMappedSymbol(mapFile.GetMappedSymbol(date, id.Symbol));

                var factorFile = _factorFileProvider.Get(Symbol);
                var factorFileRow = factorFile.GetScalingFactors(date);
                PriceScaleFactor = (double)factorFileRow.PriceScaleFactor;
            }

            // Include TradeBar._scaleFactor
            if (DataType == typeof(TradeBar))
            {
                PriceScaleFactor /= 10000;
            }

            MarketOpenHours = new List<DateTime>();

            if (IncludeExtendedMarketHours)
            {
                FillForwardResolution = Resolution;
            }

            Date = date;
            Resolution = resolution;
            Source = LeanData.GenerateZipFilePath(Globals.DataFolder, Symbol, Date, Resolution, TickType);

            var startTime = StartTimeUtc.ConvertFromUtc(config.DataTimeZone);
            var endTime = EndTimeUtc.ConvertFromUtc(config.DataTimeZone);

            if (Resolution == Resolution.Daily)
            {
                var localDateTime = startTime.Date;
                while (localDateTime < endTime.Date)
                {
                    localDateTime = localDateTime.Add(Period);
                    MarketOpenHours.Add(localDateTime);
                }
            }
            else
            {
                var localDateTime = Date > startTime ? startTime : Date;

                // If market is open, use the previsous day to compute the next market open
                if (ExchangeHours.IsOpen(localDateTime, IncludeExtendedMarketHours))
                {
                    localDateTime = localDateTime.AddDays(-1);
                }
                localDateTime = ExchangeHours.GetNextMarketOpen(localDateTime.Date, IncludeExtendedMarketHours);

                // Define an earlier end time for high resolution to avoid extended market hours
                if (Resolution != Resolution.Hour && Date > endTime)
                {
                    endTime = ExchangeHours.GetNextMarketClose(localDateTime, IncludeExtendedMarketHours);
                }

                while (localDateTime < endTime)
                {
                    if (ExchangeHours.IsOpen(localDateTime, IncludeExtendedMarketHours))
                    {
                        var openHours = localDateTime.Add(Period).RoundDown(Period);
                        if (openHours > startTime)
                        {
                            MarketOpenHours.Add(openHours);
                        }
                    }
                    localDateTime = localDateTime.Add(Period);
                }
            }

            DataType = IsCustomData ? config.Type : LeanData.GetDataType(resolution, TickType);

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

        /// <summary>
        /// Returns the source file path to represent the current object.
        /// </summary>
        public override string ToString() => Source;
    }
}