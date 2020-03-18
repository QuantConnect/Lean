/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Changes from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class QueryBuilder
    {
        private readonly IDictionary<String, String> _queryParameters =
            new Dictionary<String, String>();

        public static implicit operator String(QueryBuilder builder) => builder.ToString();

        public QueryBuilder AddParameter(
            String name,
            String value)
        {
            if (value != null)
            {
                _queryParameters.Add(name, value);
            }
            return this;
        }

        public QueryBuilder AddParameter(
            String name,
            Boolean? value) =>
#if NETSTANDARD1_3
            addParameter(name, value, _ => _.ToString());
#else
            addParameter(name, value, _ => _.ToString(CultureInfo.InvariantCulture));
#endif

        public QueryBuilder AddParameter<TValue>(
            String name,
            TValue? value)
            where TValue : struct =>
            addParameter(name, value, EnumExtensions.ToEnumString);

        public QueryBuilder AddParameter<TValue>(
            String name,
            IEnumerable<TValue> values)
            where TValue : struct =>
            addParameter(name, values, EnumExtensions.ToEnumString);

        public QueryBuilder AddParameter(
            String name,
            DateTime? value,
            String format) =>
            addParameter(name, value,
                time =>
                {
                    if (time.Kind == DateTimeKind.Unspecified)
                    {
                        time = DateTime.SpecifyKind(time, DateTimeKind.Utc);
                    }

                    return time.ToString(format, CultureInfo.InvariantCulture);
                });

        public QueryBuilder AddParameter(
            String name,
            Int64? value) =>
            addParameter(name, value,
                time => time.ToString("D", CultureInfo.InvariantCulture));

        public override String ToString()
        {
            using (var content = new FormUrlEncodedContent(_queryParameters))
            {
                return content.ReadAsStringAsync().Result;
            }
        }

        private QueryBuilder addParameter<TValue>(
            String name,
            TValue? value,
            Func<TValue, String> converter)
            where TValue : struct =>
            value.HasValue ? AddParameter(name, converter(value.Value)) : this;

        private QueryBuilder addParameter<TValue>(
            String name,
            IEnumerable<TValue> values,
            Func<TValue, String> converter)
            where TValue : struct =>
            values != null ? AddParameter(name, String.Join(",", values.Select(converter))) : this;
    }
}