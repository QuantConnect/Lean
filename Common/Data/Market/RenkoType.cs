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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The type of the RenkoBar
    /// </summary>
    public enum RenkoType
    {
        /// <summary>
        /// Indicates that the RenkoConsolidator works in a
        /// "Classic" manner (ie. that it only returns a single
        /// bar, at most, irrespective of tick movement).
        /// NOTE: the Classic mode has only been retained for
        /// backwards compatability with existing code.
        /// </summary>
        Classic,

        /// <summary>
        /// Indicates that the RenkoConsolidator works properly,
        /// and returns zero or more bars per tick, as appropriate.
        /// </summary>
        Wicked
    }
}
