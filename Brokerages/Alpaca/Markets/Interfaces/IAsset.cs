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