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

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Data transfer object class
    /// </summary>
    public class OptionAssignmentResult
    {
        /// <summary>
        /// No option assignment should take place
        /// </summary>
        public static OptionAssignmentResult Null { get; } =
            new OptionAssignmentResult(decimal.Zero, string.Empty);

        /// <summary>
        /// The amount of option holdings to trigger the assignment for
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The tag that will be used in the order for the option assignment
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="quantity">The quantity to assign</param>
        /// <param name="tag">The order tag to use</param>
        public OptionAssignmentResult(decimal quantity, string tag)
        {
            Quantity = quantity;
            Tag = tag;
        }
    }
}
