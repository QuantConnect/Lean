/*
 * Cascade Labs - Hyperliquid REST Client
 *
 * REST client for Hyperliquid DEX API
 * Supports fetching historical market data (candles, trades)
 * and metadata for perpetual futures
 */

using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// REST client for Hyperliquid DEX API
    /// </summary>
    /// <remarks>
    /// All requests use POST to /info endpoint with different request types.
    /// Rate limit: 1200 requests/minute (20 req/sec)
    /// </remarks>
    public class HyperliquidRestClient : IDisposable
    {
        private const int MaxRequestRetries = 3;
        private const int RateLimitRetries = 5;
        private const string InfoEndpoint = "/info";

        /// <summary>
        /// Rate limit for Hyperliquid API: 1200 requests per minute
        /// </summary>
        private const int RequestsPerMinute = 1200;

        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly RateGate _rateGate;
        private readonly Random _jitterRandom = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the Hyperliquid REST client
        /// </summary>
        /// <param name="testnet">If true, uses testnet API endpoint</param>
        public HyperliquidRestClient(bool testnet = false)
        {
            _baseUrl = testnet
                ? Config.Get("hyperliquid-testnet-url", "https://api.hyperliquid-testnet.xyz")
                : Config.Get("hyperliquid-api-url", "https://api.hyperliquid.xyz");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromMinutes(2)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Rate gate: allow 20 requests per second (1200/min with some buffer)
            _rateGate = new RateGate(20, TimeSpan.FromSeconds(1));

            Log.Trace($"HyperliquidRestClient: Initialized with base URL: {_baseUrl}");
        }

        /// <summary>
        /// Gets metadata for all perpetual futures assets
        /// </summary>
        /// <returns>JSON object containing universe of tradeable assets</returns>
        public async Task<JObject?> GetMetaAsync()
        {
            var payload = new { type = "meta" };
            return await PostRequestAsync<JObject>(InfoEndpoint, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets extended metadata including asset contexts (mark price, funding, etc.)
        /// </summary>
        /// <returns>Array with [meta, assetCtxs]</returns>
        public async Task<JArray?> GetMetaAndAssetCtxsAsync()
        {
            var payload = new { type = "metaAndAssetCtxs" };
            return await PostRequestAsync<JArray>(InfoEndpoint, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets candle/OHLCV data for a specific coin
        /// </summary>
        /// <param name="coin">Coin symbol (e.g., "BTC", "ETH")</param>
        /// <param name="interval">Candle interval (e.g., "1m", "5m", "1h", "1d")</param>
        /// <param name="startTimeMs">Start time in milliseconds since epoch</param>
        /// <param name="endTimeMs">End time in milliseconds since epoch</param>
        /// <returns>Array of candle objects</returns>
        public async Task<JArray?> GetCandleSnapshotAsync(
            string coin,
            string interval,
            long startTimeMs,
            long endTimeMs)
        {
            var req = new
            {
                coin,
                interval,
                startTime = startTimeMs,
                endTime = endTimeMs
            };

            var payload = new { type = "candleSnapshot", req };
            return await PostRequestAsync<JArray>(InfoEndpoint, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets recent trades for a specific coin
        /// </summary>
        /// <param name="coin">Coin symbol (e.g., "BTC", "ETH")</param>
        /// <returns>Array of recent trade objects</returns>
        public async Task<JArray?> GetRecentTradesAsync(string coin)
        {
            var payload = new { type = "recentTrades", coin };
            return await PostRequestAsync<JArray>(InfoEndpoint, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets L2 order book snapshot for a specific coin
        /// </summary>
        /// <param name="coin">Coin symbol (e.g., "BTC", "ETH")</param>
        /// <returns>Order book with bids and asks</returns>
        public async Task<JObject?> GetL2BookAsync(string coin)
        {
            var payload = new { type = "l2Book", coin };
            return await PostRequestAsync<JObject>(InfoEndpoint, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all mid prices for actively traded coins
        /// </summary>
        /// <returns>Dictionary mapping coin symbols to mid prices</returns>
        public async Task<JObject?> GetAllMidsAsync()
        {
            var payload = new { type = "allMids" };
            return await PostRequestAsync<JObject>(InfoEndpoint, payload).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a POST request with retry logic and rate limiting
        /// </summary>
        private async Task<T?> PostRequestAsync<T>(string endpoint, object payload) where T : class
        {
            var retryCount = 0;
            var rateLimitRetryCount = 0;

            while (true)
            {
                try
                {
                    // Rate limiting
                    _rateGate.WaitToProceed();

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    Log.Debug($"HyperliquidRestClient: POST {endpoint} - {jsonPayload}");

                    var response = await _httpClient.PostAsync(endpoint, content).ConfigureAwait(false);

                    // Handle rate limiting (429 Too Many Requests)
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        if (rateLimitRetryCount >= RateLimitRetries)
                        {
                            Log.Error($"HyperliquidRestClient: Max rate limit retries ({RateLimitRetries}) exceeded");
                            return null;
                        }

                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, rateLimitRetryCount));
                        var jitter = TimeSpan.FromMilliseconds(_jitterRandom.Next(0, 1000));
                        var delay = retryAfter + jitter;

                        Log.Trace($"HyperliquidRestClient: Rate limited (429). Retry {rateLimitRetryCount + 1}/{RateLimitRetries} after {delay.TotalSeconds:F1}s");

                        await Task.Delay(delay).ConfigureAwait(false);
                        rateLimitRetryCount++;
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        Log.Trace($"HyperliquidRestClient: Empty response from {endpoint}");
                        return null;
                    }

                    var result = JsonConvert.DeserializeObject<T>(responseContent);
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    if (retryCount >= MaxRequestRetries)
                    {
                        Log.Error($"HyperliquidRestClient: Max retries ({MaxRequestRetries}) exceeded: {ex.Message}");
                        return null;
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)) +
                                TimeSpan.FromMilliseconds(_jitterRandom.Next(0, 1000));

                    Log.Trace($"HyperliquidRestClient: Request failed. Retry {retryCount + 1}/{MaxRequestRetries} after {delay.TotalSeconds:F1}s: {ex.Message}");

                    await Task.Delay(delay).ConfigureAwait(false);
                    retryCount++;
                }
                catch (JsonException ex)
                {
                    Log.Error($"HyperliquidRestClient: JSON parsing error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.Error($"HyperliquidRestClient: Unexpected error: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Disposes resources used by the client
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _httpClient?.Dispose();
            _rateGate?.Dispose();
            _disposed = true;
        }
    }
}
