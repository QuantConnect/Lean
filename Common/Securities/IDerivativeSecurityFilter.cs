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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Filters a set of derivative symbols using the underlying price data.
    /// </summary>
    public interface IDerivativeSecurityFilter<T>
         where T : ISymbol
    {
        /// <summary>
        /// Filters the input set of symbols represented by the universe
        /// </summary>
        /// <param name="universe">derivative symbols universe used in filtering</param>
        /// <returns>The filtered set of symbols</returns>
        IDerivativeSecurityFilterUniverse<T> Filter(IDerivativeSecurityFilterUniverse<T> universe);

        /// <summary>
        /// True if this universe filter can run async in the data stack
        /// </summary>
        bool Asynchronous { get; set; }
    }
}
