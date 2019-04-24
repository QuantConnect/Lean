/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class DateTimeHelper
    {
        private static readonly DateTime _epoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnixTimeMilliseconds(
            Int64 linuxTimeStamp)
        {
            return _epoch.Add(TimeSpan.FromMilliseconds(linuxTimeStamp));
        }

        public static DateTime FromUnixTimeSeconds(
            Int64 linuxTimeStamp)
        {
            return _epoch.Add(TimeSpan.FromSeconds(linuxTimeStamp));
        }
    }
}
