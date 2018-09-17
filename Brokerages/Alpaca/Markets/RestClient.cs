/*
 * The official C# API client for alpaca brokerage
 * https://github.com/alpacahq/alpaca-trade-api-csharp
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Alpaca REST API and Polygon REST API endpoints.
    /// </summary>
    public sealed partial class RestClient
    {
        private readonly HttpClient _alpacaHttpClient = new HttpClient();

        private readonly HttpClient _polygonHttpClient = new HttpClient();

        private readonly Boolean _isPolygonStaging;

        private readonly String _polygonApiKey;

        private static readonly IThrottler _alpacaRestApiThrottler =
            new RateThrottler(200, 5, TimeSpan.FromMinutes(1));

        /// <summary>
        /// Creates new instance of <see cref="RestClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        /// <param name="polygonRestApi">Polygon REST API ennpoint URL.</param>
        /// <param name="isStagingEnvironment">If <c>true</c> use staging.</param>
        public RestClient(
            String keyId,
            String secretKey,
            String alpacaRestApi = null,
            String polygonRestApi = null,
            Boolean? isStagingEnvironment = null)
            : this(
                keyId,
                secretKey,
                new Uri(alpacaRestApi ?? "https://api.alpaca.markets"),
                new Uri(polygonRestApi ?? "https://api.polygon.io"),
                isStagingEnvironment ?? false)
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="RestClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        /// <param name="polygonRestApi">Polygon REST API ennpoint URL.</param>
        /// <param name="isStagingEnvironment">If <c>true</c> use staging.</param>
        public RestClient(
            String keyId,
            String secretKey,
            Uri alpacaRestApi,
            Uri polygonRestApi,
            Boolean isStagingEnvironment)
        {
            if (keyId == null) throw new ArgumentException(nameof(keyId));
            if (secretKey == null) throw new ArgumentException(nameof(secretKey));

            _alpacaHttpClient.DefaultRequestHeaders.Add(
                "APCA-API-KEY-ID", keyId);
            _alpacaHttpClient.DefaultRequestHeaders.Add(
                "APCA-API-SECRET-KEY", secretKey);
            _alpacaHttpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _alpacaHttpClient.BaseAddress =
                alpacaRestApi ?? new Uri("https://api.alpaca.markets");

            _polygonApiKey = keyId;
            _polygonHttpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _polygonHttpClient.BaseAddress =
                polygonRestApi ?? new Uri("https://api.polygon.io");
            _isPolygonStaging = isStagingEnvironment ||
                _alpacaHttpClient.BaseAddress.Host.Contains("staging");

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

        }

        private async Task<TApi> getSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            IThrottler throttler,
            String endpointUri)
            where TJson : TApi
        {
            Queue<Exception> exceptions = new Queue<Exception>();

            for (var attempts = 0; attempts < throttler.MaxAttempts; ++attempts)
            {
                throttler.WaitToProceed();

                try
                {
                    using (var stream = await httpClient.GetStreamAsync(endpointUri))
                    using (var reader = new JsonTextReader(new StreamReader(stream)))
                    {
                        var serializer = new JsonSerializer();
                        return serializer.Deserialize<TJson>(reader);
                    }
                }
                catch (HttpRequestException ex)
                {
                    exceptions.Enqueue(ex);
                }
            }

            throw new AggregateException(exceptions);
        }

        private Task<TApi> getSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            IThrottler throttler,
            UriBuilder uriBuilder)
            where TJson : TApi
        {
            return getSingleObjectAsync<TApi, TJson>(httpClient, throttler, uriBuilder.ToString());
        }

        private async Task<IEnumerable<TApi>> getObjectsListAsync<TApi, TJson>(
            HttpClient httpClient,
            IThrottler throttler,
            String endpointUri)
            where TJson : TApi
        {
            return (IEnumerable<TApi>)await
                getSingleObjectAsync<IEnumerable<TJson>, List<TJson>>(httpClient, throttler, endpointUri);
        }

        private async Task<IEnumerable<TApi>> getObjectsListAsync<TApi, TJson>(
            HttpClient httpClient,
            IThrottler throttler,
            UriBuilder uriBuilder)
            where TJson : TApi
        {
            return (IEnumerable<TApi>)await
                getSingleObjectAsync<IEnumerable<TJson>, List<TJson>>(httpClient, throttler, uriBuilder);
        }
    }
}
