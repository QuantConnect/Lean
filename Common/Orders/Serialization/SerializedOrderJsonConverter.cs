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
using QuantConnect.Util;

namespace QuantConnect.Orders.Serialization
{
    /// <summary>
    /// Defines how Orders should be serialized to json
    /// </summary>
    /// <remarks>This class is to replace <see cref="OrderJsonConverter"/> once all consumers are also updated to support SerializedOrder json format</remarks>
    public class SerializedOrderJsonConverter : TypeChangeJsonConverter<Order, SerializedOrder>
    {
        private readonly string _algorithmId;

        /// <summary>
        /// True will populate TResult object returned by <see cref="Convert(SerializedOrder)"/> with json properties
        /// </summary>
        protected override bool PopulateProperties => false;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="algorithmId">The associated algorithm id, required when serializing</param>
        public SerializedOrderJsonConverter(string algorithmId = null)
        {
            _algorithmId = algorithmId;
        }

        /// <summary>
        /// Returns true if the provided type can be converted
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Order).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Convert the input value to a value to be serialized
        /// </summary>
        /// <param name="value">The input value to be converted before serialization</param>
        /// <returns>A new instance of TResult that is to be serialized</returns>
        protected override SerializedOrder Convert(Order value)
        {
            return new SerializedOrder(value, _algorithmId);
        }

        /// <summary>
        /// Converts the input value to be deserialized
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to <see cref="Order"/></param>
        /// <returns>The converted value</returns>
        protected override Order Convert(SerializedOrder value)
        {
            return Order.FromSerialized(value);
        }
    }
}