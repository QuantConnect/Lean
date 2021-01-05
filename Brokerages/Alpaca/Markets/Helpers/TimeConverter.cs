/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System.Globalization;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class TimeConverter : IsoDateTimeConverter
    {
        public TimeConverter()
        {
            DateTimeStyles = DateTimeStyles.AssumeLocal;
            DateTimeFormat = "HH:mm";
        }
    }
}
