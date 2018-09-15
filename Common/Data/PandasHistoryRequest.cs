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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents a request for historical data using pandas
    /// </summary>
    public class PandasHistoryRequest
    {
        /// <summary>
        /// Gets the symbol to request data for
        /// </summary>
        public Symbol Symbol { get; }

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
        public string ZipFilePath { get; }

        /// <summary>
        /// Entry in the source file
        /// </summary>
        public string ZipEntryName { get; }

        /// <summary>
        /// Factor used to create adjusted prices from raw prices
        /// </summary>
        public double PriceScaleFactor { get; set; } = 1;

        /// <summary>
        /// Columns names
        /// </summary>
        public string[] Names { get; }

        /// <summary>
        /// Requested data resolution in TimeSpan
        /// </summary>
        public TimeSpan Period { get; }

        /// <summary>
        /// Gets a flag indicating whether the file exists
        /// </summary>
        public bool IsValid => File.Exists(ZipFilePath);

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRequest"/> class from the specified config and exchange hours
        /// </summary>
        /// <param name="config">The subscription data config used to initalize this request</param>
        /// <param name="hours">The exchange hours used for fill forward processing</param>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        /// <param name="resolution">Requested data resolution</param>
        /// <param name="date">Date of this request</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        public PandasHistoryRequest(SubscriptionDataConfig config,
            SecurityExchangeHours hours,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            Resolution resolution,
            DateTime date,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider)
        {
            Symbol = config.Symbol;

            var id = Symbol.ID;
            if (id.SecurityType == SecurityType.Equity)
            {
                var resolver = mapFileProvider.Get(id.Market);
                var mapFile = resolver.ResolveMapFile(id.Symbol, id.Date);
                Symbol.UpdateMappedSymbol(mapFile.GetMappedSymbol(date, id.Symbol));

                var factorFile = factorFileProvider.Get(Symbol);
                var factorFileRow = factorFile.GetScalingFactors(date);
                PriceScaleFactor = (double)factorFileRow.PriceScaleFactor / 10000;
            }
            else if (id.SecurityType == SecurityType.Option)
            {
                PriceScaleFactor /= 10000;
            }

            MarketOpenHours = new List<DateTime>();

            Period = resolution.ToTimeSpan();
            ZipFilePath = LeanData.GenerateZipFilePath(Globals.DataFolder, Symbol, date, resolution, config.TickType);
            ZipEntryName = LeanData.GenerateZipEntryName(Symbol, date, resolution, config.TickType);

            Names = GetNames(resolution, config.TickType);

            var startTime = startTimeUtc.ConvertFromUtc(config.ExchangeTimeZone);
            var endTime = endTimeUtc.ConvertFromUtc(config.ExchangeTimeZone);

            if (resolution == Resolution.Daily)
            {
                var localDateTime = startTime.Date;
                while (localDateTime < endTime.Date)
                {
                    localDateTime = localDateTime.Add(Period);
                    MarketOpenHours.Add(localDateTime);
                }
            }
            else if (resolution == Resolution.Hour)
            {
                var localDateTime = date > startTime ? startTime : date;

                // If market is open, use the previsous day to compute the next market open
                if (hours.IsOpen(localDateTime, config.ExtendedMarketHours))
                {
                    localDateTime = localDateTime.AddDays(-1);
                }
                localDateTime = hours.GetNextMarketOpen(localDateTime.Date, config.ExtendedMarketHours);

                while (localDateTime < endTime)
                {
                    if (hours.IsOpen(localDateTime, config.ExtendedMarketHours))
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
            // High resolution requests will only record the lower and upper boundary
            else
            {
                // Loop through market hours segments, since the security can 
                // have more than one open period each day (e.g. futures)
                var segments = hours.GetMarketHours(date).Segments;
                foreach (var segment in segments)
                {
                    if (segment.State != MarketHoursState.Market)
                    {
                        if (segment.State == MarketHoursState.Closed ||
                            !config.ExtendedMarketHours)
                        {
                            continue;
                        }
                    }

                    var lowerBound = date.Add(segment.Start);
                    lowerBound = startTime > lowerBound ? startTime : lowerBound;

                    var upperBound = date.Add(segment.End);
                    upperBound = endTime < upperBound ? endTime : upperBound;

                    if (lowerBound > upperBound)
                    {
                        lowerBound = upperBound;
                    }

                    MarketOpenHours.Add(lowerBound);
                    MarketOpenHours.Add(upperBound);
                }
            }
            Date = date.ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);

            // Handles previous day data
            // If current date/time in MarketOpenHours are all the same and
            // Date at data time zone if from the previous day, we add that pair
            var distinct = MarketOpenHours.Distinct().ToList();
            if (distinct.Count == 1)
            {
                var upperBound = distinct[0];
                if (Date.Date != upperBound.Date)
                {
                    MarketOpenHours.Clear();
                    MarketOpenHours.Add(Date);
                    MarketOpenHours.Add(upperBound);
                }
            }
        }

        private string[] GetNames(Resolution resolution, TickType tickType)
        {
            var dataType = LeanData.GetDataType(resolution, tickType);

            if (dataType == typeof(TradeBar))
            {
                return new[] { "open", "hight", "low", "close", "volume" };
            }

            if (dataType == typeof(QuoteBar))
            {
                return new[] { "bidopen", "bidhigh", "bidlow", "bidclose", "bidsize", "askopen", "askhigh", "asklow", "askclose", "asksize" };
            }

            if (dataType == typeof(OpenInterest))
            {
                return new[] { "openinterest" };
            }

            if (dataType == typeof(Tick))
            {
                var securityType = Symbol.ID.SecurityType;
                switch (securityType)
                {
                    case SecurityType.Equity:
                        return new[] { "lastprice", "quantity", "exchange", "salecondition", "suspicious" };
                    case SecurityType.Forex:
                    case SecurityType.Cfd:
                        return new[] { "bidprice", "askprice" };
                    case SecurityType.Option:
                    case SecurityType.Future:
                        if (tickType == TickType.Trade)
                        {
                            return new[] { "lastprice", "quantity", "exchange", "salecondition", "suspicious" };
                        }
                        else
                        {
                            return new[] { "bidprice", "bidsize", "askprice", "asksize", "exchange", "suspicious" };
                        }
                    case SecurityType.Crypto:
                        if (tickType == TickType.Trade)
                        {
                            return new[] { "lastprice", "quantity" };
                        }
                        else
                        {
                            return new[] { "bidprice", "bidsize", "askprice", "asksize" };
                        }
                    default:
                        throw new ArgumentOutOfRangeException($"PandasHistoryRequest.GetNames: invalid data type {nameof(dataType)}");

                }
            }
            throw new ArgumentOutOfRangeException($"PandasHistoryRequest.GetNames: invalid data type {nameof(dataType)}");
        }

        /// <summary>
        /// Returns the source file path to represent the current object.
        /// </summary>
        public override string ToString() => $"{ZipFilePath}#{ZipEntryName}";
    }
}