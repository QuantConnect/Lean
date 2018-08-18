using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates bar infromation from Polygon streaming API.
    /// </summary>
    public interface IStreamBar
    {
        /// <summary>
        /// Gets asset name.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Gets asset's exchange identifier.
        /// </summary>
        Int64 Exchange { get; }

        /// <summary>
        /// Gets bar open price.
        /// </summary>
        Decimal Open { get; }

        /// <summary>
        /// Gets bar high price.
        /// </summary>
        Decimal High { get; }

        /// <summary>
        /// Gets bar low price.
        /// </summary>
        Decimal Low { get; }

        /// <summary>
        /// Gets bar close price.
        /// </summary>
        Decimal Close { get; }

        /// <summary>
        /// Gets bar average price.
        /// </summary>
        Decimal Average { get; }

        /// <summary>
        /// Gets bar trading volume.
        /// </summary>
        Int64 Volume { get; }

        /// <summary>
        /// Gets bar opening timestamp.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Gets bar closing timestamp.
        /// </summary>
        DateTime EndTime { get; }
    }
}