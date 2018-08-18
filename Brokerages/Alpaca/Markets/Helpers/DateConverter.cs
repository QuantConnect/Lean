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