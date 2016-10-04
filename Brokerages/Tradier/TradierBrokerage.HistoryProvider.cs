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
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Tradier Brokerage - IHistoryProvider implementation
    /// </summary>
    public partial class TradierBrokerage
    {
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
                if (request.Symbol.ID.SecurityType != SecurityType.Equity)
                {
                    throw new ArgumentException("Invalid security type: " + request.Symbol.ID.SecurityType);
                }

                if (request.StartTimeUtc >= request.EndTimeUtc)
                {
                    throw new ArgumentException("Invalid date range specified");
                }

                var start = request.StartTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);
                var end = request.EndTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);

                var history = Enumerable.Empty<Slice>();

                switch (request.Resolution)
                {
                    case Resolution.Tick:
                        history = GetHistoryTick(request.Symbol, start, end);
                        break;

                    case Resolution.Second:
                        history = GetHistorySecond(request.Symbol, start, end);
                        break;

                    case Resolution.Minute:
                        history = GetHistoryMinute(request.Symbol, start, end);
                        break;

                    case Resolution.Hour:
                        history = GetHistoryHour(request.Symbol, start, end);
                        break;

                    case Resolution.Daily:
                        history = GetHistoryDaily(request.Symbol, start, end);
                        break;
                }

                foreach (var slice in history)
                {
                    yield return slice;
                }
            }
        }

        private IEnumerable<Slice> GetHistoryTick(Symbol symbol, DateTime start, DateTime end)
        {
            var history = GetTimeSeries(symbol.Value, start, end, TradierTimeSeriesIntervals.Tick);

            if (history == null)
                return Enumerable.Empty<Slice>();

            DataPointCount += history.Count;

            return history
                .Select(tick => new Tick
                {
                    Time = tick.Time,
                    Symbol = symbol,
                    Value = tick.Price,
                    TickType = TickType.Trade,
                    Quantity = Convert.ToInt32(tick.Volume)
                })
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private IEnumerable<Slice> GetHistorySecond(Symbol symbol, DateTime start, DateTime end)
        {
            var history = GetTimeSeries(symbol.Value, start, end, TradierTimeSeriesIntervals.Tick);

            if (history == null)
                return Enumerable.Empty<Slice>();

            // aggregate ticks into 1 second bars
            var result = history
                .Select(tick => new Tick
                {
                    Time = tick.Time,
                    Symbol = symbol,
                    Value = tick.Price,
                    TickType = TickType.Trade,
                    Quantity = Convert.ToInt32(tick.Volume)
                })
                .GroupBy(x => x.Time.RoundDown(Time.OneSecond))
                .Select(g => new TradeBar(
                    g.Key, 
                    symbol, 
                    g.First().LastPrice, 
                    g.Max(t => t.LastPrice), 
                    g.Min(t => t.LastPrice), 
                    g.Last().LastPrice, 
                    g.Sum(t => t.Quantity),
                    Time.OneSecond))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }))
                .ToList();

            DataPointCount += result.Count;

            return result;
        }

        private IEnumerable<Slice> GetHistoryMinute(Symbol symbol, DateTime start, DateTime end)
        {
            var history = GetTimeSeries(symbol.Value, start, end, TradierTimeSeriesIntervals.OneMinute);

            if (history == null)
                return Enumerable.Empty<Slice>();

            DataPointCount += history.Count;

            return history
                .Select(bar => new TradeBar(bar.Time, symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, Time.OneMinute))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private IEnumerable<Slice> GetHistoryHour(Symbol symbol, DateTime start, DateTime end)
        {
            var history = GetTimeSeries(symbol.Value, start, end, TradierTimeSeriesIntervals.FifteenMinutes);

            if (history == null)
                return Enumerable.Empty<Slice>();

            // aggregate 15 minute bars into hourly bars
            var result = history
                .Select(bar => new TradeBar(bar.Time, symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, Time.OneHour))
                .GroupBy(x => x.Time.RoundDown(Time.OneHour))
                .Select(g => new TradeBar(
                    g.Key,
                    symbol,
                    g.First().Open,
                    g.Max(t => t.High),
                    g.Min(t => t.Low),
                    g.Last().Close,
                    g.Sum(t => t.Volume),
                    Time.OneHour))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }))
                .ToList();

            DataPointCount += result.Count;

            return result;
        }

        private IEnumerable<Slice> GetHistoryDaily(Symbol symbol, DateTime start, DateTime end)
        {
            var history = GetHistoricalData(symbol.Value, start, end);

            DataPointCount += history.Count;

            return history
                .Select(bar => new TradeBar(bar.Time, symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, Time.OneDay))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        #endregion
    }
}
