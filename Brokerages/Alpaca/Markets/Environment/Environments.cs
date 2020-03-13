/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides single entry point for obtaining information about different environments.
    /// </summary>
    public static class Environments
    {
        /// <summary>
        /// Gets environment used by all Alpaca users who has fully registered accounts.
        /// </summary>
        public static IEnvironment Live { get; } = new LiveEnvironment();

        /// <summary>
        /// Gets environment used by all Alpaca users who have no registered accounts.
        /// </summary>
        public static IEnvironment Paper { get; } = new PaperEnvironment();

        internal static Uri GetUrlSafe(this String url, Uri defaultUrl) => new Uri(url ?? defaultUrl.AbsoluteUri);
    }
}
