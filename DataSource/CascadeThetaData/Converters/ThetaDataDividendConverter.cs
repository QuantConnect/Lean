/*
 * Cascade Labs - ThetaData Dividend JSON Converter
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

/// <summary>
/// JSON converter for parsing ThetaData dividend responses
/// API format: [ms_of_day, ex_date, record_date, payment_date, ann_date, dividend_amount, undefined, less_amount, date]
/// </summary>
public class ThetaDataDividendConverter : JsonConverter<DividendResponse>
{
    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can read JSON.
    /// </summary>
    public override bool CanRead => true;

    public override DividendResponse ReadJson(JsonReader reader, Type objectType, DividendResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Array || token.Count() != 9)
        {
            throw new JsonSerializationException(
                $"{nameof(ThetaDataDividendConverter)}.{nameof(ReadJson)}: Invalid token type or count. Expected a JSON array with exactly 9 elements, got {token.Count()} elements.");
        }

        return new DividendResponse(
            msOfDay: token[0]!.Value<uint>(),
            exDate: DateTime.ParseExact(token[1]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture),
            recordDate: DateTime.ParseExact(token[2]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture),
            paymentDate: DateTime.ParseExact(token[3]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture),
            announcementDate: DateTime.ParseExact(token[4]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture),
            dividendAmount: token[5]!.Value<decimal>(),
            // token[6] is undefined/unused
            lessAmount: token[7]!.Value<decimal>(),
            queryDate: DateTime.ParseExact(token[8]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture)
        );
    }

    public override void WriteJson(JsonWriter writer, DividendResponse value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
