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
        private readonly IQFeedFileHistoryProvider _fileHistoryProvider;
        private readonly TickType _tickType;

        /// <summary>
        /// Initializes a new instance of the <see cref="IQFeedDataDownloader"/> class
        /// </summary>
        /// <param name="fileHistoryProvider"></param>
        public IQFeedDataDownloader(IQFeedFileHistoryProvider fileHistoryProvider)
        {
            _fileHistoryProvider = fileHistoryProvider;
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            Symbol symbol = dataDownloaderGetParameters.Symbol;
            Resolution resolution = dataDownloaderGetParameters.Resolution;
            DateTime startUtc = dataDownloaderGetParameters.StartUtc;
            DateTime endUtc = dataDownloaderGetParameters.EndUtc;
            TickType tickType = dataDownloaderGetParameters.TickType;

            if (tickType == TickType.OpenInterest)
                return Enumerable.Empty<BaseData>();

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
                    tickType));
        }
    }
}
