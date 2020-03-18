/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides way for creating instance of <see cref="IWebSocket"/> interface implementation.
    /// </summary>
    public interface IWebSocketFactory
    {
        /// <summary>
        /// Creates new instance of <see cref="IWebSocket"/> interface implementation.
        /// </summary>
        /// <param name="url">Base URL for underlying web socket connection.</param>
        /// <returns>Instance of class which implements <see cref="IWebSocket"/> interface.</returns>
        IWebSocket CreateWebSocket(
            Uri url);
    }
}
