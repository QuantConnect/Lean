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

using System.Collections.Generic;
using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Contains additional properties and settings for an order sent over a FIX connection
    /// </summary>
    public class FixOrderProperties : OrderProperties
    {
        /// <summary>
        /// Custom FIX tags to send with the order. The key is the FIX tag number
        /// and the value is the tag value, e.g. AdditionalProperties["9301"] = "1"
        /// </summary>
        public Dictionary<string, string> AdditionalProperties { get; set; } = [];

        /// <summary>
        /// Instruction for order handling on Broker floor
        /// </summary>
        public char? HandleInstruction { get; set; }

        /// <summary>
        /// Free format text string
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Automated execution order, private, no broker intervention
        /// </summary>
        public const char AutomatedExecutionOrderPrivate = '1';

        /// <summary>
        /// Automated execution order, public, broker, intervention OK
        /// </summary>
        public const char AutomatedExecutionOrderPublic = '2';

        /// <summary>
        /// Staged order, broker intervention required
        /// </summary>
        public const char ManualOrder = '3';

        /// <summary>
        /// Returns a new instance clone of this object
        /// </summary>
        public override IOrderProperties Clone()
        {
            var clone = (FixOrderProperties)MemberwiseClone();
            clone.AdditionalProperties = new Dictionary<string, string>(AdditionalProperties);
            return clone;
        }
    }
}
