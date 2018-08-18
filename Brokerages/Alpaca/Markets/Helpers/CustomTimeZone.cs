using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class CustomTimeZone
    {
        public static TimeZoneInfo Est { get; } =
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    }
}
