#define NET45

/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes: removed compiler directives
 *   * Removed inline using statements
 *   * Removed nullable reference types
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class HttpClientExtensions
    {
        public static void AddAuthenticationHeaders(
            this HttpClient httpClient,
            SecurityKey securityKey)
        {
            foreach (var pair in securityKey.GetAuthenticationHeaders())
            {
                httpClient.DefaultRequestHeaders.Add(pair.Key, pair.Value);
            }
        }

        public static Task<TApi> GetSingleObjectAsync<TApi, TJson>(
            this HttpClient httpClient,
            IThrottler throttler,
            UriBuilder uriBuilder,
            CancellationToken cancellationToken)
            where TJson : TApi =>
            callAndDeserializeSingleObjectAsync<TApi, TJson>(
                httpClient, throttler, uriBuilder.Uri, cancellationToken);

        public static async Task<IReadOnlyList<TApi>> GetObjectsListAsync<TApi, TJson>(
            this HttpClient httpClient,
            IThrottler throttler,
            UriBuilder uriBuilder,
            CancellationToken cancellationToken)
            where TJson : TApi =>
            (IReadOnlyList<TApi>) await callAndDeserializeSingleObjectAsync<IReadOnlyList<TJson>, List<TJson>>(
                    httpClient, throttler, uriBuilder.Uri, cancellationToken)
                .ConfigureAwait(false);

        public static async Task<IReadOnlyList<TApi>> DeleteObjectsListAsync<TApi, TJson>(
            this HttpClient httpClient,
            IThrottler throttler,
            UriBuilder uriBuilder,
            CancellationToken cancellationToken)
            where TJson : TApi =>
            (IReadOnlyList<TApi>) await callAndDeserializeSingleObjectAsync<IReadOnlyList<TJson>, List<TJson>>(
                    httpClient, throttler, uriBuilder.Uri, cancellationToken, HttpMethod.Delete)
                .ConfigureAwait(false);

        public static async Task<TApi> DeserializeAsync<TApi, TJson>(
            this HttpResponseMessage response)
            where TJson : TApi
        {
            //#if NETSTANDARD2_1
            //            await using var stream = await response.Content.ReadAsStreamAsync()
            //#else
            //            using var stream = await response.Content.ReadAsStreamAsync()
            //                .ConfigureAwait(false);
            //#endif
            using (var stream = await response.Content.ReadAsStreamAsync()
                .ConfigureAwait(false))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var serializer = new JsonSerializer();
                if (response.IsSuccessStatusCode)
                {
                    return serializer.Deserialize<TJson>(reader);
                }

                // ReSharper disable once ConstantNullCoalescingCondition
                var jsonError =
                    serializer.Deserialize<JsonError>(reader) ?? new JsonError();

                if (jsonError.Code == 0 ||
                    String.IsNullOrEmpty(jsonError.Message))
                {
                    throw new RestClientErrorException(response);
                }

                throw new RestClientErrorException(jsonError);
            }
        }

        private static async Task<TApi> callAndDeserializeSingleObjectAsync<TApi, TJson>(
            HttpClient httpClient,
            IThrottler throttler,
            Uri endpointUri,
            CancellationToken cancellationToken,
            HttpMethod method = null)
            where TJson : TApi
        {
            var exceptions = new Queue<Exception>();

            for(var attempts = 0; attempts < throttler.MaxRetryAttempts; ++attempts)
            {
                await throttler.WaitToProceed(cancellationToken).ConfigureAwait(false);
                try
                {
                    using (var request = new HttpRequestMessage(method ?? HttpMethod.Get, endpointUri))
                    using (var response = await httpClient
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        // Check response for server and caller specified waits and retries
                        if (!throttler.CheckHttpResponse(response))
                        {
                            continue;
                        }

                        return await response.DeserializeAsync<TApi, TJson>()
                            .ConfigureAwait(false);
                    }
                }
                catch (HttpRequestException ex)
                {
                    exceptions.Enqueue(ex);
                    break;
                }
            }

            throw new AggregateException(exceptions);
        }

        [Conditional("NET45")]
        public static void SetSecurityProtocol(
            this HttpClient httpClient)
        {
#if NET45
            System.Net.ServicePointManager.SecurityProtocol =
#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
                System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11;
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols
#endif
        }
    }
}
