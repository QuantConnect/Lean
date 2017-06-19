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
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a functional implementation of <see cref="IDerivativeSecurityFilter"/>
    /// </summary>
    public class FuncSecurityDerivativeFilter : IDerivativeSecurityFilter
    {
        private readonly Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> _filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncSecurityDerivativeFilter"/> class
        /// </summary>
        /// <param name="filter">The functional implementation of the <see cref="Filter"/> method</param>
        public FuncSecurityDerivativeFilter(Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> filter)
        {
            _filter = filter;
        }

        /// <summary>
        /// Filters the input set of symbols represented by the universe 
        /// </summary>
        /// <param name="universe">Derivative symbols universe used in filtering</param>
        /// <returns>The filtered set of symbols</returns>
        public IDerivativeSecurityFilterUniverse Filter(IDerivativeSecurityFilterUniverse universe)
        {
            return _filter(universe);
        }
    }
}