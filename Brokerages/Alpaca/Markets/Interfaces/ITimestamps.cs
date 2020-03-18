/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates timestamps information from Polygon REST API.
    /// </summary>
    public interface ITimestamps
    {
        /// <summary>
        /// Gets SIP timestamp.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets participant/exchange timestamp.
        /// </summary>
        DateTime ParticipantTimestamp { get; }

        /// <summary>
        /// Gets trade reporting facility timestamp.
        /// </summary>
        DateTime TradeReportingFacilityTimestamp { get; }
    }
}