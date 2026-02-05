/*
 * Cascade Labs - Hyperliquid Data Downloader
 * Wraps HyperliquidHistoryProvider for IDataDownloader interface
 */

using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Data downloader that uses HyperliquidHistoryProvider for historical data
    /// </summary>
    public class HyperliquidDataDownloader : IDataDownloader, IDisposable
    {
        private readonly HyperliquidHistoryProvider _historyProvider;
        private readonly MarketHoursDatabase _marketHoursDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperliquidDataDownloader"/> class
        /// </summary>
        public HyperliquidDataDownloader()
        {
            _historyProvider = new HyperliquidHistoryProvider();
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">Parameters for the historical data request</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData>? Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            var dataType = LeanData.GetDataType(resolution, tickType);
            var exchangeHours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var dataTimeZone = _marketHoursDatabase.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);

            var historyRequest = new HistoryRequest(
                startUtc,
                endUtc,
                dataType,
                symbol,
                resolution,
                exchangeHours,
                dataTimeZone,
                resolution,
                includeExtendedMarketHours: true,
                isCustomData: false,
                DataNormalizationMode.Raw,
                tickType);

            return _historyProvider.GetHistory(historyRequest);
        }

        /// <summary>
        /// Disposes of the downloader and underlying provider
        /// </summary>
        public void Dispose()
        {
            _historyProvider.Dispose();
        }
    }
}
