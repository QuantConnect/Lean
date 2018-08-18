using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates bar infromation from Polygon REST API.
    /// </summary>
    public interface IBar
    {
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
        /// Gets bar trading volume.
        /// </summary>
        Int64 Volume { get; }

        /// <summary>
        /// Gets bar timestamp.
        /// </summary>
        DateTime Time { get; }
    }
}