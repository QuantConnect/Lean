/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed partial class RestClient
    {
        /// <summary>
        /// Gets list of available exchanes from Polygon REST API endpoint.
        /// </summary>
        /// <returns>Read-only list of exchange information objects.</returns>
        public Task<IEnumerable<IExchange>> ListExchangesAsync()
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = "v1/meta/exchanges",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return getObjectsListAsync<IExchange, JsonExchange>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets mapping dictionary for symbol types from Polygon REST API endpoint.
        /// </summary>
        /// <returns>
        /// Read-only dictionary with keys equal to symbol type abbreviation and values
        /// equal to full symbol type names descriptions for each supported symbol type.
        /// </returns>
        public Task<IReadOnlyDictionary<String, String>> GetSymbolTypeMapAsync()
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = "v1/meta/symbol-types",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return getSingleObjectAsync
                <IReadOnlyDictionary<String,String>, Dictionary<String, String>>(
                    _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets list of historical trades for single asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">>Asset name for data retrieval.</param>
        /// <param name="date">Single date for data retrieval.</param>
        /// <param name="offset">Paging - offset or first historical trade in days trades llist.</param>
        /// <param name="limit">Paging - maximal number of historical trades in data response.</param>
        /// <returns>Read-only list of historical trade information.</returns>
        public Task<IDayHistoricalItems<IHistoricalTrade>> ListHistoricalTradesAsync(
            String symbol,
            DateTime date,
            Int64? offset = null,
            Int32? limit = null)
        {
            var dateAsString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/historic/trades/{symbol}/{dateAsString}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("offset", offset)
                    .AddParameter("limit", limit)
            };

            return getSingleObjectAsync
                <IDayHistoricalItems<IHistoricalTrade>,
                    JsonDayHistoricalItems<IHistoricalTrade, JsonHistoricalTrade>>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets list of historical quotes for single asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">>Asset name for data retrieval.</param>
        /// <param name="date">Single date for data retrieval.</param>
        /// <param name="offset">Paging - offset or first historical quote in days quotes llist.</param>
        /// <param name="limit">Paging - maximal number of historical quotes in data response.</param>
        /// <returns>Read-only list of historical quote information.</returns>
        public Task<IDayHistoricalItems<IHistoricalQuote>> ListHistoricalQuotesAsync(
            String symbol,
            DateTime date,
            Int64? offset = null,
            Int32? limit = null)
        {
            var dateAsString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/historic/quotes/{symbol}/{dateAsString}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("offset", offset)
                    .AddParameter("limit", limit)
            };

            return getSingleObjectAsync
                <IDayHistoricalItems<IHistoricalQuote>,
                    JsonDayHistoricalItems<IHistoricalQuote, JsonHistoricalQuote>>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets list of historical daily bars for single asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">>Asset name for data retrieval.</param>
        /// <param name="dateFromInclusive">Start time for filtering (inclusive).</param>
        /// <param name="dateIntoInclusive">End time for filtering (inclusive).</param>
        /// <param name="limit">Maximal number of daily bars in data response.</param>
        /// <returns>Read-only list of daily bars for specified asset.</returns>
        public Task<IAggHistoricalItems<IAgg>> ListDayAggregatesAsync(
            String symbol,
            DateTime? dateFromInclusive = null,
            DateTime? dateIntoInclusive = null,
            Int32? limit = null)
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/historic/agg/day/{symbol}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("from", dateFromInclusive, "yyyy-MM-dd")
                    .AddParameter("to", dateIntoInclusive, "yyyy-MM-dd")
                    .AddParameter("limit", limit)
            };

            return getSingleObjectAsync
                <IAggHistoricalItems<IAgg>,
                    JsonAggHistoricalItems<IAgg, JsonDayAgg>>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets list of historical minute bars for single asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">>Asset name for data retrieval.</param>
        /// <param name="dateFromInclusive">Start time for filtering (inclusive).</param>
        /// <param name="dateIntoInclusive">End time for filtering (inclusive).</param>
        /// <param name="limit">Maximal number of minute bars in data response.</param>
        /// <returns>Read-only list of minute bars for specified asset.</returns>
        public Task<IAggHistoricalItems<IAgg>> ListMinuteAggregatesAsync(
            String symbol,
            DateTime? dateFromInclusive = null,
            DateTime? dateIntoInclusive = null,
            Int32? limit = null)
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/historic/agg/minute/{symbol}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("from", dateFromInclusive)
                    .AddParameter("to", dateIntoInclusive)
                    .AddParameter("limit", limit)
            };

            return getSingleObjectAsync
                <IAggHistoricalItems<IAgg>,
                    JsonAggHistoricalItems<IAgg, JsonMinuteAgg>>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets last trade for singe asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <returns>Read-only last trade information.</returns>
        public Task<ILastTrade> GetLastTradeAsync(
            String symbol)
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/last/stocks/{symbol}",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return getSingleObjectAsync<ILastTrade, JsonLastTrade>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets current quote for singe asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <returns>Read-only current quote information.</returns>
        public Task<ILastQuote> GetLastQuoteAsync(
            String symbol)
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/last_quote/stocks/{symbol}",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return getSingleObjectAsync<ILastQuote, JsonLastQuote>(
                _polygonHttpClient, FakeThrottler.Instance, builder);
        }

        /// <summary>
        /// Gets mapping dictionary for specific tick type from Polygon REST API endpoint.
        /// </summary>
        /// <param name="tickType">Tick type for conditions map.</param>
        /// <returns>
        /// Read-only dictionary with keys equal to condition integer values and values
        /// equal to full tick condition descriptions for each supported tick type.
        /// </returns>
        public async Task<IReadOnlyDictionary<Int64, String>> GetConditionMapAsync(
            TickType tickType = TickType.Trades)
        {
            var builder = new UriBuilder(_polygonHttpClient.BaseAddress)
            {
                Path = $"v1/meta/conditions/{tickType.ToEnumString()}",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            var dictionary = await getSingleObjectAsync
                <IDictionary<String, String>, Dictionary<String, String>>(
                    _polygonHttpClient, FakeThrottler.Instance, builder);

            return dictionary
                .ToDictionary(
                    kvp => Int64.Parse(kvp.Key,
                        NumberStyles.Integer, CultureInfo.InvariantCulture),
                    kvp => kvp.Value);
        }

        private QueryBuilder getDefaultPolygonApiQueryBuilder()
        {
            var builder = new QueryBuilder()
                .AddParameter("apiKey", _polygonApiKey);

            if (_isPolygonStaging)
            {
                builder.AddParameter("staging", "true");
            }

            return builder;
        }
    }
}
