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
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents an entire chain of futures contracts for a single underlying
    /// This type is <see cref="IEnumerable{FuturesContract}"/>
    /// </summary>
    public class FuturesChain : BaseChain<FuturesContract, FuturesContracts>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChain"/> class
        /// </summary>
        /// <param name="canonicalFutureSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public FuturesChain(Symbol canonicalFutureSymbol, DateTime time, bool flatten = true)
            : base(canonicalFutureSymbol, time, MarketDataType.FuturesChain, flatten)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChain"/> class
        /// </summary>
        /// <param name="canonicalFutureSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="contracts">The list of contracts that form this chain</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public FuturesChain(Symbol canonicalFutureSymbol, DateTime time, IEnumerable<FutureUniverse> contracts, bool flatten = true)
            : this(canonicalFutureSymbol, time, flatten)
        {
            foreach (var contractData in contracts)
            {
                if (contractData.Symbol.ID.Date.Date < time.Date) continue;
                Contracts[contractData.Symbol] = new FuturesContract(contractData);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChain"/> class as a clone of the specified instance
        /// </summary>
        private FuturesChain(FuturesChain other)
            : base(other)
        {
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new FuturesChain(this);
        }
    }
}
