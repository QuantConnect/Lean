/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class EnumExtensions
    {
        private static readonly Char[] _doubleQuotes = { '"' };

        public static String ToEnumString<T>(
            this T enumValue)
        {
            return JsonConvert.SerializeObject(enumValue).Trim(_doubleQuotes);
        }
    }
}
