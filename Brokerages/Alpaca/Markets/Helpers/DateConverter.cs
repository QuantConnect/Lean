/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using System.Globalization;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class DateConverter : IsoDateTimeConverter
    {
        public DateConverter()
            : this("yyyy-MM-dd")
        {
        }

        public DateConverter(String format)
        {
            DateTimeStyles = DateTimeStyles.AssumeLocal;
            DateTimeFormat = format;
        }
    }
}