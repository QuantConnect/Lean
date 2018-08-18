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