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

using QuantConnect.Util;

namespace QuantConnect.Orders.Serialization
{
    /// <summary>
    /// Defines how OrderEvents should be serialized to json
    /// </summary>
    public class OrderEventJsonConverter : TypeChangeJsonConverter<OrderEvent, SerializedOrderEvent>
    {
        private readonly string _algorithmId;

        /// <summary>
        /// True will populate TResult object returned by <see cref="Convert(SerializedOrderEvent)"/> with json properties
        /// </summary>
        protected override bool PopulateProperties => false;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="algorithmId">The associated algorithm id, required when serializing</param>
        public OrderEventJsonConverter(string algorithmId = null)
        {
            _algorithmId = algorithmId;
        }

        /// <summary>
        /// Convert the input value to a value to be serialzied
        /// </summary>
        /// <param name="value">The input value to be converted before serialziation</param>
        /// <returns>A new instance of TResult that is to be serialzied</returns>
        protected override SerializedOrderEvent Convert(OrderEvent value)
        {
            return new SerializedOrderEvent(value, _algorithmId);
        }

        /// <summary>
        /// Converts the input value to be deserialized
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to <see cref="OrderEvent"/></param>
        /// <returns>The converted value</returns>
        protected override OrderEvent Convert(SerializedOrderEvent value)
        {
            return OrderEvent.FromSerialized(value);
        }
    }
}
