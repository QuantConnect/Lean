/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal interface IThrottler
    {
        /// <summary>
        /// Gets maximal number of retry attempts for single request.
        /// </summary>
        Int32 MaxRetryAttempts { get; }

        /// <summary>
        /// Blocks the current thread indefinitely until allowed to proceed.
        /// </summary>
        Task WaitToProceed();

        /// <summary>
        /// Evaluates the StatusCode of <paramref name="response"/>, initiates any server requested delays, 
        /// and returns false to indicate when a client should retry a request
        /// </summary>
        /// <param name="response">Server response to an Http request</param>
        /// <returns>False indicates that client should retry the request.
        /// True indicates that StatusCode was HttpStatusCode.OK.
        /// </returns>
        /// <exception cref="HttpRequestException">
        /// The HTTP response is unsuccessful, and caller did not indicate that requests with this StatusCode should be retried.
        /// </exception>
        Boolean CheckHttpResponse(HttpResponseMessage response);
    }
}
