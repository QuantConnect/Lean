/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Configuration parameters object for <see cref="PolygonDataClient"/> class.
    /// </summary>
    public sealed class PolygonDataClientConfiguration
    {
        /// <summary>
        /// Creates new instance of <see cref="PolygonDataClientConfiguration"/> class.
        /// </summary>
        public PolygonDataClientConfiguration()
        {
            KeyId = String.Empty;
            ApiEndpoint = Environments.Live.PolygonDataApi;
        }

        /// <summary>
        /// Gets or sets Alpaca application key identifier.
        /// </summary>
        public String KeyId { get; set; }

        /// <summary>
        /// Gets or sets Polygon Data API base URL.
        /// </summary>
        public Uri ApiEndpoint { get; set; }

        internal void EnsureIsValid()
        {
            if (String.IsNullOrEmpty(KeyId))
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(KeyId)}' property shouldn't be null or empty.");
            }

            if (ApiEndpoint == null)
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(ApiEndpoint)}' property shouldn't be null.");
            }
        }
    }
}
