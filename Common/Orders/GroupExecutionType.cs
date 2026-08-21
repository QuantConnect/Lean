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

namespace QuantConnect.Orders
{
    /// <summary>
    /// How the orders that share a <see cref="GroupOrderManager"/> execute relative to each other
    /// </summary>
    public enum GroupExecutionType
    {
        /// <summary>
        /// All legs are placed and filled together as one unit (today's combo behavior) (0)
        /// </summary>
        Combo = 0,

        /// <summary>
        /// One leg fills and every other leg in the group is canceled (1)
        /// </summary>
        OneCancelsTheOther = 1
    }
}
