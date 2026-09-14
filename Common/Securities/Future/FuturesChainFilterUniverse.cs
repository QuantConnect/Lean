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
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Future contracts filter over the contracts of a <see cref="FuturesChain"/>, so chains offer
    /// the same filters as the futures universe selection (<see cref="FutureFilterUniverse"/>)
    /// </summary>
    public class FuturesChainFilterUniverse : BaseFutureFilterUniverse<FuturesChainFilterUniverse, FuturesContract>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChainFilterUniverse"/> class over the contracts of the given chain
        /// </summary>
        /// <param name="chain">The futures chain to filter</param>
        internal FuturesChainFilterUniverse(FuturesChain chain)
            : base(GetContracts(chain), chain.ExchangeTime)
        {
        }

        /// <summary>
        /// Not supported: the chain filters only ever select contracts that are already in the chain
        /// </summary>
        protected override FuturesContract CreateDataInstance(Symbol symbol)
        {
            throw new InvalidOperationException($"FuturesChainFilterUniverse.CreateDataInstance(): {symbol} is not part of the chain");
        }

        /// <summary>
        /// Gets the open interest of the given contract
        /// </summary>
        protected override decimal GetOpenInterest(FuturesContract contract) => contract.OpenInterest;

        /// <summary>
        /// Gets the volume of the given contract
        /// </summary>
        protected override decimal GetVolume(FuturesContract contract) => contract.Volume;

        private static IReadOnlyList<FuturesContract> GetContracts(FuturesChain chain)
        {
            // The dictionary caches its values as a list that is replaced, never mutated, so it is safe to share
            return chain.Contracts.Values as IReadOnlyList<FuturesContract> ?? chain.Contracts.Values.ToList();
        }
    }
}
