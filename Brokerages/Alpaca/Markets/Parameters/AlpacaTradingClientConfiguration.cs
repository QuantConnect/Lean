/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Configuration parameters object for <see cref="AlpacaTradingClient"/> class.
    /// </summary>
    public sealed class AlpacaTradingClientConfiguration
    {
        private static readonly HashSet<ApiVersion> _supportedApiVersions = new HashSet<ApiVersion> { ApiVersion.V1, ApiVersion.V2 };

        internal const ApiVersion DefaultApiVersion = ApiVersion.V2;

        /// <summary>
        /// Creates new instance of <see cref="AlpacaTradingClientConfiguration"/> class.
        /// </summary>
        public AlpacaTradingClientConfiguration()
        {
            ApiVersion = DefaultApiVersion;
            SecurityId = new SecretKey(String.Empty, String.Empty);
            ApiEndpoint = Environments.Live.AlpacaTradingApi;
            ThrottleParameters = ThrottleParameters.Default;
        }

        /// <summary>
        /// Security identifier for API authentication.
        /// </summary>
        public SecurityKey SecurityId { get; set; }

        /// <summary>
        /// Gets or sets Alpaca Trading API base URL.
        /// </summary>
        public Uri ApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets Alpaca Trading API version.
        /// </summary>
        public ApiVersion ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets REST API throttling parameters.
        /// </summary>
        public ThrottleParameters ThrottleParameters { get; set; }

        internal void EnsureIsValid()
        {
            if (SecurityId == null)
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(SecurityId)}' property shouldn't be null.");
            }

            if (ApiEndpoint == null)
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(ApiEndpoint)}' property shouldn't be null.");
            }

            if (!_supportedApiVersions.Contains(ApiVersion))
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(ApiVersion)}' property is invalid.");
            }

            if (ThrottleParameters == null)
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(ThrottleParameters)}' property shouldn't be null.");
            }
        }
    }
}
