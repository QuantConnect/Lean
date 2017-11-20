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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework
{
    /// <summary>
    /// Provides convenience methods for handling security changes
    /// </summary>
    public static class NotifiedSecurityChanged
    {
        /// <summary>
        /// Adds and removes the security changes to/from the collection
        /// </summary>
        /// <param name="securities">The securities collection to be updated with the changes</param>
        /// <param name="changes">The changes to be applied to the securities collection</param>
        public static void UpdateCollection(ICollection<Security> securities, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                securities.Add(added);
            }
            foreach (var removed in changes.RemovedSecurities)
            {
                securities.Remove(removed);
            }
        }
    }
}