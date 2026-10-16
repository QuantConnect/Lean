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

using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Compares registered consolidators by the consolidator the user holds: a <see cref="ConsolidatorAdapterBase"/>
    /// and the consolidator it wraps are the same registration, whichever side of the comparison they are on.
    /// Used by the collections that track registered consolidators so lookups by either object succeed.
    /// </summary>
    public sealed class ConsolidatorIdentityComparer : IEqualityComparer<IDataConsolidator>, IEqualityComparer
    {
        /// <summary>
        /// The shared comparer instance
        /// </summary>
        public static readonly ConsolidatorIdentityComparer Instance = new();

        private ConsolidatorIdentityComparer()
        {
        }

        /// <summary>
        /// Determines whether both consolidators refer to the same registration
        /// </summary>
        public bool Equals(IDataConsolidator x, IDataConsolidator y)
        {
            if (x == null || y == null)
            {
                return x == null && y == null;
            }
            return Registered(x).Equals(Registered(y));
        }

        /// <summary>
        /// Gets the hash code of the consolidator the user holds
        /// </summary>
        public int GetHashCode(IDataConsolidator consolidator)
        {
            return Registered(consolidator).GetHashCode();
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals(x as IDataConsolidator, y as IDataConsolidator);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return GetHashCode((IDataConsolidator)obj);
        }

        private static IDataConsolidator Registered(IDataConsolidator consolidator)
        {
            return (consolidator as ConsolidatorAdapterBase)?.Consolidator ?? consolidator;
        }
    }
}
