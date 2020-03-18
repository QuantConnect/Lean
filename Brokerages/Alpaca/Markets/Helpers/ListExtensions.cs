#define NET45

/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes:
 *   * Defined preprocessor directive
*/
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class ListExtensions
    {
#if NET45
        private static class Empty<T>
        {
#pragma warning disable CA1825 // Avoid zero-length array allocations.
            public static readonly T[] Array = new T[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations.
        }
#endif
        public static IReadOnlyList<TInto> EmptyIfNull<TInto, TFrom>(this List<TFrom> list)
            where TFrom : TInto => EmptyIfNull((IReadOnlyList<TInto>) list);

        public static IReadOnlyList<T> EmptyIfNull<T>(this IReadOnlyList<T> list) =>
            list ??
#if NET45
                Empty<T>.Array;
#else
                System.Array.Empty<T>();
#endif
    }
}
