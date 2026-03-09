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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents the brokerage factory type required to load a data queue handler
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BrokerageFactoryAttribute : Attribute
    {
        /// <summary>
        /// The type of the brokerage factory
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="BrokerageFactoryAttribute"/> class
        /// </summary>
        /// <param name="type">The brokerage factory type</param>
        public BrokerageFactoryAttribute(Type type)
        {
            Type = type;
        }
    }
}
