using System;
using System.Collections.Generic;
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
        private readonly IQFeedFileHistoryProvider _fileHistoryProvider;
        private readonly TickType _tickType;

        /// <summary>
        /// Initializes a new instance of the <see cref="IQFeedDataDownloader"/> class
        /// </summary>
        /// <param name="fileHistoryProvider"></param>
        public IQFeedDataDownloader(IQFeedFileHistoryProvider fileHistoryProvider)
        {
            _fileHistoryProvider = fileHistoryProvider;
            _tickType = TickType.Trade;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IQFeedDataDownloader"/> class
        /// </summary>
        /// <param name="fileHistoryProvider"></param>
        /// <param name="tickType"></param>
        public IQFeedDataDownloader(IQFeedFileHistoryProvider fileHistoryProvider, TickType tickType)
        {
            _fileHistoryProvider = fileHistoryProvider;
            _tickType = tickType;
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

            return _fileHistoryProvider.ProcessHistoryRequests(
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
                    _tickType));
        }
    }
}
