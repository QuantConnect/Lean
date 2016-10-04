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
using NodaTime;
using QuantConnect.Brokerages.Oanda.DataType.Communications.Requests;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda Brokerage - implementation of IHistoryProvider interface
    /// </summary>
    public partial class OandaBrokerage
    {
        /// <summary>
        /// The maximum number of bars per historical data request
        /// </summary>
        public const int MaxBarsPerRequest = 5000;

        #region IHistoryProvider implementation

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount { get; private set; }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        /// <param name="statusUpdate">Function used to send status updates</param>
        public void Initialize(AlgorithmNodePacket job, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            foreach (var request in requests)
            {
                var granularity = ToGranularity(request.Resolution);

                // Oanda only has 5-second bars, we return these for Resolution.Second
                var period = request.Resolution == Resolution.Second ?
                    TimeSpan.FromSeconds(5) : request.Resolution.ToTimeSpan();

                // set the starting date/time
                var startDateTime = request.StartTimeUtc;

                // loop until last date
                while (startDateTime <= request.EndTimeUtc)
                {
                    var start = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    // request blocks of bars at the requested resolution with a starting date/time
                    var oandaSymbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
                    var candles = DownloadBars(oandaSymbol, start, MaxBarsPerRequest, granularity);
                    if (candles.Count == 0)
                        break;

                    foreach (var candle in candles)
                    {
                        var time = GetDateTimeFromString(candle.time);
                        if (time > request.EndTimeUtc)
                            break;

                        var tradeBar = new TradeBar(
                            time, 
                            request.Symbol,
                            Convert.ToDecimal(candle.openMid),
                            Convert.ToDecimal(candle.highMid),
                            Convert.ToDecimal(candle.lowMid),
                            Convert.ToDecimal(candle.closeMid),
                            0,
                            period);

                        DataPointCount++;

                        yield return new Slice(tradeBar.EndTime, new[] { tradeBar });
                    }

                    // calculate the next request datetime
                    startDateTime = GetDateTimeFromString(candles[candles.Count - 1].time).Add(period);
                }
            }
        }

        /// <summary>
        /// Converts a LEAN Resolution to an EGranularity
        /// </summary>
        /// <param name="resolution">The resolution to convert</param>
        /// <returns></returns>
        private static EGranularity ToGranularity(Resolution resolution)
        {
            EGranularity interval;

            switch (resolution)
            {
                case Resolution.Second:
                    interval = EGranularity.S5;
                    break;

                case Resolution.Minute:
                    interval = EGranularity.M1;
                    break;

                case Resolution.Hour:
                    interval = EGranularity.H1;
                    break;

                case Resolution.Daily:
                    interval = EGranularity.D;
                    break;

                default:
                    throw new ArgumentException("Unsupported resolution: " + resolution);
            }

            return interval;
        }

        #endregion
    }
}