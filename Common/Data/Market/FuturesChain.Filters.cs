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
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The futures chain filters, the same ones the futures universe selection offers, see <see cref="IFutureContractFilters{TSelf}"/>.
    /// The filters shared with the option chains live in <see cref="BaseChain{T, TContractsCollection, TSelf, TUniverse}"/>.
    /// Each filter returns a new chain, leaving this one untouched
    /// </summary>
    public partial class FuturesChain
    {
        /// <summary>
        /// Selects the contracts expiring in any of the given months of the year.
        /// Same as <see cref="BaseFutureFilterUniverse{TUniverse, TData}.ExpirationCycle"/>
        /// </summary>
        /// <param name="months">Months to select contracts from, see <see cref="FutureExpirationCycles"/></param>
        /// <returns>A new chain with the filter applied</returns>
        public FuturesChain ExpirationCycle(IEnumerable<int> months)
        {
            return Filter(universe => universe.ExpirationCycle(months));
        }

        /// <summary>
        /// Selects the contracts whose contract month is any of the given months of the year, like <see cref="ExpirationCycle"/>
        /// but by the contract month, the month the contract is named after, which for some products, e.g. crude oil, is the month
        /// after the expiration month.
        /// Same as <see cref="BaseFutureFilterUniverse{TUniverse, TData}.ContractMonths"/>
        /// </summary>
        /// <param name="months">Months of the year to select contracts from, see <see cref="FutureExpirationCycles"/></param>
        /// <returns>A new chain with the filter applied</returns>
        public FuturesChain ContractMonths(IEnumerable<int> months)
        {
            return Filter(universe => universe.ContractMonths(months));
        }

        /// <summary>
        /// Creates the filter universe over the contracts of this chain
        /// </summary>
        protected override FuturesChainFilterUniverse CreateFilterUniverse()
        {
            return new FuturesChainFilterUniverse(this);
        }

        /// <summary>
        /// Creates a copy of this chain with only the given contracts
        /// </summary>
        /// <param name="contracts">The contracts to keep</param>
        protected override FuturesChain CreateChain(IEnumerable<FuturesContract> contracts)
        {
            return new FuturesChain(this, contracts);
        }
    }
}
