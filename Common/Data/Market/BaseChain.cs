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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;
using QuantConnect.Python;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Base representation of an entire chain of contracts for a single underlying security.
    /// This type is <see cref="IEnumerable{T}"/> where T is <see cref="OptionContract"/>, <see cref="FuturesContract"/>, etc.
    /// </summary>
    public class BaseChain<T, TContractsCollection> : BaseData, IEnumerable<T>
        where T : BaseContract
        where TContractsCollection : DataDictionary<T>, new()
    {
        private Dictionary<Type, Dictionary<Symbol, List<BaseData>>> _auxiliaryData;
        private readonly Lazy<PyObject> _dataframe;
        private readonly bool _flatten;

        private Dictionary<Type, Dictionary<Symbol, List<BaseData>>> AuxiliaryData
        {
            get
            {
                if (_auxiliaryData == null)
                {
                    _auxiliaryData = new Dictionary<Type, Dictionary<Symbol, List<BaseData>>>();
                }

                return _auxiliaryData;
            }
        }

        /// <summary>
        /// Gets the most recent trade information for the underlying. This may
        /// be a <see cref="Tick"/> or a <see cref="TradeBar"/>
        /// </summary>
        [PandasIgnore]
        public BaseData Underlying
        {
            get; internal set;
        }

        /// <summary>
        /// Gets all ticks for every option contract in this chain, keyed by option symbol
        /// </summary>
        [PandasIgnore]
        public Ticks Ticks
        {
            get; protected set;
        }

        /// <summary>
        /// Gets all trade bars for every option contract in this chain, keyed by option symbol
        /// </summary>
        [PandasIgnore]
        public TradeBars TradeBars
        {
            get; protected set;
        }

        /// <summary>
        /// Gets all quote bars for every option contract in this chain, keyed by option symbol
        /// </summary>
        [PandasIgnore]
        public QuoteBars QuoteBars
        {
            get; protected set;
        }

        /// <summary>
        /// Gets all contracts in the chain, keyed by option symbol
        /// </summary>
        public TContractsCollection Contracts
        {
            get; private set;
        }

        /// <summary>
        /// Gets the set of symbols that passed the <see cref="Option.ContractFilter"/>
        /// </summary>
        [PandasIgnore]
        public HashSet<Symbol> FilteredContracts
        {
            get; protected set;
        }

        /// <summary>
        /// The data frame representation of the option chain
        /// </summary>
        [PandasIgnore]
        public PyObject DataFrame => _dataframe.Value;

        /// <summary>
        /// The number of contracts in this chain
        /// </summary>
        public int Count => Contracts.Count;

        /// <summary>
        /// Checks if the chain contains a contract with the specified symbol
        /// </summary>
        /// <param name="key">The symbol of the contract to check for</param>
        /// <returns>True if the chain contains a contract with the specified symbol; otherwise, false.</returns>
        public bool ContainsKey(Symbol key)
        {
            return Contracts.ContainsKey(key);
        }

        /// <summary>
        /// Initializes a new default instance of the <see cref="BaseChain{T, TContractsCollection}"/> class
        /// </summary>
        protected BaseChain(MarketDataType dataType, bool flatten)
        {
            DataType = dataType;
            _flatten = flatten;
            _dataframe = new Lazy<PyObject>(
                () =>
                {
                    if (!PythonEngine.IsInitialized)
                    {
                        return null;
                    }
                    return new PandasConverter().GetDataFrame(new[] { this }, symbolOnlyIndex: true, flatten: _flatten);
                },
                isThreadSafe: false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseChain{T, TContractsCollection}"/> class
        /// </summary>
        /// <param name="canonicalOptionSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        protected BaseChain(Symbol canonicalOptionSymbol, DateTime time, MarketDataType dataType, bool flatten = true)
            : this(dataType, flatten)
        {
            Time = time;
            Symbol = canonicalOptionSymbol;
            Ticks = new Ticks(time);
            TradeBars = new TradeBars(time);
            QuoteBars = new QuoteBars(time);
            FilteredContracts = new HashSet<Symbol>();
            Underlying = new QuoteBar();
            Contracts = new();
            Contracts.Time = time;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseChain{T, TContractsCollection}"/> class as a copy of the specified chain
        /// </summary>
        protected BaseChain(BaseChain<T, TContractsCollection> other)
            : this(other.DataType, other._flatten)
        {
            Symbol = other.Symbol;
            Time = other.Time;
            Value = other.Value;
            Underlying = other.Underlying;
            Ticks = other.Ticks;
            QuoteBars = other.QuoteBars;
            TradeBars = other.TradeBars;
            Contracts = other.Contracts;
            FilteredContracts = other.FilteredContracts;
        }

        /// <summary>
        /// Gets the auxiliary data with the specified type and symbol
        /// </summary>
        /// <typeparam name="TAux">The type of auxiliary data</typeparam>
        /// <param name="symbol">The symbol of the auxiliary data</param>
        /// <returns>The last auxiliary data with the specified type and symbol</returns>
        public TAux GetAux<TAux>(Symbol symbol)
        {
            List<BaseData> list;
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!AuxiliaryData.TryGetValue(typeof(TAux), out dictionary) || !dictionary.TryGetValue(symbol, out list))
            {
                return default;
            }
            return list.OfType<TAux>().LastOrDefault();
        }

        /// <summary>
        /// Gets all auxiliary data of the specified type as a dictionary keyed by symbol
        /// </summary>
        /// <typeparam name="TAux">The type of auxiliary data</typeparam>
        /// <returns>A dictionary containing all auxiliary data of the specified type</returns>
        public DataDictionary<TAux> GetAux<TAux>()
        {
            Dictionary<Symbol, List<BaseData>> d;
            if (!AuxiliaryData.TryGetValue(typeof(TAux), out d))
            {
                return new DataDictionary<TAux>();
            }
            var dictionary = new DataDictionary<TAux>();
            foreach (var kvp in d)
            {
                var item = kvp.Value.OfType<TAux>().LastOrDefault();
                if (item != null)
                {
                    dictionary.Add(kvp.Key, item);
                }
            }
            return dictionary;
        }

        /// <summary>
        /// Gets all auxiliary data of the specified type as a dictionary keyed by symbol
        /// </summary>
        /// <typeparam name="TAux">The type of auxiliary data</typeparam>
        /// <returns>A dictionary containing all auxiliary data of the specified type</returns>
        public Dictionary<Symbol, List<BaseData>> GetAuxList<TAux>()
        {
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!AuxiliaryData.TryGetValue(typeof(TAux), out dictionary))
            {
                return new Dictionary<Symbol, List<BaseData>>();
            }
            return dictionary;
        }

        /// <summary>
        /// Gets a list of auxiliary data with the specified type and symbol
        /// </summary>
        /// <typeparam name="TAux">The type of auxiliary data</typeparam>
        /// <param name="symbol">The symbol of the auxiliary data</param>
        /// <returns>The list of auxiliary data with the specified type and symbol</returns>
        public List<TAux> GetAuxList<TAux>(Symbol symbol)
        {
            List<BaseData> list;
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!AuxiliaryData.TryGetValue(typeof(TAux), out dictionary) || !dictionary.TryGetValue(symbol, out list))
            {
                return new List<TAux>();
            }
            return list.OfType<TAux>().ToList();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return Contracts.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds the specified data to this chain
        /// </summary>
        /// <param name="data">The data to be added</param>
        internal void AddData(BaseData data)
        {
            switch (data)
            {
                case Tick tick:
                    Ticks.Add(tick.Symbol, tick);
                    break;

                case TradeBar tradeBar:
                    TradeBars[tradeBar.Symbol] = tradeBar;
                    break;

                case QuoteBar quoteBar:
                    QuoteBars[quoteBar.Symbol] = quoteBar;
                    break;

                default:
                    if (data.DataType == MarketDataType.Base)
                    {
                        AddAuxData(data);
                    }
                    break;
            }
        }

        /// <summary>
        /// Adds the specified auxiliary data to this option chain
        /// </summary>
        /// <param name="baseData">The auxiliary data to be added</param>
        private void AddAuxData(BaseData baseData)
        {
            var type = baseData.GetType();
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!AuxiliaryData.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<Symbol, List<BaseData>>();
                AuxiliaryData[type] = dictionary;
            }

            List<BaseData> list;
            if (!dictionary.TryGetValue(baseData.Symbol, out list))
            {
                list = new List<BaseData>();
                dictionary[baseData.Symbol] = list;
            }
            list.Add(baseData);
        }
    }
}
