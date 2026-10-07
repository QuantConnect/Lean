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
using Python.Runtime;
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// A chain of contracts with the filters of its universe selection, see <see cref="IContractFilters{TSelf}"/>.
    /// Each filter returns a new chain, leaving this one untouched
    /// </summary>
    /// <typeparam name="T">The type of contract in the chain</typeparam>
    /// <typeparam name="TContractsCollection">The type of the contracts collection</typeparam>
    /// <typeparam name="TSelf">The chain type, returned by every filter</typeparam>
    /// <typeparam name="TUniverse">The filter universe applying the filters to the contracts of the chain</typeparam>
    public abstract class BaseChain<T, TContractsCollection, TSelf, TUniverse> : BaseChain<T, TContractsCollection>, IContractFilters<TSelf>
        where T : BaseContract
        where TContractsCollection : DataDictionary<T>, new()
        where TSelf : BaseChain<T, TContractsCollection, TSelf, TUniverse>
        where TUniverse : ContractSecurityFilterUniverse<TUniverse, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseChain{T, TContractsCollection, TSelf, TUniverse}"/> class
        /// </summary>
        /// <param name="canonicalSymbol">The symbol for this chain</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="dataType">The type of data this chain represents</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        protected BaseChain(Symbol canonicalSymbol, DateTime time, MarketDataType dataType, bool flatten = true)
            : base(canonicalSymbol, time, dataType, flatten)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseChain{T, TContractsCollection, TSelf, TUniverse}"/> class as a copy of the specified chain
        /// </summary>
        /// <param name="other">The chain to copy</param>
        protected BaseChain(BaseChain<T, TContractsCollection> other)
            : base(other)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseChain{T, TContractsCollection, TSelf, TUniverse}"/> class as a copy of the specified chain
        /// containing only the given subset of its contracts
        /// </summary>
        /// <param name="other">The chain to copy</param>
        /// <param name="contracts">The contracts to keep</param>
        protected BaseChain(BaseChain<T, TContractsCollection> other, IEnumerable<T> contracts)
            : base(other, contracts)
        {
        }

        #region Filters

        /// <summary>
        /// Selects the contracts expiring in the given range relative to the chain date.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(TimeSpan, TimeSpan)"/>
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf Expiration(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            return Filter(universe => universe.Expiration(minExpiry, maxExpiry));
        }

        /// <summary>
        /// Selects the contracts expiring in the given range of days relative to the chain date.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(int, int)"/>
        /// </summary>
        /// <param name="minExpiryDays">The minimum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiryDays">The maximum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf Expiration(int minExpiryDays, int maxExpiryDays)
        {
            return Filter(universe => universe.Expiration(minExpiryDays, maxExpiryDays));
        }

        /// <summary>
        /// Selects the contracts expiring on any of the given dates. Time of day is ignored.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(IEnumerable{DateTime})"/>
        /// </summary>
        /// <param name="expiries">The expiration dates</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf Expiration(IEnumerable<DateTime> expiries)
        {
            return Filter(universe => universe.Expiration(expiries));
        }

        /// <summary>
        /// Selects the contracts expiring after the given date, excluding it. Time of day is ignored.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.ExpiringAfter"/>
        /// </summary>
        /// <param name="date">The date the expirations must be after</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf ExpiringAfter(DateTime date)
        {
            return Filter(universe => universe.ExpiringAfter(date));
        }

        /// <summary>
        /// Selects the contracts expiring before the given date, excluding it. Time of day is ignored.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.ExpiringBefore"/>
        /// </summary>
        /// <param name="date">The date the expirations must be before</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf ExpiringBefore(DateTime date)
        {
            return Filter(universe => universe.ExpiringBefore(date));
        }

        /// <summary>
        /// Selects the contracts expiring today. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.ZeroDte"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf ZeroDte()
        {
            return Filter(universe => universe.ZeroDte());
        }

        /// <summary>
        /// Selects the standard contracts in the chain. Unlike <see cref="ContractSecurityFilterUniverse{T, TData}.StandardsOnly"/>,
        /// it applies to the contracts already selected, so it can be combined with the expiry filters in any order
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf StandardsOnly()
        {
            return Filter(universe => universe.StandardsOnly());
        }

        /// <summary>
        /// Selects the non standard contracts in the chain. Unlike <see cref="ContractSecurityFilterUniverse{T, TData}.WeeklysOnly"/>,
        /// it applies to the contracts already selected, so it can be combined with the expiry filters in any order
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf WeeklysOnly()
        {
            return Filter(universe => universe.WeeklysOnly());
        }

        /// <summary>
        /// Selects the contracts of the nearest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.FrontMonth"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf FrontMonth()
        {
            return Filter(universe => universe.FrontMonth());
        }

        /// <summary>
        /// Selects the contracts of the farthest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.FarthestExpiration"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf FarthestExpiration()
        {
            return Filter(universe => universe.FarthestExpiration());
        }

        /// <summary>
        /// Selects the contracts of all expirations but the nearest one. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.BackMonths"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf BackMonths()
        {
            return Filter(universe => universe.BackMonths());
        }

        /// <summary>
        /// Selects the contracts of the second nearest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.BackMonth"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf BackMonth()
        {
            return Filter(universe => universe.BackMonth());
        }

        /// <summary>
        /// Selects the contracts with open interest in the given range. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.OpenInterest"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf OpenInterest(long min, long max)
        {
            return Filter(universe => universe.OpenInterest(min, max));
        }

        /// <summary>
        /// Selects the contracts with open interest in the given range. Alias for <see cref="OpenInterest"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf OI(long min, long max)
        {
            return OpenInterest(min, max);
        }

        /// <summary>
        /// Selects the contracts with volume in the given range. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Volume"/>
        /// </summary>
        /// <param name="min">The minimum volume</param>
        /// <param name="max">The maximum volume</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf Volume(long min, long max)
        {
            return Filter(universe => universe.Volume(min, max));
        }

        /// <summary>
        /// Selects the contracts matching the given predicate, e.g. <c>chain.where(lambda contract: contract.open_interest > 100)</c>.
        /// From C# use Linq's Where, which keeps this chain's type untouched
        /// </summary>
        /// <param name="predicate">Function determining which contracts are kept</param>
        /// <returns>A new chain with the filter applied</returns>
        public TSelf Where(PyObject predicate)
        {
            return CreateChain(Contracts.Values.Where(predicate.SafeAs<Func<T, bool>>()));
        }

        #endregion

        /// <summary>
        /// Applies the given universe filter to the contracts of this chain and returns the result as a new chain
        /// </summary>
        /// <param name="filter">The universe filter to apply</param>
        /// <returns>A new chain with the filter applied</returns>
        protected TSelf Filter(Func<TUniverse, TUniverse> filter)
        {
            var universe = CreateFilterUniverse();
            // the type filters (standards/weeklys) are only applied on demand, like the universe selection does after the user filter
            return CreateChain(filter(universe).ApplyTypesFilter());
        }

        /// <summary>
        /// Creates the filter universe over the contracts of this chain
        /// </summary>
        protected abstract TUniverse CreateFilterUniverse();

        /// <summary>
        /// Creates a copy of this chain with only the given contracts
        /// </summary>
        /// <param name="contracts">The contracts to keep</param>
        protected abstract TSelf CreateChain(IEnumerable<T> contracts);
    }
}
