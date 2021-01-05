/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates bar information from Polygon streaming API.
    /// </summary>
    public interface IStreamAgg : IAggBase
    {
        /// <summary>
        /// Gets asset name.
        /// </summary>
        String Symbol { get; } 
        
        /// <summary>
        /// Gets bar average price.
        /// </summary>
        Decimal Average { get; }

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
