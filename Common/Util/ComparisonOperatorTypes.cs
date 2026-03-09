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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Util
{
    /// <summary>
    /// Comparison operators
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum ComparisonOperatorTypes
    {
        /// <summary>
        /// Check if their operands are equal
        /// </summary>
        Equals,

        /// <summary>
        /// Check if their operands are not equal
        /// </summary>
        NotEqual,

        /// <summary>
        /// Checks left-hand operand is greater than its right-hand operand
        /// </summary>
        Greater,

        /// <summary>
        /// Checks left-hand operand is greater or equal to its right-hand operand
        /// </summary>
        GreaterOrEqual,

        /// <summary>
        /// Checks left-hand operand is less than its right-hand operand
        /// </summary>
        Less,

        /// <summary>
        /// Checks left-hand operand is less or equal to its right-hand operand
        /// </summary>
        LessOrEqual
    }
}
