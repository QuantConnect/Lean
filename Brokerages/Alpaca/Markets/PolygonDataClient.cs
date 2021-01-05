/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes:
 *   * default literals to default(T)
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Polygon Data API via HTTP/REST.
    /// </summary>
    public sealed partial class PolygonDataClient : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private readonly Boolean _isStagingEnvironment;

        private readonly String _keyId;

        /// <summary>
        /// Creates new instance of <see cref="PolygonDataClient"/> object.
        /// </summary>
        /// <param name="configuration">Configuration parameters object.</param>
        public PolygonDataClient(
            PolygonDataClientConfiguration configuration)
        {
            configuration
                .EnsureNotNull(nameof(configuration))
                .EnsureIsValid();

            _isStagingEnvironment = configuration.KeyId
                .EndsWith("-staging", StringComparison.Ordinal);
            _keyId = configuration.KeyId;

            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.BaseAddress = configuration.ApiEndpoint;
            _httpClient.SetSecurityProtocol();
        }

        /// <inheritdoc />
        public void Dispose() => _httpClient.Dispose();

        /// <summary>
        /// Gets list of available exchanges from Polygon REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of exchange information objects.</returns>
        public Task<IReadOnlyList<IExchange>> ListExchangesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = "v1/meta/exchanges",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return _httpClient.GetObjectsListAsync<IExchange, JsonExchange>(
                FakeThrottler.Instance, builder, cancellationToken);
        }

        /// <summary>
        /// Gets mapping dictionary for symbol types from Polygon REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>
        /// Read-only dictionary with keys equal to symbol type abbreviation and values
        /// equal to full symbol type names descriptions for each supported symbol type.
        /// </returns>
        public Task<IReadOnlyDictionary<String, String>> GetSymbolTypeMapAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = "v1/meta/symbol-types",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return _httpClient.GetSingleObjectAsync
                <IReadOnlyDictionary<String,String>, Dictionary<String, String>>(
                    FakeThrottler.Instance, builder, cancellationToken);
        }

        /// <summary>
        /// Gets list of historical trades for a single asset from Polygon's REST API endpoint.
        /// </summary>
        /// <param name="request">Historical trades request parameter.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of historical trade information.</returns>
        public Task<IHistoricalItems<IHistoricalTrade>> ListHistoricalTradesAsync(
            HistoricalRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = $"v2/ticks/stocks/trades/{request.Symbol}/{request.Date.AsDateString()}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("limit", request.Limit)
                    .AddParameter("timestamp", request.Timestamp)
                    .AddParameter("timestamp_limit", request.TimestampLimit)
                    .AddParameter("reverse", request.Reverse != null ? request.Reverse.ToString() : null)
            };

            return _httpClient.GetSingleObjectAsync
                <IHistoricalItems<IHistoricalTrade>, JsonHistoricalItems<IHistoricalTrade, JsonHistoricalTrade>>(
                    FakeThrottler.Instance, builder, cancellationToken);
        }

        /// <summary>
        /// Gets list of historical trades for a single asset from Polygon's REST API endpoint.
        /// </summary>
        /// <param name="request">Historical quotes request parameter.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of historical trade information.</returns>
        public Task<IHistoricalItems<IHistoricalQuote>> ListHistoricalQuotesAsync(
            HistoricalRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = $"v2/ticks/stocks/nbbo/{request.Symbol}/{request.Date.AsDateString()}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("limit", request.Limit)
                    .AddParameter("timestamp", request.Timestamp)
                    .AddParameter("timestamp_limit", request.TimestampLimit)
                    .AddParameter("reverse", request.Reverse != null ? request.Reverse.ToString() : null)
            };

            return _httpClient.GetSingleObjectAsync
                <IHistoricalItems<IHistoricalQuote>, JsonHistoricalItems<IHistoricalQuote, JsonHistoricalQuote>>(
                    FakeThrottler.Instance, builder, cancellationToken);
        }


        /// <summary>
        /// Gets list of historical minute bars for single asset from Polygon's v2 REST API endpoint.
        /// </summary>
        /// <param name="request">Day aggregates request parameter.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of day bars for specified asset.</returns>
        public Task<IHistoricalItems<IAgg>> ListAggregatesAsync(
            AggregatesRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var unixFrom = DateTimeHelper.GetUnixTimeMilliseconds(request.DateFrom);
            var unixTo = DateTimeHelper.GetUnixTimeMilliseconds(request.DateInto);

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = $"v2/aggs/ticker/{request.Symbol}/range/{request.Period.ToString()}/{unixFrom}/{unixTo}",
                Query = getDefaultPolygonApiQueryBuilder()
                    .AddParameter("unadjusted", request.Unadjusted ? Boolean.TrueString : Boolean.FalseString)
            };

            return _httpClient.GetSingleObjectAsync
                <IHistoricalItems<IAgg>, JsonHistoricalItems<IAgg, JsonMinuteAgg>>(
                    FakeThrottler.Instance, builder, cancellationToken);
        }

        /// <summary>
        /// Gets last trade for singe asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only last trade information.</returns>
        public Task<ILastTrade> GetLastTradeAsync(
            String symbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = $"v1/last/stocks/{symbol}",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return _httpClient.GetSingleObjectAsync<ILastTrade, JsonLastTrade>(
                FakeThrottler.Instance, builder, cancellationToken);
        }

        /// <summary>
        /// Gets current quote for singe asset from Polygon REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only current quote information.</returns>
        public Task<ILastQuote> GetLastQuoteAsync(
            String symbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = $"v1/last_quote/stocks/{symbol}",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            return _httpClient.GetSingleObjectAsync<ILastQuote, JsonLastQuote>(
                FakeThrottler.Instance, builder, cancellationToken);
        }

        /// <summary>
        /// Gets mapping dictionary for specific tick type from Polygon REST API endpoint.
        /// </summary>
        /// <param name="tickType">Tick type for conditions map.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>
        /// Read-only dictionary with keys equal to condition integer values and values
        /// equal to full tick condition descriptions for each supported tick type.
        /// </returns>
        public async Task<IReadOnlyDictionary<Int64, String>> GetConditionMapAsync(
            TickType tickType = TickType.Trades,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = $"v1/meta/conditions/{tickType.ToEnumString()}",
                Query = getDefaultPolygonApiQueryBuilder()
            };

            var dictionary = await _httpClient.GetSingleObjectAsync
                    <IDictionary<String, String>, Dictionary<String, String>>(
                        FakeThrottler.Instance, builder, cancellationToken)
                .ConfigureAwait(false);

            return dictionary
                .ToDictionary(
                    kvp => Int64.Parse(kvp.Key,
                        NumberStyles.Integer, CultureInfo.InvariantCulture),
                    kvp => kvp.Value);
        }

        private QueryBuilder getDefaultPolygonApiQueryBuilder()
        {
            var builder = new QueryBuilder()
                .AddParameter("apiKey", _keyId);

            if (_isStagingEnvironment)
            {
                builder.AddParameter("staging", "true");
            }

            return builder;
        }
    }
}
