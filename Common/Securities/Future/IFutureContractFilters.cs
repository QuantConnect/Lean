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

namespace QuantConnect.Securities
{
    /// <summary>
    /// The future contract filters shared by the futures universe selection (<see cref="FutureFilterUniverse"/>)
    /// and the futures chain (<see cref="Data.Market.FuturesChain"/>), so both offer the same filters with the same semantics.
    /// <c>FuturesChainTests.ChainExposesEveryUniverseFilter</c> checks that every universe filter is declared here
    /// </summary>
    /// <typeparam name="TSelf">The implementing type, returned by every filter for chaining</typeparam>
    public interface IFutureContractFilters<TSelf> : IContractFilters<TSelf>
    {
        /// <summary>
        /// Selects the contracts expiring in any of the given months of the year, see <see cref="FutureExpirationCycles"/>
        /// </summary>
        TSelf ExpirationCycle(int[] months);

        /// <summary>
        /// Selects the contracts for the given contract month, the month the contract is named after, which for some products,
        /// e.g. crude oil, is the month after the expiration month
        /// </summary>
        TSelf ContractMonth(int year, int month);

        /// <summary>
        /// Selects the contracts whose contract month is any of the given months of the year, see <see cref="FutureExpirationCycles"/>.
        /// Like <see cref="ExpirationCycle"/> but by the contract month instead of the expiration month
        /// </summary>
        TSelf ContractMonths(IEnumerable<int> months);
    }
}
