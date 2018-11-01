/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates asset information from Alpaca REST API.
    /// </summary>
    public interface IAsset
    {
        /// <summary>
        /// Gets unique asset identifier.
        /// </summary>
        Guid AssetId { get; }

        /// <summary>
        /// Gets asset class.
        /// </summary>
        AssetClass Class { get; }

        /// <summary>
        /// Gets asset source exchange.
        /// </summary>
        Exchange Exchange { get; }

        /// <summary>
        /// Gest asset name.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Get asset status in API.
        /// </summary>
        AssetStatus Status { get; }

        /// <summary>
        /// Returns <c>true</c> if asset is tradable.
        /// </summary>
        Boolean IsTradable { get; }
    }
}