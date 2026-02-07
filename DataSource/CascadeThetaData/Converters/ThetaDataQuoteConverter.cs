/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

/// <summary>
/// JSON converter to convert ThetaData Quote
/// </summary>
public class ThetaDataQuoteConverter : JsonConverter<QuoteResponse>
{
    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
    /// </summary>
    /// <value><c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.</value>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets a value indicating whether this <see cref="JsonConverter"/> can read JSON.
    /// </summary>
    /// <value><c>true</c> if this <see cref="JsonConverter"/> can read JSON; otherwise, <c>false</c>.</value>
    public override bool CanRead => true;

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, QuoteResponse value, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="hasExistingValue">The existing value has a value.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override QuoteResponse ReadJson(JsonReader reader, Type objectType, QuoteResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Array || token.Count() != 10) throw new Exception($"{nameof(ThetaDataQuoteConverter)}.{nameof(ReadJson)}: Invalid token type or count. Expected a JSON array with exactly four elements.");

        return new QuoteResponse(
            timeMilliseconds: token[0]!.Value<uint>(),
            bidSize: token[1]!.Value<decimal>(),
            bidExchange: token[2]!.Value<byte>(),
            bidPrice: token[3]!.Value<decimal>(),
            bidCondition: token[4]!.Value<string>() ?? string.Empty,
            askSize: token[5]!.Value<decimal>(),
            askExchange: token[6]!.Value<byte>(),
            askPrice: token[7]!.Value<decimal>(),
            askCondition: token[8]!.Value<string>() ?? string.Empty,
            date: DateTime.ParseExact(token[9]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
