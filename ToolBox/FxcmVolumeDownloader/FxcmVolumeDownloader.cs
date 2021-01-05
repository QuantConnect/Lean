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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using QuantConnect.Data;
using QuantConnect.Data.Custom;

namespace QuantConnect.ToolBox
{
    /// <summary>
    ///     FXCM Real FOREX Volume/Transactions Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
    /// </summary>
    /// <seealso cref="QuantConnect.ToolBox.IDataDownloader" />
    public class FxcmVolumeDownloader : IDataDownloader
    {
        private enum FxcmSymbolId
        {
            EURUSD = 1,
            USDJPY = 2,
            GBPUSD = 3,
            USDCHF = 4,
            EURCHF = 5,
            AUDUSD = 6,
            USDCAD = 7,
            NZDUSD = 8,
            EURGBP = 9,
            EURJPY = 10,
            GBPJPY = 11,
            EURAUD = 14,
            EURCAD = 15,
            AUDJPY = 17
        }

        #region Fields

        /// <summary>
        ///     The request base URL.
        /// </summary>
        private readonly string _baseUrl = " http://marketsummary2.fxcorporate.com/ssisa/servlet?RT=SSI";

        private readonly string _dataDirectory;

        /// <summary>
        ///     FXCM session id.
        /// </summary>
        private readonly string _sid = "quantconnect";

        /// <summary>
        ///     The columns index which should be added to obtain the transactions.
        /// </summary>
        private readonly long[] _transactionsIdx = {27, 29, 31, 33};

        /// <summary>
        ///     Integer representing client version.
        /// </summary>
        private readonly int _ver = 1;

        /// <summary>
        ///     The columns index which should be added to obtain the volume.
        /// </summary>
        private readonly int[] _volumeIdx = {26, 28, 30, 32};

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="FxcmVolumeDownloader" /> class.
        /// </summary>
        /// <param name="dataDirectory">The Lean data directory.</param>
        public FxcmVolumeDownloader(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
        }

        #region Public Methods

        /// <summary>
        ///     Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>
        ///     Enumerable of base data for this symbol
        /// </returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var idx = 0;
            var obsTime = startUtc;
            var requestedData = new List<BaseData>();
            var lines = RequestData(symbol, resolution, startUtc, endUtc);

            do
            {
                var line = lines[idx++];
                var obs = line.Split(';');
                var stringDate = obs[0].Substring(startIndex: 3);
                obsTime = DateTime.ParseExact(stringDate, "yyyyMMddHHmm",
                                              DateTimeFormatInfo.InvariantInfo);
                var volume = _volumeIdx.Select(x => Parse.Long(obs[x])).Sum();

                var transactions = _transactionsIdx.Select(x => Parse.Int(obs[x])).Sum();
                requestedData.Add(new FxcmVolume
                {
                    Symbol = symbol,
                    Time = obsTime,
                    Value = volume,
                    Transactions = transactions
                });
            } while (obsTime.Date <= endUtc.Date && idx < lines.Length - 1);
            return requestedData.Where(o => o.Time.Date >= startUtc.Date && o.Time.Date <= endUtc.Date);
        }

        /// <summary>
        ///     Method in charge of making all the steps to save the data. It makes the request, parses the data and saves it.
        ///     This method takes into account the size limitation of the responses, slicing big request into smaller ones.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="startUtc">The start UTC.</param>
        /// <param name="endUtc">The end UTC.</param>
        /// <param name="update">Flag to </param>
        public void Run(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc, bool update = false)
        {
            var data = new List<BaseData>();
            var requestDayInterval = 0;
            var writer = new FxcmVolumeWriter(resolution, symbol, _dataDirectory);
            var intermediateStartDate = startUtc;
            var intermediateEndDate = endUtc;

            if (update)
            {
                var updatedStartDate = FxcmVolumeAuxiliaryMethods.GetLastAvailableDateOfData(symbol, resolution, writer.FolderPath);
                if (updatedStartDate == null) return;

                intermediateStartDate = ((DateTime) updatedStartDate).AddDays(value: -1);
                intermediateEndDate = DateTime.Today;
            }

            // As the responses has a Limit of 10000 lines, hourly data the minute data request should be sliced.
            if (resolution == Resolution.Minute && (endUtc - startUtc).TotalMinutes > 10000)
            {
                // Six days are 8640 minute observations, 7 days are 10080.
                requestDayInterval = 6;
            }
            else if (resolution == Resolution.Hour && (endUtc - startUtc).TotalHours > 10000)
            {
                // 410 days x 24 hr = 9840 hr.
                requestDayInterval = 410;
            }

            var counter = 0;
            do
            {
                if (requestDayInterval != 0)
                {
                    if (counter++ != 0)
                    {
                        intermediateStartDate = intermediateEndDate.AddDays(value: 1);
                    }
                    intermediateEndDate = intermediateStartDate.AddDays(requestDayInterval);
                    if (intermediateEndDate > endUtc) intermediateEndDate = endUtc;
                }
                data.AddRange(Get(symbol, resolution, intermediateStartDate, intermediateEndDate));
                // For every 300k observations in memory, write it.
                if (resolution == Resolution.Minute && counter % 30 == 0)
                {
                    writer.Write(data);
                    data.Clear();
                }
            } while (intermediateEndDate != endUtc);

            writer.Write(data, update);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Gets the FXCM identifier from a FOREX pair symbol.
        /// </summary>
        /// <param name="symbol">The pair symbol.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Volume data is not available for the selected symbol. - symbol</exception>
        private int GetFxcmIDFromSymbol(Symbol symbol)
        {
            int symbolId;
            try
            {
                symbolId = (int) Enum.Parse(typeof(FxcmSymbolId), symbol.Value);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Volume data is not available for the selected symbol.", "symbol");
            }
            return symbolId;
        }

        /// <summary>
        ///     Gets the string interval representation from the resolution.
        /// </summary>
        /// <param name="resolution">The requested resolution.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     resolution - tick or second resolution are not supported for Forex
        ///     Volume.
        /// </exception>
        private string GetIntervalFromResolution(Resolution resolution)
        {
            string interval;
            switch (resolution)
            {
                case Resolution.Minute:
                    interval = "M1";
                    break;

                case Resolution.Hour:
                    interval = "H1";
                    break;

                case Resolution.Daily:
                    interval = "D1";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution,
                                                          "tick or second resolution are not supported for Forex Volume.");
            }
            return interval;
        }

        /// <summary>
        ///     Generates the API Requests.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="startUtc">The start date in UTC.</param>
        /// <param name="endUtc">The end date in UTC.</param>
        /// <returns></returns>
        private string[] RequestData(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var startDate = string.Empty;
            var endDate = endUtc.AddDays(value: 2).ToStringInvariant("yyyyMMdd") + "2100";
            var symbolId = GetFxcmIDFromSymbol(symbol);
            var interval = GetIntervalFromResolution(resolution);
            switch (resolution)
            {
                case Resolution.Minute:
                case Resolution.Hour:
                    startDate = startUtc.ToStringInvariant("yyyyMMdd") + "0000";
                    break;

                case Resolution.Daily:
                    startDate = startUtc.AddDays(value: 1).ToStringInvariant("yyyyMMdd") + "2100";
                    break;
            }

            var request = $"{_baseUrl}&ver={_ver}&sid={_sid}&interval={interval}&offerID={symbolId}" +
                $"&timeFrom={startDate.ToStringInvariant()}&timeTo={endDate.ToStringInvariant()}";

            string[] lines;
            using (var client = new WebClient())
            {
                var data = client.DownloadString(request);
                lines = data.Split('\n');
            }
            // Removes the HTML head and tail.
            return lines.Skip(count: 2).Take(lines.Length - 4).ToArray();
        }

        #endregion
    }
}