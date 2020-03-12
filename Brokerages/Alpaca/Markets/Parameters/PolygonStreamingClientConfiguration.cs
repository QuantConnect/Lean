/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Configuration parameters object for <see cref="PolygonStreamingClient"/> class.
    /// </summary>
    public sealed class PolygonStreamingClientConfiguration : StreamingClientConfiguration
    {
        /// <summary>
        /// Creates new instance of <see cref="PolygonStreamingClientConfiguration"/> class.
        /// </summary>
        public PolygonStreamingClientConfiguration()
            : base(Environments.Live.PolygonStreamingApi)
        {
            KeyId = String.Empty;
        }

        /// <summary>
        /// Gets or sets Alpaca application key identifier.
        /// </summary>
        public String KeyId { get; set; }

        internal override void EnsureIsValid()
        {
            base.EnsureIsValid();

            if (String.IsNullOrEmpty(KeyId))
            {
                throw new InvalidOperationException(
                    $"The value of '{nameof(KeyId)}' property shouldn't be null or empty.");
            }
        }
    }
}
