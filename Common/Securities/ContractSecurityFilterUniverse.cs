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
using Python.Runtime;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base class for contract symbols filtering universes.
    /// Used by OptionFilterUniverse and FutureFilterUniverse
    /// </summary>
    public abstract class ContractSecurityFilterUniverse<T, TData> : IDerivativeSecurityFilterUniverse<TData>
        where T: ContractSecurityFilterUniverse<T, TData>
        where TData: IChainUniverseData
    {
        private bool _alreadyAppliedTypeFilters;

        private IEnumerable<TData> _data;

        /// <summary>
        /// Defines listed contract types with Flags attribute
        /// </summary>
        [Flags]
        protected enum ContractExpirationType : int
        {
            /// <summary>
            /// Standard contracts
            /// </summary>
            Standard = 1,

            /// <summary>
            /// Non standard weekly contracts
            /// </summary>
            Weekly = 2
        }

        /// <summary>
        /// Expiration Types allowed through the filter
        /// Standards only by default
        /// </summary>
        protected ContractExpirationType Type { get; set; } = ContractExpirationType.Standard;

        /// <summary>
        /// The local exchange current time
        /// </summary>
        public DateTime LocalTime { get; private set; }

        /// <summary>
        /// All data in this filter
        /// Marked internal for use by extensions
        /// </summary>
        /// <remarks>
        /// Setting it will also set AllSymbols
        /// </remarks>
        internal IEnumerable<TData> Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        /// <summary>
        /// All Symbols in this filter
        /// Marked internal for use by extensions
        /// </summary>
        /// <remarks>
        /// Setting it will remove any data that doesn't have a symbol in AllSymbols
        /// </remarks>
        internal IEnumerable<Symbol> AllSymbols
        {
            get
            {
                return _data.Select(x => x.Symbol);
            }
            set
            {
                // We create a "fake" data instance for each symbol that is not in the data,
                // so we are polite to the user and keep backwards compatibility
                _data = value.Select(symbol => _data.FirstOrDefault(x => x.Symbol == symbol) ?? CreateDataInstance(symbol)).ToList();
            }
        }

        /// <summary>
        /// Constructs ContractSecurityFilterUniverse
        /// </summary>
        protected ContractSecurityFilterUniverse()
        {
        }

        /// <summary>
        /// Constructs ContractSecurityFilterUniverse
        /// </summary>
        protected ContractSecurityFilterUniverse(IEnumerable<TData> allData, DateTime localTime)
        {
            Data = allData;
            LocalTime = localTime;
            Type = ContractExpirationType.Standard;
        }

        /// <summary>
        /// Function to determine if the given symbol is a standard contract
        /// </summary>
        /// <returns>True if standard type</returns>
        protected abstract bool IsStandard(Symbol symbol);

        /// <summary>
        /// Creates a new instance of the data type for the given symbol
        /// </summary>
        /// <returns>A data instance for the given symbol</returns>
        protected abstract TData CreateDataInstance(Symbol symbol);

        /// <summary>
        /// Returns universe, filtered by contract type
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        internal T ApplyTypesFilter()
        {
            if (_alreadyAppliedTypeFilters)
            {
                return (T) this;
            }

            // memoization map for ApplyTypesFilter()
            var memoizedMap = new Dictionary<DateTime, bool>();

            Func<TData, bool> memoizedIsStandardType = data =>
            {
                var dt = data.ID.Date;

                bool result;
                if (memoizedMap.TryGetValue(dt, out result))
                    return result;
                var res = IsStandard(data.Symbol);
                memoizedMap[dt] = res;

                return res;
            };

            Data = Data.Where(x =>
            {
                switch (Type)
                {
                    case ContractExpirationType.Weekly:
                        return !memoizedIsStandardType(x);
                    case ContractExpirationType.Standard:
                        return memoizedIsStandardType(x);
                    case ContractExpirationType.Standard | ContractExpirationType.Weekly:
                        return true;
                    default:
                        return false;
                }
            }).ToList();

            _alreadyAppliedTypeFilters = true;
            return (T) this;
        }

        /// <summary>
        /// Refreshes this filter universe
        /// </summary>
        /// <param name="allData">All data for contracts in the Universe</param>
        /// <param name="localTime">The local exchange current time</param>
        public virtual void Refresh(IEnumerable<TData> allData, DateTime localTime)
        {
            Data = allData;
            LocalTime = localTime;
            Type = ContractExpirationType.Standard;
            _alreadyAppliedTypeFilters = false;
        }

        /// <summary>
        /// Sets universe of standard contracts (if any) as selection
        /// Contracts by default are standards; only needed to switch back if changed
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public T StandardsOnly()
        {
            if (_alreadyAppliedTypeFilters)
            {
                throw new InvalidOperationException("Type filters have already been applied, " +
                    "please call StandardsOnly() before applying other filters such as FrontMonth() or BackMonths()");
            }

            Type = ContractExpirationType.Standard;
            return (T)this;
        }

        /// <summary>
        /// Includes universe of non-standard weeklys contracts (if any) into selection
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public T IncludeWeeklys()
        {
            if (_alreadyAppliedTypeFilters)
            {
                throw new InvalidOperationException("Type filters have already been applied, " +
                    "please call IncludeWeeklys() before applying other filters such as FrontMonth() or BackMonths()");
            }

            Type |= ContractExpirationType.Weekly;
            return (T)this;
        }

        /// <summary>
        /// Sets universe of weeklys contracts (if any) as selection
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public T WeeklysOnly()
        {
            Type = ContractExpirationType.Weekly;
            return (T)this;
        }

        /// <summary>
        /// Returns front month contract
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public virtual T FrontMonth()
        {
            ApplyTypesFilter();
            var ordered = Data.OrderBy(x => x.ID.Date).ToList();
            if (ordered.Count == 0) return (T) this;
            var frontMonth = ordered.TakeWhile(x => ordered[0].ID.Date == x.ID.Date);

            Data = frontMonth.ToList();
            return (T) this;
        }

        /// <summary>
        /// Returns a list of back month contracts
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public virtual T BackMonths()
        {
            ApplyTypesFilter();
            var ordered = Data.OrderBy(x => x.ID.Date).ToList();
            if (ordered.Count == 0) return (T) this;
            var backMonths = ordered.SkipWhile(x => ordered[0].ID.Date == x.ID.Date);

            Data = backMonths.ToList();
            return (T) this;
        }

        /// <summary>
        /// Returns first of back month contracts
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public T BackMonth()
        {
            return BackMonths().FrontMonth();
        }

        /// <summary>
        /// Adjust the reference date used for expiration filtering. By default it just returns the same date.
        /// </summary>
        /// <param name="referenceDate">The reference date to be adjusted</param>
        /// <returns>The adjusted date</returns>
        protected virtual DateTime AdjustExpirationReferenceDate(DateTime referenceDate)
        {
            return referenceDate;
        }

        /// <summary>
        /// Applies filter selecting options contracts based on a range of expiration dates relative to the current day
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>Universe with filter applied</returns>
        public virtual T Expiration(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            if (LocalTime == default)
            {
                return (T)this;
            }

            if (maxExpiry > Time.MaxTimeSpan) maxExpiry = Time.MaxTimeSpan;

            var referenceDate = AdjustExpirationReferenceDate(LocalTime.Date);

            var minExpiryToDate = referenceDate + minExpiry;
            var maxExpiryToDate = referenceDate + maxExpiry;

            Data = Data
                .Where(symbol => symbol.ID.Date.Date >= minExpiryToDate && symbol.ID.Date.Date <= maxExpiryToDate)
                .ToList();

            return (T)this;
        }

        /// <summary>
        /// Applies filter selecting contracts based on a range of expiration dates relative to the current day
        /// </summary>
        /// <param name="minExpiryDays">The minimum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiryDays">The maximum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>Universe with filter applied</returns>
        public T Expiration(int minExpiryDays, int maxExpiryDays)
        {
            return Expiration(TimeSpan.FromDays(minExpiryDays), TimeSpan.FromDays(maxExpiryDays));
        }

        /// <summary>
        /// Explicitly sets the selected contract symbols for this universe.
        /// This overrides and and all other methods of selecting symbols assuming it is called last.
        /// </summary>
        /// <param name="contracts">The option contract symbol objects to select</param>
        /// <returns>Universe with filter applied</returns>
        public T Contracts(PyObject contracts)
        {
            // Let's first check if the object is a selector:
            if (contracts.TryConvertToDelegate(out Func<IEnumerable<TData>, IEnumerable<Symbol>> contractSelector))
            {
                return Contracts(contractSelector);
            }

            // Else, it should be a list of symbols:
            return Contracts(contracts.ConvertToSymbolEnumerable());
        }

        /// <summary>
        /// Explicitly sets the selected contract symbols for this universe.
        /// This overrides and and all other methods of selecting symbols assuming it is called last.
        /// </summary>
        /// <param name="contracts">The option contract symbol objects to select</param>
        /// <returns>Universe with filter applied</returns>
        public T Contracts(IEnumerable<Symbol> contracts)
        {
            AllSymbols = contracts.ToList();
            return (T) this;
        }

        /// <summary>
        /// Explicitly sets the selected contract symbols for this universe.
        /// This overrides and and all other methods of selecting symbols assuming it is called last.
        /// </summary>
        /// <param name="contracts">The option contract symbol objects to select</param>
        /// <returns>Universe with filter applied</returns>
        public T Contracts(IEnumerable<TData> contracts)
        {
            Data = contracts.ToList();
            return (T)this;
        }

        /// <summary>
        /// Sets a function used to filter the set of available contract filters. The input to the 'contractSelector'
        /// function will be the already filtered list if any other filters have already been applied.
        /// </summary>
        /// <param name="contractSelector">The option contract symbol objects to select</param>
        /// <returns>Universe with filter applied</returns>
        public T Contracts(Func<IEnumerable<TData>, IEnumerable<Symbol>> contractSelector)
        {
            // force materialization using ToList
            AllSymbols = contractSelector(Data).ToList();
            return (T) this;
        }

        /// <summary>
        /// Sets a function used to filter the set of available contract filters. The input to the 'contractSelector'
        /// function will be the already filtered list if any other filters have already been applied.
        /// </summary>
        /// <param name="contractSelector">The option contract symbol objects to select</param>
        /// <returns>Universe with filter applied</returns>
        public T Contracts(Func<IEnumerable<TData>, IEnumerable<TData>> contractSelector)
        {
            // force materialization using ToList
            Data = contractSelector(Data).ToList();
            return (T)this;
        }

        /// <summary>
        /// Instructs the engine to only filter contracts on the first time step of each market day.
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        /// <remarks>Deprecated since filters are always non-dynamic now</remarks>
        [Obsolete("Deprecated as of 2023-12-13. Filters are always non-dynamic as of now, which means they will only bee applied daily.")]
        public T OnlyApplyFilterAtMarketOpen()
        {
            return (T) this;
        }

        /// <summary>
        /// IEnumerable interface method implementation
        /// </summary>
        /// <returns>IEnumerator of Symbols in Universe</returns>
        public IEnumerator<TData> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        /// <summary>
        /// IEnumerable interface method implementation
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Data.GetEnumerator();
        }
    }
}
