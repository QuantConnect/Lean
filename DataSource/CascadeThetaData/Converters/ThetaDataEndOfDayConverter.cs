/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

public class ThetaDataEndOfDayConverter : JsonConverter<EndOfDayReportResponse>
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

    public override EndOfDayReportResponse ReadJson(JsonReader reader, Type objectType, EndOfDayReportResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Array || token.Count() != 17) throw new Exception($"{nameof(ThetaDataEndOfDayConverter)}.{nameof(ReadJson)}: Invalid token type or count. Expected a JSON array with exactly four elements.");

        existingValue = new EndOfDayReportResponse(
            reportGeneratedTimeMilliseconds: token[0]!.Value<uint>(),
            lastTradeTimeMilliseconds: token[1]!.Value<uint>(),
            open: token[2]!.Value<decimal>(),
            high: token[3]!.Value<decimal>(),
            low: token[4]!.Value<decimal>(),
            close: token[5]!.Value<decimal>(),
            volume: token[6]!.Value<decimal>(),
            amountTrades: token[7]!.Value<uint>(),
            bidSize: token[8]!.Value<decimal>(),
            bidExchange: token[9]!.Value<byte>(),
            bidPrice: token[10]!.Value<decimal>(),
            bidCondition: token[11]!.Value<string>() ?? string.Empty,
            askSize: token[12]!.Value<decimal>(),
            askExchange: token[13]!.Value<byte>(),
            askPrice: token[14]!.Value<decimal>(),
            askCondition: token[15]!.Value<string>() ?? string.Empty,
            date: DateTime.ParseExact(token[16]!.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture)
            );

        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, EndOfDayReportResponse value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
