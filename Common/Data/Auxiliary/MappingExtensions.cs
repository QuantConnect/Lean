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

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Mapping extensions helper methods
    /// </summary>
    public static class MappingExtensions
    {
        /// <summary>
        /// Helper method to resolve the mapping file to use.
        /// </summary>
        /// <remarks>This method is aware of the data type being added for <see cref="SecurityType.Base"/>
        /// to the <see cref="SecurityIdentifier.Symbol"/> value</remarks>
        /// <param name="mapFileResolver">The map file resolver</param>
        /// <param name="symbol">The symbol that we want to map</param>
        /// <param name="dataType">The configuration data type <see cref="SubscriptionDataConfig.Type"/></param>
        /// <returns>The mapping file to use</returns>
        public static MapFile ResolveMapFile(this MapFileResolver mapFileResolver,
            Symbol symbol,
            Type dataType)
        {
            // Load the symbol and date to complete the mapFile checks in one statement
            var symbolID = symbol.HasUnderlying ? symbol.Underlying.ID.Symbol : symbol.ID.Symbol;
            var date = symbol.HasUnderlying ? symbol.Underlying.ID.Date : symbol.ID.Date;

            return mapFileResolver.ResolveMapFile(
                symbol.SecurityType == SecurityType.Base ? symbolID.RemoveFromEnd($".{dataType.Name}") : symbolID,
                date);
        }
    }
}
