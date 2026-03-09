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
 *
*/

using System;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;

namespace QuantConnect.Util
{
    /// <summary>
    /// A <see cref="JsonConverter" /> implementation that serializes a <see cref="Color" /> as a string.
    /// If Color is empty, string is also empty and vice-versa. Meaning that color is autogen.
    /// </summary>
    public class ColorJsonConverter : TypeChangeJsonConverter<Color, string>
    {
        /// <summary>
        /// Converts a .NET Color to a hexadecimal as a string
        /// </summary>
        /// <param name="value">The input value to be converted before serialization</param>
        /// <returns>Hexadecimal number as a string. If .NET Color is null, returns default #000000</returns>
        protected override string Convert(Color value)
        {
            return value.IsEmpty ? string.Empty : $"#{value.R.ToStringInvariant("X2")}{value.G.ToStringInvariant("X2")}{value.B.ToStringInvariant("X2")}";
        }

        /// <summary>
        /// Converts the input string to a .NET Color object
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to T</param>
        /// <returns>The converted value</returns>
        protected override Color Convert(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Color.Empty;
            }
            if (value.Length != 7)
            {
                var message = $"Unable to convert '{value}' to a Color. Requires string length of 7 including the leading hashtag.";
                throw new FormatException(message);
            }

            var red = HexToInt(value.Substring(1, 2));
            var green = HexToInt(value.Substring(3, 2));
            var blue = HexToInt(value.Substring(5, 2));
            return Color.FromArgb(red, green, blue);
        }

        /// <summary>
        /// Converts hexadecimal number to integer
        /// </summary>
        /// <param name="hexValue">Hexadecimal number</param>
        /// <returns>Integer representation of the hexadecimal</returns>
        private int HexToInt(string hexValue)
        {
            if (hexValue.Length != 2)
            {
                var message = $"Unable to convert '{hexValue}' to an Integer. Requires string length of 2.";
                throw new FormatException(message);
            }

            int result;
            if (!int.TryParse(hexValue, NumberStyles.HexNumber, null, out result))
            {
                throw new FormatException($"Invalid hex number: {hexValue}");
            }

            return result;
        }
    }
}