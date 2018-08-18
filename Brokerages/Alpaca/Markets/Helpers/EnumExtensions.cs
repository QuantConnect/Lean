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
