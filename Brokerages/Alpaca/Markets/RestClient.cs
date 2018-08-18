using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly String _polygonApiKey;

        /// <summary>
        /// Creates new instance of <see cref="RestClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        /// <param name="polygonRestApi">Polygon REST API ennpoint URL.</param>
        public RestClient(
            String keyId,
            String secretKey,
            String alpacaRestApi = null,
            String polygonRestApi = null)
            : this(
                keyId,
                secretKey,
                new Uri(alpacaRestApi ?? "https://api.alpaca.markets"),
                new Uri(polygonRestApi ?? "https://api.polygon.io"))
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="RestClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        /// <param name="polygonRestApi">Polygon REST API ennpoint URL.</param>
        public RestClient(
            String keyId,
            String secretKey,
            Uri alpacaRestApi,
            Uri polygonRestApi)
        {
			if (keyId == null)
				throw new ArgumentException(nameof(keyId));
            //keyId = keyId ?? throw new ArgumentException(nameof(keyId));
			if (secretKey == null) throw new ArgumentException(nameof(secretKey));
			//secretKey = secretKey ?? throw new ArgumentException(nameof(secretKey));

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
        }

        private async Task<TApi> getSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            String endpointUri)
            where TJson : TApi
        {
            using (var stream = await httpClient.GetStreamAsync(endpointUri))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<TJson>(reader);
            }
        }

        private Task<TApi> getSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            UriBuilder uriBuilder)
            where TJson : TApi
        {
            return getSingleObjectAsync<TApi, TJson>(httpClient, uriBuilder.ToString());
        }

        private async Task<IEnumerable<TApi>> getObjectsListAsync<TApi, TJson>(
            HttpClient httpClient,
            String endpointUri)
            where TJson : TApi
        {
            return (IEnumerable<TApi>) await
                getSingleObjectAsync<IEnumerable<TJson>, List<TJson>>(httpClient, endpointUri);
        }

        private async Task<IEnumerable<TApi>> getObjectsListAsync<TApi, TJson>(
            HttpClient httpClient,
            UriBuilder uriBuilder)
            where TJson : TApi
        {
            return (IEnumerable<TApi>) await
                getSingleObjectAsync<IEnumerable<TJson>, List<TJson>>(httpClient, uriBuilder);
        }
    }
}
