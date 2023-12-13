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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Derivate security universe selection filter which will always return empty
    /// </summary>
    public class EmptyContractFilter : IDerivativeSecurityFilter
    {
        /// <summary>
        /// True if this universe filter can run async in the data stack
        /// </summary>
        public bool Asynchronous { get; set; } = true;

        /// <summary>
        /// Filters the input set of symbols represented by the universe
        /// </summary>
        /// <param name="universe">derivative symbols universe used in filtering</param>
        /// <returns>The filtered set of symbols</returns>
        public IDerivativeSecurityFilterUniverse Filter(IDerivativeSecurityFilterUniverse universe)
        {
            return new NoneIDerivativeSecurityFilterUniverse();
        }

        private class NoneIDerivativeSecurityFilterUniverse : IDerivativeSecurityFilterUniverse
        {
            public DateTime LocalTime => default;

            public IEnumerator<Symbol> GetEnumerator()
            {
                return Enumerable.Empty<Symbol>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Enumerable.Empty<Symbol>().GetEnumerator();
            }
        }
    }
}
