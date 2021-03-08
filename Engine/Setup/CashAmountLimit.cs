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

using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Live trading cash amount limit
    /// </summary>
    public class CashAmountLimit
    {
        /// <summary>
        /// The cash amount to limit
        /// </summary>
        public CashAmount Cash { get; set; }

        /// <summary>
        /// True will enforce this cash amount in the algorithm even if the brokerage does not have this exact cash currency
        /// </summary>
        /// <remarks>This is useful because brokerages like IB allow you to trade in other currencies you don't have handling conversions internally</remarks>
        public bool Force { get; set; }
    }
}
