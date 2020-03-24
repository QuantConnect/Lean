/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class UriExtensions
    {
        public static Uri AddApiVersionNumberSafe(
            this Uri baseUri,
            ApiVersion apiVersion)
        {
            var builder = new UriBuilder(baseUri);

            if (builder.Path.Equals("/", StringComparison.Ordinal))
            {
                builder.Path = $"{apiVersion.ToEnumString()}/";
            }
            if (!builder.Path.EndsWith("/", StringComparison.Ordinal))
            {
                builder.Path += "/";
            }

            return builder.Uri;
        }
    }
}
