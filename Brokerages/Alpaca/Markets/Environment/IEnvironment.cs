/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides URLs for different APIs available for this SDK on specific environment.
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        /// Gets Alpaca trading REST API base URL for this environment.
        /// </summary>
        Uri AlpacaTradingApi { get; }

        /// <summary>
        /// Gets Alpaca data REST API base URL for this environment.
        /// </summary>
        Uri AlpacaDataApi { get; }

        /// <summary>
        /// Gets Polygon.io data REST API base URL for this environment.
        /// </summary>
        Uri PolygonDataApi { get; }

        /// <summary>
        /// Gets Alpaca streaming API base URL for this environment.
        /// </summary>
        Uri AlpacaStreamingApi { get; }

        /// <summary>
        /// Gets Polygon.io streaming API base URL for this environment.
        /// </summary>
        Uri PolygonStreamingApi { get; }
    }
}
