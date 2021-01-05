/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
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
