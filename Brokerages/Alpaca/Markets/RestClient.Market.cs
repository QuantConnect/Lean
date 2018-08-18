using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    public sealed partial class RestClient
    {
        /// <summary>
        /// Gets list of historical bars for several assets from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbols">List of asset names for data retrieval.</param>
        /// <param name="barDuration">Single historical bar duration.</param>
        /// <param name="startTimeInclusive">Start time for filtering (inclusive).</param>
        /// <param name="endTimeInclusive">End time for filtering (inclusive).</param>
        /// <returns>Read-only list of historical bars lists for specified assets.</returns>
        public Task<IEnumerable<IAssetBars>> ListBarsAsync(
            IEnumerable<String> symbols,
            BarDuration barDuration,
            DateTime? startTimeInclusive = null,
            DateTime? endTimeInclusive = null)
        {
            var builder = new UriBuilder(_alpacaHttpClient.BaseAddress)
            {
                Path = "v1/bars",
                Query = new QueryBuilder()
                    .AddParameter("symbols", String.Join(",", symbols))
                    .AddParameter("timeframe", barDuration.ToEnumString())
                    .AddParameter("start_dt", startTimeInclusive)
                    .AddParameter("end_dt", endTimeInclusive)
            };

            return getObjectsListAsync<IAssetBars, JsonAssetBars>(_alpacaHttpClient, builder);
        }

        /// <summary>
        /// Gets list of historical bars for single asset from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <param name="barDuration">Single historical bar duration.</param>
        /// <param name="startTimeInclusive">Start time for filtering (inclusive).</param>
        /// <param name="endTimeInclusive">End time for filtering (inclusive).</param>
        /// <returns>Read-only list of historical bars for specified asset.</returns>
        public Task<IAssetBars> ListBarsAsync(
            String symbol,
            BarDuration barDuration,
            DateTime? startTimeInclusive = null,
            DateTime? endTimeInclusive = null)
        {
            var builder = new UriBuilder(_alpacaHttpClient.BaseAddress)
            {
                Path = $"v1/assets/{symbol}/bars",
                Query = new QueryBuilder()
                    .AddParameter("timeframe ", barDuration.ToEnumString())
                    .AddParameter("start_dt", startTimeInclusive)
                    .AddParameter("end_dt", endTimeInclusive)
            };

            return getSingleObjectAsync<IAssetBars, JsonAssetBars>(_alpacaHttpClient, builder);
        }

        /// <summary>
        /// Gets list of current quotes for several assets from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbols">List of asset names for data retrieval.</param>
        /// <returns>Read-only list of current quotes lists for specified assets.</returns>
        public Task<IEnumerable<IQuote>> ListQuotesAsync(
            IEnumerable<String> symbols)
        {
            var builder = new UriBuilder(_alpacaHttpClient.BaseAddress)
            {
                Path = "v1/quotes",
                Query = new QueryBuilder()
                    .AddParameter("symbols", String.Join(",", symbols))
            };

            return getObjectsListAsync<IQuote, JsonQuote>(_alpacaHttpClient, builder);
        }

        /// <summary>
        /// Gets current quote for singe asset from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <returns>Read-only current quote information.</returns>
        public Task<IQuote> GetQuoteAsync(
            String symbol)
        {
            return getSingleObjectAsync<IQuote, JsonQuote>(
                _alpacaHttpClient, $"v1/assets/{symbol}/quote");
        }

        /// <summary>
        /// Gets list of fundamental data for several assets from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbols">List of asset names for data retrieval.</param>
        /// <returns>Read-only list of fundamental data for specified assets.</returns>
        public Task<IEnumerable<IFundamental>> ListFundamentalsAsync(
            IEnumerable<String> symbols)
        {
            var builder = new UriBuilder(_alpacaHttpClient.BaseAddress)
            {
                Path = "v1/fundamentals",
                Query = new QueryBuilder()
                    .AddParameter("symbols", String.Join(",", symbols))
            };

            return getObjectsListAsync<IFundamental, JsonFundamental>(_alpacaHttpClient, builder);
        }

        /// <summary>
        /// Gets fundamental data for singe asset from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <returns>Read-only fundamental data information.</returns>
        public Task<IFundamental> GetFundamentalAsync(
            String symbol)
        {
            return getSingleObjectAsync<IFundamental, JsonFundamental>(
                _alpacaHttpClient, $"v1/assets/{symbol}/fundamental");
        }
    }
}