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

namespace QuantConnect.Data.Shortable
{
    /// <summary>
    /// Sources the InteractiveBrokers short availability data from the local disk for the given brokerage
    /// </summary>
    public class InteractiveBrokersShortableProvider : LocalDiskShortableProvider
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="securityType">SecurityType to read the short availability data</param>
        /// <param name="market">Market to read the short availability data</param>
        public InteractiveBrokersShortableProvider()
            : base("interactivebrokers")
        {
        }
    }
}
