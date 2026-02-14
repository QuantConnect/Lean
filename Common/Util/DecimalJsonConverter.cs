/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Globalization;
using Newtonsoft.Json;

namespace QuantConnect.Util;

/// <summary>
/// Json converter to represent decimals as strings
/// </summary>
public class DecimalJsonConverter : JsonConverter
{
    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(decimal) || objectType == typeof(decimal?);
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(((decimal)value).ToStringInvariant());
        }
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var val = reader.Value;

        if (val == null)
        {
            if (objectType == typeof(decimal?))
            {
                return null;
            }
            throw new JsonSerializationException($"Cannot convert null value to {objectType}.");
        }

        if (val is decimal dec)
        {
            return dec;
        }

        if (val is double d)
        {
            return d.SafeDecimalCast();
        }

        if (IsNumber(val))
        {
            return Convert.ToDecimal(val, CultureInfo.InvariantCulture);
        }

        if (val is string str && TryParse(str, out var res))
        {
            return res;
        }

        throw new JsonSerializationException($"Cannot convert value '{val}' to {objectType}.");
    }

    private static bool TryParse(string str, out decimal value)
    {
        return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsNumber(object value)
    {
        return value is sbyte
               || value is byte
               || value is short
               || value is ushort
               || value is int
               || value is uint
               || value is long
               || value is ulong
               || value is float
               || value is double
               || value is decimal;
    }
}
