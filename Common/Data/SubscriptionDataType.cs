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

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents the data type and tick type for a subscription
    /// </summary>
    public class SubscriptionDataType
    {
        /// <summary>
        /// Gets the data type used to process the subscription request, this type must derive from BaseData
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// Gets the tick type for the subscription request, currently can be Trade, Quote or OpenInterest
        /// </summary>
        public TickType TickType { get; }

        /// <summary>
        /// Initializes a new default instance of the <see cref="SubscriptionDataType"/> class
        /// </summary>
        public SubscriptionDataType(Type dataType, TickType tickType)
        {
            DataType = dataType;
            TickType = tickType;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"{DataType.Name}/{TickType}";
        }
    }
}
