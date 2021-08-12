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
using TDAmeritradeApi.Client;
using TDAmeritradeApi.Client.Models.MarketData;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    /// <summary>
    /// Tradier Brokerage - IHistoryProvider implementation
    /// </summary>
    public partial class TDAmeritradeBrokerage : IHistoryProvider
    {
        #region IHistoryProvider implementation

        /// <summary>
        /// Event fired when an invalid configuration has been detected
        /// </summary>
        public event EventHandler<InvalidConfigurationDetectedEventArgs> InvalidConfigurationDetected;

        /// <summary>
        /// Event fired when the numerical precision in the factor file has been limited
        /// </summary>
        public event EventHandler<NumericalPrecisionLimitedEventArgs> NumericalPrecisionLimited;

        /// <summary>
        /// Event fired when there was an error downloading a remote file
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        /// Event fired when there was an error reading the data
        /// </summary>
        public event EventHandler<ReaderErrorDetectedEventArgs> ReaderErrorDetected;

        /// <summary>
        /// Event fired when the start date has been limited
        /// </summary>
#pragma warning disable 0067 // StartDateLimited is currently not used; remove once implemented
        public event EventHandler<StartDateLimitedEventArgs> StartDateLimited;
#pragma warning restore 0067

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount { get; private set; }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public void Initialize(HistoryProviderInitializeParameters parameters)
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
                var history = GetPriceHistory(tdClient, request.Symbol, request.Resolution, request.StartTimeUtc, request.EndTimeUtc, sliceTimeZone);

                DataPointCount += history.Count();

                foreach (var slice in TradeBarsToSlices(history))
                {
                    yield return slice;
                }
            }
        }

        /// <summary>
        /// Method for getting price history
        /// </summary>
        /// <param name="client"></param>
        /// <param name="symbol"></param>
        /// <param name="resolution"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static IEnumerable<TradeBar> GetPriceHistory(TDAmeritradeClient client, Symbol symbol, Resolution resolution, DateTime startDate, DateTime endDate, DateTimeZone sliceTimeZone)
        {
            if (startDate >= endDate)
            {
                throw new ArgumentException("Invalid date range specified");
            }

            var start = startDate;//.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);
            var end = endDate;//.ConvertTo(DateTimeZone.Utc, TimeZones.NewYork);

            var history = Enumerable.Empty<TradeBar>();

            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                    break;

                case Resolution.Minute:
                    history = GetHistoryMinute(client, symbol, start, end, sliceTimeZone);
                    break;

                case Resolution.Hour:
                    history = GetHistoryHour(client, symbol, start, end, sliceTimeZone);
                    break;

                case Resolution.Daily:
                    history = GetHistoryDaily(client, symbol, start, end, sliceTimeZone);
                    break;
            }

            return history;
        }    

        /// <summary>
        /// Event invocator for the <see cref="InvalidConfigurationDetected"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="InvalidConfigurationDetected"/> event</param>
        protected virtual void OnInvalidConfigurationDetected(InvalidConfigurationDetectedEventArgs e)
        {
            InvalidConfigurationDetected?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="NumericalPrecisionLimited"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="NumericalPrecisionLimited"/> event</param>
        protected virtual void OnNumericalPrecisionLimited(NumericalPrecisionLimitedEventArgs e)
        {
            NumericalPrecisionLimited?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="DownloadFailed"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="DownloadFailed"/> event</param>
        protected virtual void OnDownloadFailed(DownloadFailedEventArgs e)
        {
            DownloadFailed?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="ReaderErrorDetected"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="ReaderErrorDetected"/> event</param>
        protected virtual void OnReaderErrorDetected(ReaderErrorDetectedEventArgs e)
        {
            ReaderErrorDetected?.Invoke(this, e);
        }

        private static IEnumerable<TradeBar> GetHistoryMinute(TDAmeritradeClient tdClient, Symbol symbol, DateTime start, DateTime end, DateTimeZone sliceTimeZone)
        {
            string brokerageSymbol = TDAmeritradeToLeanMapper.GetBrokerageSymbol(symbol);

            var history = tdClient.MarketDataApi.GetPriceHistoryAsync(brokerageSymbol, frequencyType: FrequencyType.minute, frequency: 1, startDate: new DateTimeOffset(start), endDate: new DateTimeOffset(end)).Result;

            return CandlesToTradeBars(symbol, history, Time.OneMinute, sliceTimeZone);
        }

        private IEnumerable<Slice> CandlesToSlices(Symbol symbol, CandleList history, TimeSpan time, DateTimeZone sliceTimeZone)
        {
            var tradeBars = CandlesToTradeBars(symbol, history, time, sliceTimeZone);
            
            return TradeBarsToSlices(tradeBars);
        }

        private static IEnumerable<Slice> TradeBarsToSlices(IEnumerable<TradeBar> tradeBars)
        {
            if (tradeBars == null || !tradeBars.Any())
                return Enumerable.Empty<Slice>();

            return tradeBars.Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private static IEnumerable<TradeBar> CandlesToTradeBars(Symbol symbol, CandleList history, TimeSpan time, DateTimeZone sliceTimeZone)
        {
            if (history == null)
                return Enumerable.Empty<TradeBar>();

            return history.candles
                            .Select(candle => new TradeBar(candle.datetime.ConvertTo(DateTimeZone.Utc, sliceTimeZone), symbol, candle.open, candle.high, candle.low, candle.close, candle.volume, time));
        }

        private static IEnumerable<TradeBar> GetHistoryHour(TDAmeritradeClient tdClient, Symbol symbol, DateTime start, DateTime end, DateTimeZone sliceTimeZone)
        {
            string brokerageSymbol = TDAmeritradeToLeanMapper.GetBrokerageSymbol(symbol);

            var history = tdClient.MarketDataApi.GetPriceHistoryAsync(brokerageSymbol, frequencyType: FrequencyType.minute, frequency: 30, startDate: new DateTimeOffset(start), endDate: new DateTimeOffset(end)).Result;

            var tradeBars = CandlesToTradeBars(symbol, history, Time.OneHour, sliceTimeZone);

            var aggregatedBars = tradeBars
                .GroupBy(x => x.Time.RoundDown(Time.OneHour))
                .Select(g => new TradeBar(
                    g.Key,
                    symbol,
                    g.First().Open,
                    g.Max(t => t.High),
                    g.Min(t => t.Low),
                    g.Last().Close,
                    g.Sum(t => t.Volume),
                    Time.OneHour));

            return aggregatedBars;
        }

        private static IEnumerable<TradeBar> GetHistoryDaily(TDAmeritradeClient tdClient, Symbol symbol, DateTime start, DateTime end, DateTimeZone sliceTimeZone)
        {
            string brokerageSymbol = TDAmeritradeToLeanMapper.GetBrokerageSymbol(symbol);

            var history = tdClient.MarketDataApi.GetPriceHistoryAsync(brokerageSymbol, periodType: PeriodType.year, frequencyType: FrequencyType.daily, frequency: 1, startDate: new DateTimeOffset(start), endDate: new DateTimeOffset(end)).Result;

            return CandlesToTradeBars(symbol, history, Time.OneDay, sliceTimeZone);
        }

        #endregion
    }
}
