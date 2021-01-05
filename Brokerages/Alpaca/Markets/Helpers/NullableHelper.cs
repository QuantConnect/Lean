/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made from original:
 *   - Removed throw expression in EnsureNotNull method
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class NullableHelper
    {
        public static T EnsureNotNull<T>(this T value, String name) where T : class
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(name);
            }

            return value;
        }
    }
}
