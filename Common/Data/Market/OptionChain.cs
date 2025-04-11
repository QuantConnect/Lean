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
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents an entire chain of option contracts for a single underlying security.
    /// This type is <see cref="IEnumerable{OptionContract}"/>
    /// </summary>
    public class OptionChain : BaseChain<OptionContract, OptionContracts>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class
        /// </summary>
        /// <param name="canonicalOptionSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public OptionChain(Symbol canonicalOptionSymbol, DateTime time, bool flatten = true)
            : base(canonicalOptionSymbol, time, MarketDataType.OptionChain, flatten)
        {
        }

        /// <summary>
        /// Initializes a new option chain for a list of contracts as <see cref="OptionUniverse"/> instances
        /// </summary>
        /// <param name="canonicalOptionSymbol">The canonical option symbol</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="contracts">The list of contracts data</param>
        /// <param name="symbolProperties">The option symbol properties</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public OptionChain(Symbol canonicalOptionSymbol, DateTime time, IEnumerable<OptionUniverse> contracts, SymbolProperties symbolProperties,
            bool flatten = true)
            : this(canonicalOptionSymbol, time, flatten)
        {
            foreach (var contractData in contracts)
            {
                Underlying ??= contractData.Underlying;
                if (contractData.Symbol.ID.Date.Date < time.Date) continue;
                Contracts[contractData.Symbol] = OptionContract.Create(contractData, symbolProperties);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class as a clone of the specified instance
        /// </summary>
        private OptionChain(OptionChain other)
            : base(other)
        {
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new OptionChain(this);
        }
    }
}
