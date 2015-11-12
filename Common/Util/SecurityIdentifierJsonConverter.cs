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

using Newtonsoft.Json;
using QuantConnect.Securities;

namespace QuantConnect.Util
{
    /// <summary>
    /// A <see cref="JsonConverter"/> implementation that serializes a <see cref="SecurityIdentifier"/> as a string
    /// </summary>
    public class SecurityIdentifierJsonConverter : TypeChangeJsonConverter<SecurityIdentifier, string>
    {
        /// <summary>
        /// Converts as security identifier to a string
        /// </summary>
        /// <param name="value">The input value to be converted before serialziation</param>
        /// <returns>A new instance of TResult that is to be serialzied</returns>
        protected override string Convert(SecurityIdentifier value)
        {
            return value.ToString();
        }

        /// <summary>
        /// Converts the input string to a security identifier
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to T</param>
        /// <returns>The converted value</returns>
        protected override SecurityIdentifier Convert(string value)
        {
            return SecurityIdentifier.Parse(value);
        }
    }
}