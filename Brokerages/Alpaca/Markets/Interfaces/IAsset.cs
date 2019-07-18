/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
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

        /// <summary>
        /// Asset is marginable or not
        /// </summary>
        Boolean Marginable { get; }

        /// <summary>
        /// Asset is shortable or not
        /// </summary>
        Boolean Shortable { get; }

        /// <summary>
        /// Asset is easy-to-borrow or not
        /// </summary>
        Boolean EasyToBorrow { get; }
    }
}
