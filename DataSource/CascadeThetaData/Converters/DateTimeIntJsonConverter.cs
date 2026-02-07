/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

/// <summary>
/// Converts a ThetaData Int DateTime presention to <see cref="DateTime" />
/// </summary>
public class DateTimeIntJsonConverter : JsonConverter
{
    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>
    /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type != JTokenType.Integer)
            return null;

        string? dateText = reader.Value?.ToString();

        return DateTime.ParseExact(dateText!, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
