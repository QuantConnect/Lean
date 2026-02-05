/*
 * Cascade Labs - Modified ThetaData REST Client
 * Based on QuantConnect.Lean.DataSource.ThetaData
 *
 * Modifications:
 * - Added Bearer token authentication
 * - Removed dependency on local ThetaData Terminal
 * - Added client-side rate limiting to prevent server flooding
 * - Added exponential backoff retry for 429 TooManyRequests
 */

using System.Net;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Web;
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Common;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Modified ThetaData REST client with Bearer token authentication
    /// for use with thetadata.cascadelabs.io
    /// </summary>
    public class CascadeThetaDataRestClient : IDisposable
    {
        private const int MaxRequestRetries = 3;
        private const int MaxRateLimitRetries = 5;
        private const string ApiVersion = "/v2";

        private readonly string _restApiBaseUrl;
        private readonly string? _authToken;
        private readonly HttpClient _httpClient;
        private readonly RateGate? _rateGate;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly int _maxConcurrentRequests;
        private readonly Random _jitterRandom = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with Bearer token authentication
        /// </summary>
        /// <param name="rateGate">Rate gate for controlling request rate</param>
        public CascadeThetaDataRestClient(RateGate rateGate)
        {
            _restApiBaseUrl = Config.Get("thetadata-url", "https://thetadata.cascadelabs.io");
            _authToken = Config.Get("thetadata-api-key", null);

            // Client-side concurrency limit to prevent flooding the server
            // ThetaData STANDARD plan allows 4 concurrent stock + 4 concurrent option requests
            // We limit total concurrent requests to stay safely under server limits
            _maxConcurrentRequests = Config.GetInt("thetadata-max-concurrent-requests", 4);
            _concurrencySemaphore = new SemaphoreSlim(_maxConcurrentRequests, _maxConcurrentRequests);

            var baseAddress = $"{_restApiBaseUrl}{ApiVersion}/";
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = TimeSpan.FromMinutes(5)
            };

            // Add Bearer token authentication if configured
            if (!string.IsNullOrEmpty(_authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authToken);
                Log.Debug($"CascadeThetaDataRestClient: Configured with Bearer authentication");
            }
            else
            {
                Log.Trace($"CascadeThetaDataRestClient: No auth token configured, requests will be unauthenticated");
            }

            _rateGate = rateGate;

            Log.Trace($"CascadeThetaDataRestClient: Initialized with base URL: {baseAddress}, max concurrent: {_maxConcurrentRequests}");
        }

        /// <summary>
        /// Executes a REST request and returns the results
        /// </summary>
        public IEnumerable<T?> ExecuteRequest<T>(string endpoint, Dictionary<string, string> queryParameters)
            where T : IBaseResponse
        {
            var parameters = GetSpecificQueryParameters(queryParameters,
                RequestParameters.IntervalInMilliseconds,
                RequestParameters.StartDate,
                RequestParameters.EndDate);

            if (parameters.Count != 3)
            {
                return ExecuteRequestAsync<T>(endpoint, queryParameters).SynchronouslyAwaitTaskResult();
            }

            var intervalInDay = parameters[RequestParameters.IntervalInMilliseconds] switch
            {
                "0" => 1,
                "1000" or "60000" => 30,
                "3600000" => 90,
                _ => throw new NotImplementedException(
                    $"CascadeThetaDataRestClient: Interval '{parameters[RequestParameters.IntervalInMilliseconds]}' not supported.")
            };

            var startDate = parameters[RequestParameters.StartDate].ConvertFromThetaDataDateFormat();
            var endDate = parameters[RequestParameters.EndDate].ConvertFromThetaDataDateFormat();

            if ((endDate - startDate).TotalDays <= intervalInDay)
            {
                return ExecuteRequestAsync<T>(endpoint, queryParameters).SynchronouslyAwaitTaskResult();
            }

            return ExecuteRequestParallelAsync<T>(endpoint, queryParameters, startDate, endDate, intervalInDay)
                .SynchronouslyAwaitTaskResult();
        }

        private async IAsyncEnumerable<T?> ExecuteRequestWithPaginationAsync<T>(
            string endpoint,
            Dictionary<string, string> queryParameters)
            where T : IBaseResponse
        {
            var retryCount = 0;
            var rateLimitRetryCount = 0;
            var currentEndpoint = endpoint;
            var currentQueryParams = HttpUtility.ParseQueryString(string.Empty);

            foreach (var kvp in queryParameters)
            {
                currentQueryParams[kvp.Key] = kvp.Value;
            }

            while (currentEndpoint != null)
            {
                var requestUri = BuildRequestUri(currentEndpoint, currentQueryParams);
                Log.Debug($"CascadeThetaDataRestClient: Requesting {requestUri}");

                _rateGate?.WaitToProceed();

                // Acquire semaphore to limit concurrent requests
                await _concurrencySemaphore.WaitAsync().ConfigureAwait(false);

                T? result = default;
                HttpResponseMessage? response = null;
                int? retryDelayMs = null;

                try
                {
                    response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    _concurrencySemaphore.Release();

                    if (retryCount < MaxRequestRetries)
                    {
                        retryCount++;
                        await Task.Delay(1000 * retryCount).ConfigureAwait(false);
                        continue;
                    }

                    throw new Exception(
                        $"CascadeThetaDataRestClient: HTTP request failed for {currentEndpoint}: {ex.Message}", ex);
                }
                catch (TaskCanceledException ex)
                {
                    _concurrencySemaphore.Release();
                    throw new Exception(
                        $"CascadeThetaDataRestClient: Request timeout for {currentEndpoint}: {ex.Message}", ex);
                }

                // Release semaphore immediately after HTTP request completes
                // This allows other requests to proceed while we process the response
                _concurrencySemaphore.Release();

                // ThetaData returns 472 when no data found
                if ((int)response.StatusCode == 472)
                {
                    Log.Debug($"CascadeThetaDataRestClient: No data found (472) for {requestUri}");
                    yield break;
                }

                // ThetaData returns 475 for server-side errors (e.g., ArrayIndexOutOfBoundsException)
                // Treat as no data available and continue gracefully
                if ((int)response.StatusCode == 475)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log.Debug($"CascadeThetaDataRestClient: Server error (475) for {requestUri}: {errorContent}");
                    yield break;
                }

                // ThetaData returns 572 for "Uncaught error processing request"
                // This is a transient server-side bug - treat as no data and continue gracefully
                if ((int)response.StatusCode == 572)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log.Debug($"CascadeThetaDataRestClient: Server error (572) for {requestUri}: {errorContent}");
                    yield break;
                }

                // Handle 429 TooManyRequests with exponential backoff
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (rateLimitRetryCount < MaxRateLimitRetries)
                    {
                        rateLimitRetryCount++;
                        // Exponential backoff: 2s, 4s, 8s, 16s, 32s with jitter
                        var baseDelay = (int)Math.Pow(2, rateLimitRetryCount) * 1000;
                        var jitter = _jitterRandom.Next(0, 500);
                        retryDelayMs = baseDelay + jitter;

                        Log.Trace($"CascadeThetaDataRestClient: Rate limited (429), retry {rateLimitRetryCount}/{MaxRateLimitRetries} in {retryDelayMs}ms: {errorContent}");
                        await Task.Delay(retryDelayMs.Value).ConfigureAwait(false);
                        continue;
                    }

                    Log.Error($"CascadeThetaDataRestClient: Rate limit exceeded after {MaxRateLimitRetries} retries: {errorContent}");
                    throw new Exception(
                        $"CascadeThetaDataRestClient: Rate limit exceeded for {currentEndpoint}. Server queue is full.");
                }

                // Handle transient server errors (500, 502, 503, 504) with exponential backoff
                var isTransientServerError = response.StatusCode == HttpStatusCode.InternalServerError ||
                                             response.StatusCode == HttpStatusCode.BadGateway ||
                                             response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                                             response.StatusCode == HttpStatusCode.GatewayTimeout;

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (isTransientServerError && retryCount < MaxRequestRetries)
                    {
                        retryCount++;
                        // Exponential backoff for transient errors: 2s, 4s with jitter
                        var baseDelay = (int)Math.Pow(2, retryCount) * 1000;
                        var jitter = _jitterRandom.Next(0, 500);
                        var delayMs = baseDelay + jitter;
                        Log.Trace($"CascadeThetaDataRestClient: Transient error ({response.StatusCode}), retry {retryCount}/{MaxRequestRetries} in {delayMs}ms: {errorContent}");
                        await Task.Delay(delayMs).ConfigureAwait(false);
                        continue;
                    }

                    Log.Error($"CascadeThetaDataRestClient: Request failed with {response.StatusCode}: {errorContent}");

                    if (retryCount < MaxRequestRetries)
                    {
                        retryCount++;
                        await Task.Delay(1000 * retryCount).ConfigureAwait(false);
                        continue;
                    }

                    throw new Exception(
                        $"CascadeThetaDataRestClient: Request failed with status {response.StatusCode} for {currentEndpoint}");
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result = JsonConvert.DeserializeObject<T>(content);

                if (result?.Header.NextPage != null)
                {
                    var nextPageUri = new Uri(result.Header.NextPage);
                    currentEndpoint = nextPageUri.AbsolutePath.Replace(ApiVersion, string.Empty);
                    currentQueryParams = HttpUtility.ParseQueryString(nextPageUri.Query);
                }
                else
                {
                    currentEndpoint = null;
                }

                // Reset retry counts on success
                retryCount = 0;
                rateLimitRetryCount = 0;

                if (result != null)
                {
                    yield return result;
                }
            }
        }

        private async Task<IEnumerable<T?>> ExecuteRequestAsync<T>(
            string endpoint,
            Dictionary<string, string> queryParameters)
            where T : IBaseResponse
        {
            var responses = new List<T?>();
            await foreach (var response in ExecuteRequestWithPaginationAsync<T>(endpoint, queryParameters))
            {
                responses.Add(response);
            }
            return responses;
        }

        private async Task<IEnumerable<T?>> ExecuteRequestParallelAsync<T>(
            string endpoint,
            Dictionary<string, string> queryParameters,
            DateTime startDate,
            DateTime endDate,
            int intervalInDay)
            where T : IBaseResponse
        {
            var resultDict = new ConcurrentDictionary<int, List<T?>>();

            var dateRanges = ThetaDataExtensions.GenerateDateRangesWithInterval(startDate, endDate, intervalInDay)
                .Select((range, index) => (range, index))
                .ToList();

            // Limit parallelism to avoid flooding the server
            // The semaphore provides additional protection, but limiting parallelism here
            // prevents building up a large backlog of waiting requests
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxConcurrentRequests
            };

            await Parallel.ForEachAsync(dateRanges, parallelOptions, async (item, _) =>
            {
                var (dateRange, index) = item;
                var modifiedParams = new Dictionary<string, string>(queryParameters);
                modifiedParams[RequestParameters.StartDate] = dateRange.startDate.ConvertToThetaDataDateFormat();
                modifiedParams[RequestParameters.EndDate] = dateRange.endDate.ConvertToThetaDataDateFormat();

                var results = new List<T?>();
                await foreach (var response in ExecuteRequestWithPaginationAsync<T>(endpoint, modifiedParams))
                {
                    results.Add(response);
                }
                resultDict[index] = results;
            }).ConfigureAwait(false);

            return resultDict.OrderBy(kvp => kvp.Key).SelectMany(kvp => kvp.Value);
        }

        private Uri BuildRequestUri(string endpoint, NameValueCollection queryParameters)
        {
            if (endpoint.StartsWith('/'))
            {
                endpoint = endpoint.TrimStart('/');
            }

            if (queryParameters != null && queryParameters.Count > 0)
            {
                return new Uri($"{endpoint}?{queryParameters}", UriKind.Relative);
            }

            return new Uri(endpoint, UriKind.Relative);
        }

        private Dictionary<string, string> GetSpecificQueryParameters(
            Dictionary<string, string> queryParameters,
            params string[] findingParamNames)
        {
            var parameters = new Dictionary<string, string>(findingParamNames.Length);

            foreach (var paramName in findingParamNames)
            {
                if (queryParameters.TryGetValue(paramName, out var value))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new ArgumentException(
                            $"The value for parameter '{paramName}' is null or empty.",
                            nameof(queryParameters));
                    }
                    parameters[paramName] = value;
                }
            }

            return parameters;
        }

        /// <summary>
        /// Fetches stock split data for a given ticker within a date range
        /// </summary>
        /// <param name="ticker">The stock ticker symbol</param>
        /// <param name="startDate">Start date for the query</param>
        /// <param name="endDate">End date for the query</param>
        /// <returns>Enumerable of split responses</returns>
        public IEnumerable<SplitResponse> GetSplits(string ticker, DateTime startDate, DateTime endDate)
        {
            var queryParameters = new Dictionary<string, string>
            {
                ["root"] = ticker,
                [RequestParameters.StartDate] = startDate.ConvertToThetaDataDateFormat(),
                [RequestParameters.EndDate] = endDate.ConvertToThetaDataDateFormat()
            };

            foreach (var response in ExecuteRequest<BaseResponse<SplitResponse>>("hist/stock/split", queryParameters))
            {
                if (response.Response == null) continue;

                foreach (var split in response.Response)
                {
                    yield return split;
                }
            }
        }

        /// <summary>
        /// Fetches dividend data for a given ticker within a date range
        /// </summary>
        /// <param name="ticker">The stock ticker symbol</param>
        /// <param name="startDate">Start date for the query</param>
        /// <param name="endDate">End date for the query</param>
        /// <returns>Enumerable of dividend responses</returns>
        public IEnumerable<DividendResponse> GetDividends(string ticker, DateTime startDate, DateTime endDate)
        {
            var queryParameters = new Dictionary<string, string>
            {
                ["root"] = ticker,
                [RequestParameters.StartDate] = startDate.ConvertToThetaDataDateFormat(),
                [RequestParameters.EndDate] = endDate.ConvertToThetaDataDateFormat()
            };

            foreach (var response in ExecuteRequest<BaseResponse<DividendResponse>>("hist/stock/dividend", queryParameters))
            {
                if (response.Response == null) continue;

                foreach (var dividend in response.Response)
                {
                    yield return dividend;
                }
            }
        }

        /// <summary>
        /// Fetches end-of-day stock data for a given ticker within a date range
        /// </summary>
        /// <param name="ticker">The stock ticker symbol</param>
        /// <param name="startDate">Start date for the query</param>
        /// <param name="endDate">End date for the query</param>
        /// <returns>Enumerable of end-of-day responses</returns>
        public IEnumerable<EndOfDayReportResponse> GetEndOfDayData(string ticker, DateTime startDate, DateTime endDate)
        {
            var queryParameters = new Dictionary<string, string>
            {
                ["root"] = ticker,
                [RequestParameters.StartDate] = startDate.ConvertToThetaDataDateFormat(),
                [RequestParameters.EndDate] = endDate.ConvertToThetaDataDateFormat()
            };

            foreach (var response in ExecuteRequest<BaseResponse<EndOfDayReportResponse>>("hist/stock/eod", queryParameters))
            {
                if (response.Response == null) continue;

                foreach (var eod in response.Response)
                {
                    yield return eod;
                }
            }
        }

        /// <summary>
        /// Fetches option contracts that had trades on a given date
        /// </summary>
        /// <param name="ticker">The underlying root symbol</param>
        /// <param name="date">The date to query for contracts</param>
        /// <returns>Enumerable of option contract responses</returns>
        public IEnumerable<OptionContractResponse> GetOptionContracts(string ticker, DateTime date)
        {
            var queryParameters = new Dictionary<string, string>
            {
                ["root"] = ticker,
                [RequestParameters.StartDate] = date.ConvertToThetaDataDateFormat()
            };

            foreach (var response in ExecuteRequest<BaseResponse<OptionContractResponse>>("list/contracts/option/trade", queryParameters))
            {
                if (response.Response == null) continue;

                foreach (var contract in response.Response)
                {
                    yield return contract;
                }
            }
        }

        /// <summary>
        /// Fetches all stock root symbols available in ThetaData
        /// </summary>
        /// <returns>Enumerable of stock ticker symbols</returns>
        public IEnumerable<string> GetStockRoots()
        {
            foreach (var response in ExecuteRequest<StockRootsResponse>("list/roots/stock", new Dictionary<string, string>()))
            {
                if (response.Response == null) continue;

                foreach (var ticker in response.Response)
                {
                    yield return ticker;
                }
            }
        }

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
                    _concurrencySemaphore?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
