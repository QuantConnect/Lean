using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.ToolBox.IQFeed;

namespace QuantConnect.ToolBox.IQFeedDownloader
{
    /// <summary>
    /// IQFeed Data Downloader class 
    /// </summary>
    public class IQFeedDataDownloader : IDataDownloader
    {
        private readonly IQConnect _iqConnect;
        private readonly HistoryPort _historyPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="IQFeedDataDownloader"/> class
        /// </summary>
        /// <param name="username">IQFeed user name</param>
        /// <param name="password">IQFeed password</param>
        /// <param name="productName">IQFeed product name</param>
        /// <param name="productVersion">IQFeed product version</param>
        public IQFeedDataDownloader(string userName, string password, string productName, string productVersion)
        {
            _iqConnect = new IQConnect(productName, productVersion);
            _iqConnect.Launch(new IQCredentials(userName, password, true, true));

            _historyPort = new HistoryPort(new IQFeedDataQueueUniverseProvider(), 0, 0);
            _historyPort.Connect();
            _historyPort.SetClientName("History");
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (symbol.ID.SecurityType != SecurityType.Equity)
                throw new NotSupportedException("SecurityType not available: " + symbol.ID.SecurityType);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal than the start date.");

            var dataType = resolution == Resolution.Tick ? typeof(Tick) : typeof(TradeBar);
            var tickType = resolution == Resolution.Tick ? TickType.Quote : TickType.Trade;

            var slices = _historyPort.ProcessHistoryRequests(
                new HistoryRequest(
                    startUtc,
                    endUtc,
                    dataType,
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    TimeZones.NewYork,
                    resolution,
                    true,
                    false,
                    DataNormalizationMode.Adjusted,
                    tickType)).ToList();

            switch (resolution)
            {
                case Resolution.Tick:
                    var ticks = new List<Tick>();
                    foreach (var slice in slices)
                    {
                        ticks.AddRange(slice.Ticks[symbol]);
                    }
                    return ticks;

                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    var tradeBars = new List<TradeBar>();
                    foreach (var slice in slices)
                    {
                        tradeBars.Add(slice.Bars[symbol]);
                    }
                    return tradeBars;

                default:
                    throw new NotSupportedException("Resolution not available: " + resolution);
            }
        }

        /// <summary>
        /// Aggregates a list of ticks at the requested resolution
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="ticks"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        internal static IEnumerable<TradeBar> AggregateTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan resolution)
        {
            return (from t in ticks
                    group t by t.Time.RoundDown(resolution) into g
                    select new TradeBar
                    {
                        Symbol = symbol,
                        Time = g.Key,
                        Open = g.First().LastPrice,
                        High = g.Max(t => t.LastPrice),
                        Low = g.Min(t => t.LastPrice),
                        Close = g.Last().LastPrice,
                        Volume = g.Sum(t => t.Quantity)
                    });
        }
    }
}
