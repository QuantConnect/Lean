/*
 * Cascade Labs - ThetaData Option Contract JSON Converter
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

/// <summary>
/// JSON converter for parsing ThetaData option contract responses
/// API format: [root, expiry, strike, right] e.g., ["AAPL", 20230616, 260000, "P"]
/// </summary>
public class ThetaDataOptionContractConverter : JsonConverter<OptionContractResponse>
{
    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can read JSON.
    /// </summary>
    public override bool CanRead => true;

    public override OptionContractResponse ReadJson(JsonReader reader, Type objectType, OptionContractResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Array || token.Count() != 4)
        {
            throw new JsonSerializationException(
                $"{nameof(ThetaDataOptionContractConverter)}.{nameof(ReadJson)}: Invalid token type or count. Expected a JSON array with exactly 4 elements, got {token.Count()} elements.");
        }

        return new OptionContractResponse(
            root: token[0]!.Value<string>() ?? string.Empty,
            expiry: ParseDate(token[1]!.ToString()),
            strike: token[2]!.Value<decimal>(),
            right: token[3]!.Value<string>() ?? string.Empty
        );
    }

    /// <summary>
    /// Parses a date string in YYYYMMDD format.
    /// </summary>
    private static DateTime ParseDate(string dateString)
    {
        if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }
        throw new JsonSerializationException($"Invalid date format: {dateString}. Expected YYYYMMDD format.");
    }

    public override void WriteJson(JsonWriter writer, OptionContractResponse value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
