/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class CustomTimeZone
    {
        public static TimeZoneInfo Est { get; } =
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    }
}
