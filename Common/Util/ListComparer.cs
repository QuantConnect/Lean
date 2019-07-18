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
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// An implementation of <see cref="IEqualityComparer{T}"/> for <see cref="List{T}"/>.
    /// Useful when using a <see cref="List{T}"/> as the key of a collection.
    /// </summary>
    /// <typeparam name="T">The list type</typeparam>
    public class ListComparer<T> : IEqualityComparer<List<T>>
    {
        /// <summary>Determines whether the specified objects are equal.</summary>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(List<T> x, List<T> y)
        {
            return x.SequenceEqual(y);
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <returns>A hash code for the specified object created from combining the hash
        /// code of all the elements in the collection.</returns>
        public int GetHashCode(List<T> obj)
        {
            var hashCode = 0;
            foreach (var dateTime in obj)
            {
                hashCode = (hashCode * 397) ^ dateTime.GetHashCode();
            }
            return hashCode;
        }
    }
}
