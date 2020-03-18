/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Alpaca Trading API via HTTP/REST.
    /// </summary>
    public sealed partial class AlpacaTradingClient : IDisposable
    {
        // TODO: olegra - use built-in HttpMethod.Patch property in .NET Standard 2.1
        private static readonly HttpMethod _httpMethodPatch = new HttpMethod("PATCH");

        private readonly HttpClient _httpClient = new HttpClient();

        private readonly IThrottler _alpacaRestApiThrottler;

        /// <summary>
        /// Creates new instance of <see cref="AlpacaTradingClient"/> object.
        /// </summary>
        /// <param name="configuration">Configuration parameters object.</param>
        public AlpacaTradingClient(
            AlpacaTradingClientConfiguration configuration)
        {
            configuration
                .EnsureNotNull(nameof(configuration))
                .EnsureIsValid();

            _alpacaRestApiThrottler = configuration.ThrottleParameters.GetThrottler();

            _httpClient.AddAuthenticationHeaders(configuration.SecurityId);

            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.BaseAddress = configuration.ApiEndpoint
                .AddApiVersionNumberSafe(configuration.ApiVersion);
            _httpClient.SetSecurityProtocol();
        }

        /// <inheritdoc />
        public void Dispose() => _httpClient.Dispose();

        private static StringContent toStringContent(
            Object value)
        {
            var serializer = new JsonSerializer();
            using (var stringWriter = new StringWriter())
            {

                serializer.Serialize(stringWriter, value);
                return new StringContent(stringWriter.ToString());
            }
        }
    }
}
