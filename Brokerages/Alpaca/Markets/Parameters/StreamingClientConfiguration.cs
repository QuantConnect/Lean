/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Configuration parameters object for <see cref="SockClient"/> class.
    /// </summary>
    public abstract class StreamingClientConfiguration
    {
        /// <summary>
        /// Creates new instance of <see cref="StreamingClientConfiguration"/> class.
        /// </summary>
        protected internal StreamingClientConfiguration(Uri apiEndpoint)
        {
            ApiEndpoint = apiEndpoint;
            WebSocketFactory = new WebSocketSharpFactory();
        }

        /// <summary>
        /// Gets or sets Alpaca streaming API base URL.
        /// </summary>
        public Uri ApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets web sockets connection factory.
        /// </summary>
        public IWebSocketFactory WebSocketFactory { get; set; }

        internal IWebSocket CreateWebSocket() =>
            WebSocketFactory.CreateWebSocket(ApiEndpoint);

        internal virtual void EnsureIsValid()
        {
            if (ApiEndpoint == null)
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(ApiEndpoint)}' property shouldn't be null.");
            }

            if (WebSocketFactory == null)
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(WebSocketFactory)}' property shouldn't be null.");
            }
        }
    }
}
