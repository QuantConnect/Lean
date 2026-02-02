/*
 * Cascade Labs - Kalshi REST API Client
 * HTTP client with RSA PSS signature authentication
 */

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// REST client for Kalshi API with RSA PSS signature authentication
    /// </summary>
    public class CascadeKalshiDataRestClient : IDisposable
    {
        private const string ApiBaseUrl = "https://api.elections.kalshi.com";
        private const string ApiPath = "/trade-api/v2";
        private const int MaxRetries = 3;
        private const int BaseRetryDelayMs = 1000;

        private readonly string _apiKey;
        private readonly string _privateKeyPem;
        private readonly HttpClient _httpClient;
        private readonly RateGate _rateGate;
        private readonly ConcurrentDictionary<string, KalshiMarket> _marketCache = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new Kalshi REST client with RSA PSS authentication
        /// </summary>
        /// <param name="rateGate">Rate limiter for API requests</param>
        public CascadeKalshiDataRestClient(RateGate rateGate)
        {
            _apiKey = Config.Get("kalshi-api-key", string.Empty);
            var privateKeyBase64 = Config.Get("kalshi-private-key", string.Empty);

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("kalshi-api-key configuration is required");
            }

            if (string.IsNullOrEmpty(privateKeyBase64))
            {
                throw new InvalidOperationException("kalshi-private-key configuration is required");
            }

            // Decode base64 PEM content
            try
            {
                _privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyBase64));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("kalshi-private-key must be base64 encoded PEM content", ex);
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromMinutes(2)
            };

            _rateGate = rateGate;

            Log.Trace($"CascadeKalshiDataRestClient: Initialized with API key {_apiKey[..Math.Min(8, _apiKey.Length)]}...");
        }

        /// <summary>
        /// Create RSA PSS signature for request authentication
        /// </summary>
        private string CreateSignature(string method, string path, long timestampMs)
        {
            // Message format: {timestamp_ms}{METHOD}/trade-api/v2{path}
            // Note: path should not include query string for signature
            var pathWithoutQuery = path.Split('?')[0];
            var message = $"{timestampMs}{method}{ApiPath}{pathWithoutQuery}";

            using var rsa = RSA.Create();
            rsa.ImportFromPem(_privateKeyPem);

            var signature = rsa.SignData(
                Encoding.UTF8.GetBytes(message),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            return Convert.ToBase64String(signature);
        }

        /// <summary>
        /// Add authentication headers to request
        /// </summary>
        private void AddAuthHeaders(HttpRequestMessage request, string method, string path)
        {
            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var signature = CreateSignature(method, path, timestampMs);

            request.Headers.Add("KALSHI-ACCESS-KEY", _apiKey);
            request.Headers.Add("KALSHI-ACCESS-SIGNATURE", signature);
            request.Headers.Add("KALSHI-ACCESS-TIMESTAMP", timestampMs.ToString());
        }

        /// <summary>
        /// Execute a GET request with authentication and retry logic
        /// </summary>
        public async Task<T?> GetAsync<T>(string path) where T : class
        {
            var retryCount = 0;

            while (retryCount < MaxRetries)
            {
                _rateGate.WaitToProceed();

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiPath}{path}");
                    AddAuthHeaders(request, "GET", path);

                    Log.Debug($"CascadeKalshiDataRestClient: GET {path}");

                    var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        // Rate limited - wait and retry with exponential backoff
                        retryCount++;
                        var delay = BaseRetryDelayMs * (int)Math.Pow(2, retryCount);
                        Log.Trace($"CascadeKalshiDataRestClient: Rate limited (429), waiting {delay}ms before retry {retryCount}/{MaxRetries}");
                        await Task.Delay(delay).ConfigureAwait(false);
                        continue;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Log.Debug($"CascadeKalshiDataRestClient: Not found (404) for {path}");
                        return null;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Log.Error($"CascadeKalshiDataRestClient: Request failed with {response.StatusCode}: {errorContent}");

                        if ((int)response.StatusCode >= 500)
                        {
                            // Server error - retry
                            retryCount++;
                            var delay = BaseRetryDelayMs * (int)Math.Pow(2, retryCount);
                            await Task.Delay(delay).ConfigureAwait(false);
                            continue;
                        }

                        return null;
                    }

                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(content);
                }
                catch (HttpRequestException ex)
                {
                    Log.Error($"CascadeKalshiDataRestClient: HTTP error for {path}: {ex.Message}");
                    retryCount++;
                    if (retryCount < MaxRetries)
                    {
                        var delay = BaseRetryDelayMs * (int)Math.Pow(2, retryCount);
                        await Task.Delay(delay).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    Log.Error($"CascadeKalshiDataRestClient: Request timeout for {path}: {ex.Message}");
                    return null;
                }
            }

            Log.Error($"CascadeKalshiDataRestClient: Max retries exceeded for {path}");
            return null;
        }

        #region Market Operations

        /// <summary>
        /// Get a single market by ticker
        /// </summary>
        public async Task<KalshiMarket?> GetMarketAsync(string ticker)
        {
            // Check cache first
            if (_marketCache.TryGetValue(ticker, out var cached))
            {
                return cached;
            }

            var response = await GetAsync<KalshiMarketResponse>($"/markets/{ticker}").ConfigureAwait(false);
            if (response?.Market != null)
            {
                _marketCache[ticker] = response.Market;
            }
            return response?.Market;
        }

        /// <summary>
        /// Get a single market by ticker (synchronous)
        /// </summary>
        public KalshiMarket? GetMarket(string ticker)
        {
            return GetMarketAsync(ticker).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get all markets with optional filtering
        /// </summary>
        /// <param name="status">Filter by status: open, closed, settled</param>
        /// <param name="seriesTicker">Filter by series ticker</param>
        /// <param name="eventTicker">Filter by event ticker</param>
        /// <param name="minCloseTs">Minimum close timestamp (Unix seconds)</param>
        /// <param name="maxCloseTs">Maximum close timestamp (Unix seconds)</param>
        public async Task<List<KalshiMarket>> GetMarketsAsync(
            string? status = null,
            string? seriesTicker = null,
            string? eventTicker = null,
            long? minCloseTs = null,
            long? maxCloseTs = null)
        {
            var allMarkets = new List<KalshiMarket>();
            string? cursor = null;

            do
            {
                var path = "/markets?limit=200";
                if (!string.IsNullOrEmpty(status))
                {
                    path += $"&status={status}";
                }
                if (!string.IsNullOrEmpty(seriesTicker))
                {
                    path += $"&series_ticker={seriesTicker}";
                }
                if (!string.IsNullOrEmpty(eventTicker))
                {
                    path += $"&event_ticker={eventTicker}";
                }
                if (minCloseTs.HasValue)
                {
                    path += $"&min_close_ts={minCloseTs.Value}";
                }
                if (maxCloseTs.HasValue)
                {
                    path += $"&max_close_ts={maxCloseTs.Value}";
                }
                if (!string.IsNullOrEmpty(cursor))
                {
                    path += $"&cursor={cursor}";
                }

                var response = await GetAsync<KalshiMarketsResponse>(path).ConfigureAwait(false);
                if (response == null)
                {
                    break;
                }

                allMarkets.AddRange(response.Markets);

                // Cache markets
                foreach (var market in response.Markets)
                {
                    _marketCache[market.Ticker] = market;
                }

                cursor = response.Cursor;
            }
            while (!string.IsNullOrEmpty(cursor));

            return allMarkets;
        }

        /// <summary>
        /// Get all markets (synchronous)
        /// </summary>
        public List<KalshiMarket> GetMarkets(
            string? status = null,
            string? seriesTicker = null,
            string? eventTicker = null,
            long? minCloseTs = null,
            long? maxCloseTs = null)
        {
            return GetMarketsAsync(status, seriesTicker, eventTicker, minCloseTs, maxCloseTs)
                .GetAwaiter().GetResult();
        }

        #endregion

        #region Candlestick Operations

        /// <summary>
        /// Get candlestick data for a market
        /// </summary>
        public async Task<List<KalshiCandlestick>> GetCandlesticksAsync(
            string seriesTicker,
            string marketTicker,
            long startTs,
            long endTs,
            int periodInterval = 1)
        {
            var path = $"/series/{seriesTicker}/markets/{marketTicker}/candlesticks" +
                       $"?period_interval={periodInterval}&start_ts={startTs}&end_ts={endTs}";

            var response = await GetAsync<KalshiCandlestickResponse>(path).ConfigureAwait(false);
            return response?.Candlesticks ?? new List<KalshiCandlestick>();
        }

        /// <summary>
        /// Get candlestick data for a market by ticker only (looks up series automatically)
        /// </summary>
        public async Task<List<KalshiCandlestick>> GetCandlesticksAsync(
            string marketTicker,
            long startTs,
            long endTs,
            int periodInterval = 1)
        {
            var market = await GetMarketAsync(marketTicker).ConfigureAwait(false);
            if (market == null)
            {
                Log.Error($"CascadeKalshiDataRestClient: Market not found: {marketTicker}");
                return new List<KalshiCandlestick>();
            }

            return await GetCandlesticksAsync(market.SeriesTicker, marketTicker, startTs, endTs, periodInterval)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get candlestick data in chunks to handle large date ranges
        /// </summary>
        public IEnumerable<KalshiCandlestick> GetCandlesticks(
            string marketTicker,
            DateTime startDate,
            DateTime endDate,
            int periodInterval = 1,
            int chunkDays = 3)
        {
            // Look up the market to get series ticker
            var market = GetMarket(marketTicker);
            if (market == null)
            {
                Log.Error($"CascadeKalshiDataRestClient: Market not found: {marketTicker}");
                yield break;
            }

            foreach (var candle in GetCandlesticks(market.SeriesTicker, marketTicker, startDate, endDate, periodInterval, chunkDays))
            {
                yield return candle;
            }
        }

        /// <summary>
        /// Get candlestick data in chunks with explicit series ticker
        /// </summary>
        public IEnumerable<KalshiCandlestick> GetCandlesticks(
            string seriesTicker,
            string marketTicker,
            DateTime startDate,
            DateTime endDate,
            int periodInterval = 1,
            int chunkDays = 3)
        {
            foreach (var (rangeStart, rangeEnd) in KalshiExtensions.GenerateDateRanges(startDate, endDate, chunkDays))
            {
                var startTs = rangeStart.ToUnixSeconds(KalshiExtensions.KalshiTimeZone);
                var endTs = rangeEnd.ToUnixSeconds(KalshiExtensions.KalshiTimeZone);

                var candlesticks = GetCandlesticksAsync(seriesTicker, marketTicker, startTs, endTs, periodInterval)
                    .GetAwaiter().GetResult();

                foreach (var candle in candlesticks)
                {
                    yield return candle;
                }
            }
        }

        #endregion

        #region Events and Series Operations

        /// <summary>
        /// Get all events with optional filtering
        /// </summary>
        public async Task<List<KalshiEvent>> GetEventsAsync(
            string? status = null,
            string? seriesTicker = null)
        {
            var allEvents = new List<KalshiEvent>();
            string? cursor = null;

            do
            {
                var path = "/events?limit=200";
                if (!string.IsNullOrEmpty(status))
                {
                    path += $"&status={status}";
                }
                if (!string.IsNullOrEmpty(seriesTicker))
                {
                    path += $"&series_ticker={seriesTicker}";
                }
                if (!string.IsNullOrEmpty(cursor))
                {
                    path += $"&cursor={cursor}";
                }

                var response = await GetAsync<KalshiEventsResponse>(path).ConfigureAwait(false);
                if (response == null)
                {
                    break;
                }

                allEvents.AddRange(response.Events);
                cursor = response.Cursor;
            }
            while (!string.IsNullOrEmpty(cursor));

            return allEvents;
        }

        /// <summary>
        /// Get all series
        /// </summary>
        public async Task<List<KalshiSeries>> GetSeriesAsync()
        {
            var allSeries = new List<KalshiSeries>();
            string? cursor = null;

            do
            {
                var path = "/series?limit=200";
                if (!string.IsNullOrEmpty(cursor))
                {
                    path += $"&cursor={cursor}";
                }

                var response = await GetAsync<KalshiSeriesResponse>(path).ConfigureAwait(false);
                if (response == null)
                {
                    break;
                }

                allSeries.AddRange(response.Series);
                cursor = response.Cursor;
            }
            while (!string.IsNullOrEmpty(cursor));

            return allSeries;
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
