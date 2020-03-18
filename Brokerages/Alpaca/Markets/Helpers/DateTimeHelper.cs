#define NET45

/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Changes from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made:
 *   * Defined NET45 in file
 *   * Uses new methods
*/


using System;
using System.Globalization;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class DateTimeHelper
    {
#if NET45
        private static readonly DateTime _epoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

        private const Int64 NanosecondsInMilliseconds = 1000000;

        private static readonly Int64 _timeSpanMaxValueMilliseconds =
            (Int64)TimeSpan.MaxValue.TotalMilliseconds;

        public static String DateFormat { get; } = "yyyy-MM-dd";

        public static String AsDateString(this DateTime dateTime) =>
            dateTime.ToString(DateFormat, CultureInfo.InvariantCulture);

        public static DateTime FromUnixTimeNanoseconds(
            Int64 linuxTimeStamp) =>
            linuxTimeStamp > _timeSpanMaxValueMilliseconds
                ? FromUnixTimeMilliseconds(linuxTimeStamp / NanosecondsInMilliseconds)
                : FromUnixTimeMilliseconds(linuxTimeStamp);

        public static DateTime FromUnixTimeMilliseconds(
            Int64 linuxTimeStamp) =>
#if NET45
            _epoch.Add(TimeSpan.FromMilliseconds(linuxTimeStamp));
#else
            DateTime.SpecifyKind(
                DateTimeOffset.FromUnixTimeMilliseconds(linuxTimeStamp)
                    .DateTime, DateTimeKind.Utc);
#endif

        public static DateTime FromUnixTimeSeconds(
            Int64 linuxTimeStamp) =>
#if NET45
            _epoch.Add(TimeSpan.FromSeconds(linuxTimeStamp));
#else
            DateTime.SpecifyKind(
                DateTimeOffset.FromUnixTimeSeconds(linuxTimeStamp)
                    .DateTime, DateTimeKind.Utc);
#endif

        public static Int64 GetUnixTimeMilliseconds(
            DateTime dateTime) =>
#if NET45
            (Int64)(dateTime.Subtract(_epoch)).TotalMilliseconds;
#else
            new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
#endif
    }
}
