/*
 * Cascade Labs - ThetaData Split JSON Converter
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

/// <summary>
/// JSON converter for parsing ThetaData split responses
/// API format: [ms_of_day, split_date, before_shares, after_shares, date]
/// </summary>
public class ThetaDataSplitConverter : JsonConverter<SplitResponse>
{
    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can read JSON.
    /// </summary>
    public override bool CanRead => true;

    public override SplitResponse ReadJson(JsonReader reader, Type objectType, SplitResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Array || token.Count() != 5)
        {
            throw new JsonSerializationException(
                $"{nameof(ThetaDataSplitConverter)}.{nameof(ReadJson)}: Invalid token type or count. Expected a JSON array with exactly 5 elements, got {token.Count()} elements.");
        }

        return new SplitResponse(
            msOfDay: token[0]!.Value<uint>(),
            splitDate: TryParseDate(token[1]!.ToString()),
            beforeShares: token[2]!.Value<decimal>(),
            afterShares: token[3]!.Value<decimal>(),
            queryDate: TryParseDate(token[4]!.ToString())
        );
    }

    /// <summary>
    /// Attempts to parse a date string in YYYYMMDD format.
    /// Returns DateTime.MinValue if parsing fails (e.g., for '0' or invalid values).
    /// </summary>
    private static DateTime TryParseDate(string dateString)
    {
        if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }
        return DateTime.MinValue;
    }

    public override void WriteJson(JsonWriter writer, SplitResponse value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
