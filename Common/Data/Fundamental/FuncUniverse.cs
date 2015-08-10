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

using System;
using System.Collections.Generic;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Provides a functional implementation of <see cref="IUniverse"/>
    /// </summary>
    public class FuncUniverse : IUniverse
    {
        private readonly Func<IEnumerable<CoarseFundamental>, IEnumerable<CoarseFundamental>> _coarse;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncUniverse"/> class
        /// </summary>
        /// <param name="coarse">Defines an initial coarse selection</param>
        public FuncUniverse(
            Func<IEnumerable<CoarseFundamental>, IEnumerable<CoarseFundamental>> coarse
            )
        {
            _coarse = coarse;
        }

        /// <summary>
        /// Performs an initial, coarse filter
        /// </summary>
        /// <param name="data">The coarse fundamental data</param>
        /// <returns>The data that passes the filter</returns>
        public IEnumerable<CoarseFundamental> SelectCoarse(IEnumerable<CoarseFundamental> data)
        {
            return _coarse(data);
        }
    }
}