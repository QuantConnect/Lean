/*
 * The official C# API client for alpaca brokerage
 * https://github.com/alpacahq/alpaca-trade-api-csharp
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal interface IThrottler
    {
        /// <summary>
        /// Gets flag indicating we are currently being rate limited.
        /// </summary>
        Int32 MaxAttempts { get; }

        /// <summary>
        /// Blocks the current thread indefinitely until allowed to proceed.
        /// </summary>
        void WaitToProceed();
    }
}