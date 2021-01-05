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
using System.Globalization;

namespace QuantConnect.Util
{
    /// <summary>
    /// Allows for conversion of string numeric values from JSON to the <see cref="decimal"/> type
    /// </summary>
    public class StringDecimalJsonConverter : TypeChangeJsonConverter<decimal, string>
    {
        private readonly bool _defaultOnFailure;

        /// <summary>
        /// Creates an instance of the class, with an optional flag to default to decimal's default value on failure.
        /// </summary>
        /// <param name="defaultOnFailure">Default to decimal's default value on failure</param>
        public StringDecimalJsonConverter(bool defaultOnFailure = false)
        {
            _defaultOnFailure = defaultOnFailure;
        }

        /// <summary>
        /// Converts a decimal to a string
        /// </summary>
        /// <param name="value">The input value to be converted before serialization</param>
        /// <returns>String representation of the decimal</returns>
        protected override string Convert(decimal value)
        {
            return value.ToStringInvariant();
        }

        /// <summary>
        /// Converts the input string to a decimal
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to T</param>
        /// <returns>The converted value</returns>
        protected override decimal Convert(string value)
        {
            try
            {
                return decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                if (_defaultOnFailure)
                {
                    return default(decimal);
                }

                throw;
            }
        }
    }
}
