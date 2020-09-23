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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IQFeed.CSharpApiClient.Lookup;
using IQFeed.CSharpApiClient.Lookup.Historical.Enums;
using IQFeed.CSharpApiClient.Lookup.Historical.Messages;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.IQFeed
{
    /// <summary>
    /// IQFeed history provider downloading data directly to disk to reduce memory impact when processing large tick request.
    /// This provider also enables concurrent file download.
    /// </summary>
    public class IQFeedFileHistoryProvider
    {
        private readonly LookupClient _lookupClient;
        private readonly ISymbolMapper _symbolMapper;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly ConcurrentDictionary<string, string> _filesByRequestKeyCache;

        public IQFeedFileHistoryProvider(LookupClient lookupClient, ISymbolMapper symbolMapper, MarketHoursDatabase marketHoursDatabase)
        {
            _lookupClient = lookupClient;
            _symbolMapper = symbolMapper;
            _marketHoursDatabase = marketHoursDatabase;
            _filesByRequestKeyCache = new ConcurrentDictionary<string, string>();
        }

        public IEnumerable<BaseData> ProcessHistoryRequests(HistoryRequest request)
        {
            // skipping universe and canonical symbols
            if (!CanHandle(request.Symbol) ||
                request.Symbol.ID.SecurityType == SecurityType.Option && request.Symbol.IsCanonical() ||
                request.Symbol.ID.SecurityType == SecurityType.Future && request.Symbol.IsCanonical())
            {
                return Enumerable.Empty<BaseData>();
            }

            // skipping empty ticker
            var ticker = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            if (string.IsNullOrEmpty(ticker))
            {
                Log.Trace($"IQFeedHistoryProvider.ProcessHistoryRequests(): Unable to retrieve ticker from Symbol: ${request.Symbol}");
                return Enumerable.Empty<BaseData>();
            }

            var start = request.StartTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            DateTime? end = request.EndTimeUtc.ConvertFromUtc(TimeZones.NewYork);

            // if we're within a minute of now, don't set the end time
            if (request.EndTimeUtc >= DateTime.UtcNow.AddMinutes(-1))
            {
                end = null;
            }

            Log.Trace(
                $"IQFeedHistoryProvider.ProcessHistoryRequests(): Submitting request: {request.Symbol.SecurityType.ToStringInvariant()}-{ticker}: " +
                $"{request.Resolution.ToStringInvariant()} {start.ToStringInvariant()}->{(end ?? DateTime.UtcNow.AddMinutes(-1)).ToStringInvariant()}"
            );

            return GetDataFromFile(request, ticker, start, end);
        }

        private IEnumerable<BaseData> GetDataFromFile(HistoryRequest request, string ticker, DateTime startDate, DateTime? endDate)
        {
            try
            {
                string filename;

                switch (request.Resolution)
                {
                    case Resolution.Tick:
                        var requestKey = GetHistoryRequestKey(ticker, startDate, endDate);
                        var tickFunc = request.TickType == TickType.Trade ? new Func<DateTime, Symbol, TickMessage, Tick>(CreateTradeTick) : CreateQuoteTick;

                        if (_filesByRequestKeyCache.TryRemove(requestKey, out filename))
                            return GetDataFromTickMessages(filename, request, tickFunc, true);
                        
                        filename = _lookupClient.Historical.File.GetHistoryTickTimeframeAsync(ticker, startDate, endDate, dataDirection: DataDirection.Oldest).SynchronouslyAwaitTaskResult();
                        _filesByRequestKeyCache.AddOrUpdate(requestKey, filename);
                        return GetDataFromTickMessages(filename, request, tickFunc, false);

                    case Resolution.Daily:
                        filename = _lookupClient.Historical.File.GetHistoryDailyTimeframeAsync(ticker, startDate, endDate, dataDirection: DataDirection.Oldest).SynchronouslyAwaitTaskResult();
                        return GetDataFromDailyMessages(filename, request);

                    default:
                        var interval = new Interval(GetPeriodType(request.Resolution), 1);
                        filename = _lookupClient.Historical.File.GetHistoryIntervalTimeframeAsync(ticker, interval.Seconds, startDate, endDate, dataDirection: DataDirection.Oldest).SynchronouslyAwaitTaskResult();
                        return GetDataFromIntervalMessages(filename, request);
                }
            }
            catch (Exception e)
            {
                Log.Error($"IQFeedHistoryProvider.GetDataFromFile(): {e}");
            }

            return Enumerable.Empty<BaseData>();
        }

        /// <summary>
        /// Stream IQFeed TickMessages from disk to Lean Tick
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="request"></param>
        /// <param name="tickFunc"></param>
        /// <param name="delete"></param>
        /// <returns>Converted Tick</returns>
        private IEnumerable<BaseData> GetDataFromTickMessages(string filename, HistoryRequest request, Func<DateTime, Symbol, TickMessage, Tick> tickFunc, bool delete)
        {
            var dataTimeZone = _marketHoursDatabase.GetDataTimeZone(request.Symbol.ID.Market, request.Symbol, request.Symbol.SecurityType);

            // We need to discard ticks which are not impacting the price, i.e those having BasisForLast = O
            // To get a better understanding how IQFeed is resampling ticks, have a look to this algorithm:
            // https://github.com/mathpaquette/IQFeed.CSharpApiClient/blob/1b33250e057dfd6cd77e5ee35fa16aebfc8fbe79/src/IQFeed.CSharpApiClient.Extensions/Lookup/Historical/Resample/TickMessageExtensions.cs#L41
            foreach (var tick in TickMessage.ParseFromFile(filename).Where(t => t.BasisForLast != 'O'))
            {
                var timestamp = tick.Timestamp.ConvertTo(TimeZones.NewYork, dataTimeZone);
                yield return tickFunc(timestamp, request.Symbol, tick);
            }

            if (delete)
                File.Delete(filename);
        }

        /// <summary>
        /// Stream IQFeed DailyWeeklyMonthlyMessage from disk to Lean TradeBar
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="request"></param>
        /// <returns>Converted TradeBar</returns>
        private IEnumerable<BaseData> GetDataFromDailyMessages(string filename, HistoryRequest request)
        {
            var dataTimeZone = _marketHoursDatabase.GetDataTimeZone(request.Symbol.ID.Market, request.Symbol, request.Symbol.SecurityType);

            foreach (var daily in DailyWeeklyMonthlyMessage.ParseFromFile(filename))
            {
                var dStartTime = daily.Timestamp;
                dStartTime = dStartTime.ConvertTo(TimeZones.NewYork, dataTimeZone);
                yield return new TradeBar(
                    dStartTime,
                    request.Symbol,
                    (decimal)daily.Open,
                    (decimal)daily.High,
                    (decimal)daily.Low,
                    (decimal)daily.Close,
                    daily.PeriodVolume,
                    request.Resolution.ToTimeSpan()
                );
            }

            File.Delete(filename);
        }

        /// <summary>
        /// Stream IQFeed IntervalMessage from disk to Lean TradeBar
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="request"></param>
        /// <returns>Converted TradeBar</returns>
        private IEnumerable<BaseData> GetDataFromIntervalMessages(string filename, HistoryRequest request)
        {
            var dataTimeZone = _marketHoursDatabase.GetDataTimeZone(request.Symbol.ID.Market, request.Symbol, request.Symbol.SecurityType);

            foreach (var interval in IntervalMessage.ParseFromFile(filename))
            {
                var iStartTime = interval.Timestamp;
                iStartTime = iStartTime.ConvertTo(TimeZones.NewYork, dataTimeZone);
                yield return new TradeBar(
                    iStartTime,
                    request.Symbol,
                    (decimal)interval.Open,
                    (decimal)interval.High,
                    (decimal)interval.Low,
                    (decimal)interval.Close,
                    interval.PeriodVolume
                );
            }

            File.Delete(filename);
        }

        /// <summary>
        /// Returns true if this data provide can handle the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to be handled</param>
        /// <returns>True if this data provider can get data for the symbol, false otherwise</returns>
        private bool CanHandle(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;
            return
                (securityType == SecurityType.Equity && market == Market.USA) ||
                (securityType == SecurityType.Forex && market == Market.FXCM) ||
                (securityType == SecurityType.Option && market == Market.USA) ||
                (securityType == SecurityType.Future);
        }

        /// <summary>
        /// Create Trade Tick from TickMessage
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="symbol"></param>
        /// <param name="tick"></param>
        /// <returns>Trade Tick</returns>
        private static Tick CreateTradeTick(DateTime timestamp, Symbol symbol, ITickMessage tick)
        {
            return new Tick(
                timestamp,
                symbol,
                tick.TradeConditions,
                tick.TradeMarketCenter.ToStringInvariant(),
                tick.LastSize,
                (decimal)tick.Last
            );
        }

        /// <summary>
        /// Create Quote Tick from TickMessage
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="symbol"></param>
        /// <param name="tick"></param>
        /// <returns>Quote Tick</returns>
        private static Tick CreateQuoteTick(DateTime timestamp, Symbol symbol, ITickMessage tick)
        {
            return new Tick(
                timestamp,
                symbol,
                tick.TradeConditions,
                tick.TradeMarketCenter.ToStringInvariant(),
                0, // not provided by IQFeed on history
                (decimal)tick.Bid,
                0, // not provided by IQFeed on history
                (decimal)tick.Ask
            );
        }

        /// <summary>
        /// Generate unique key from history request parameters
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private static string GetHistoryRequestKey(string ticker, DateTime startDate, DateTime? endDate)
        {
            return $"{ticker}-{startDate}-{endDate}";
        }

        private static PeriodType GetPeriodType(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Second:
                    return PeriodType.Second;
                case Resolution.Minute:
                    return PeriodType.Minute;
                case Resolution.Hour:
                    return PeriodType.Hour;
                case Resolution.Tick:
                case Resolution.Daily:
                default:
                    throw new ArgumentOutOfRangeException("resolution", resolution, null);
            }
        }
    }
}