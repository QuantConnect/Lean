using System;
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

namespace QuantConnect.ToolBox.IQFeed
{
    /// <summary>
    /// IQFeed history provider downloading data directly to disk to reduce memory impact when processing large tick request.
    /// This provider also enables concurrent file download.
    /// </summary>
    public class IQFeedFileHistoryProvider
    {
        private readonly ISymbolMapper _symbolMapper;
        private readonly LookupClient _lookupClient;

        public IQFeedFileHistoryProvider(LookupClient lookupClient, ISymbolMapper symbolMapper)
        {
            _lookupClient = lookupClient;
            _symbolMapper = symbolMapper;
        }

        public IEnumerable<BaseData> ProcessHistoryRequests(HistoryRequest request)
        {
            // skipping universe and canonical symbols
            if (!CanHandle(request.Symbol) ||
                (request.Symbol.ID.SecurityType == SecurityType.Option && request.Symbol.IsCanonical()) ||
                (request.Symbol.ID.SecurityType == SecurityType.Future && request.Symbol.IsCanonical()))
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

            var isEquity = request.Symbol.SecurityType == SecurityType.Equity;

            Log.Trace(
                $"IQFeedHistoryProvider.ProcessHistoryRequests(): Submitting request: {request.Symbol.SecurityType.ToStringInvariant()}-{ticker}: " +
                $"{request.Resolution.ToStringInvariant()} {start.ToStringInvariant()}->{(end ?? DateTime.UtcNow.AddMinutes(-1)).ToStringInvariant()}"
            );

            return GetDataFromFile(request, ticker, start, end, isEquity);
        }

        private IEnumerable<BaseData> GetDataFromFile(HistoryRequest request, string ticker, DateTime startDate, DateTime? endDate, bool isEquity)
        {
            try
            {
                string filename;

                switch (request.Resolution)
                {
                    case Resolution.Tick:
                        filename = _lookupClient.Historical.File.GetHistoryTickTimeframeAsync(ticker, startDate, endDate, dataDirection: DataDirection.Oldest).SynchronouslyAwaitTaskResult();
                        return GetDataFromTickMessages(filename, request, isEquity);

                    case Resolution.Daily:
                        filename = _lookupClient.Historical.File.GetHistoryDailyTimeframeAsync(ticker, startDate, endDate, dataDirection: DataDirection.Oldest).SynchronouslyAwaitTaskResult();
                        return GetDataFromDailyMessages(filename, request, isEquity);

                    default:
                        var interval = new Interval(GetPeriodType(request.Resolution), 1);
                        filename = _lookupClient.Historical.File.GetHistoryIntervalTimeframeAsync(ticker, interval.Seconds, startDate, endDate, dataDirection: DataDirection.Oldest).SynchronouslyAwaitTaskResult();
                        return GetDataFromIntervalMessages(filename, request, isEquity);
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
        /// <param name="isEquity"></param>
        /// <returns>Converted Tick</returns>
        private IEnumerable<BaseData> GetDataFromTickMessages(string filename, HistoryRequest request, bool isEquity)
        {
            // We need to discard ticks which are not impacting the price, i.e those having BasisForLast = O
            // To get a better understanding how IQFeed is resampling ticks, have a look to this algorithm:
            // https://github.com/mathpaquette/IQFeed.CSharpApiClient/blob/1b33250e057dfd6cd77e5ee35fa16aebfc8fbe79/src/IQFeed.CSharpApiClient.Extensions/Lookup/Historical/Resample/TickMessageExtensions.cs#L41
            foreach (var tick in TickMessage.ParseFromFile(filename).Where(t => t.BasisForLast != 'O'))
            {
                var time = isEquity ? tick.Timestamp : tick.Timestamp.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                yield return new Tick(time, request.Symbol, (decimal)tick.Last, (decimal)tick.Bid, (decimal)tick.Ask)
                { Quantity = tick.LastSize };
            }

            File.Delete(filename);
        }

        /// <summary>
        /// Stream IQFeed DailyWeeklyMonthlyMessage from disk to Lean TradeBar
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="request"></param>
        /// <param name="isEquity"></param>
        /// <returns>Converted TradeBar</returns>
        private IEnumerable<BaseData> GetDataFromDailyMessages(string filename, HistoryRequest request, bool isEquity)
        {
            foreach (var daily in DailyWeeklyMonthlyMessage.ParseFromFile(filename))
            {
                var dstartTime = daily.Timestamp.Date;
                if (!isEquity) dstartTime = dstartTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                yield return new TradeBar(
                    dstartTime,
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
        /// <param name="isEquity"></param>
        /// <returns>Converted TradeBar</returns>
        private IEnumerable<BaseData> GetDataFromIntervalMessages(string filename, HistoryRequest request, bool isEquity)
        {
            foreach (var interval in IntervalMessage.ParseFromFile(filename))
            {
                var istartTime = interval.Timestamp - request.Resolution.ToTimeSpan();
                if (!isEquity) istartTime = istartTime.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
                yield return new TradeBar(
                    istartTime,
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