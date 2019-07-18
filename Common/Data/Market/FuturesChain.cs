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
using QuantConnect.Securities.Future;
using QuantConnect.Util;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents an entire chain of futures contracts for a single underlying
    /// This type is <see cref="IEnumerable{FuturesContract}"/>
    /// </summary>
    public class FuturesChain : BaseData, IEnumerable<FuturesContract>
    {
        private readonly Dictionary<Type, Dictionary<Symbol, List<BaseData>>> _auxiliaryData = new Dictionary<Type, Dictionary<Symbol, List<BaseData>>>();

        /// <summary>
        /// Gets the most recent trade information for the underlying. This may
        /// be a <see cref="Tick"/> or a <see cref="TradeBar"/>
        /// </summary>
        public BaseData Underlying
        {
            get; internal set;
        }

        /// <summary>
        /// Gets all ticks for every futures contract in this chain, keyed by symbol
        /// </summary>
        public Ticks Ticks
        {
            get; private set;
        }

        /// <summary>
        /// Gets all trade bars for every futures contract in this chain, keyed by symbol
        /// </summary>
        public TradeBars TradeBars
        {
            get; private set;
        }

        /// <summary>
        /// Gets all quote bars for every futures contract in this chain, keyed by symbol
        /// </summary>
        public QuoteBars QuoteBars
        {
            get; private set;
        }

        /// <summary>
        /// Gets all contracts in the chain, keyed by symbol
        /// </summary>
        public FuturesContracts Contracts
        {
            get; private set;
        }

        /// <summary>
        /// Gets the set of symbols that passed the <see cref="Future.ContractFilter"/>
        /// </summary>
        public HashSet<Symbol> FilteredContracts
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new default instance of the <see cref="FuturesChain"/> class
        /// </summary>
        private FuturesChain()
        {
            DataType = MarketDataType.FuturesChain;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChain"/> class
        /// </summary>
        /// <param name="canonicalFutureSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        public FuturesChain(Symbol canonicalFutureSymbol, DateTime time)
        {
            Time = time;
            Symbol = canonicalFutureSymbol;
            DataType = MarketDataType.FuturesChain;
            Ticks = new Ticks(time);
            TradeBars = new TradeBars(time);
            QuoteBars = new QuoteBars(time);
            Contracts = new FuturesContracts(time);
            FilteredContracts = new HashSet<Symbol>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChain"/> class
        /// </summary>
        /// <param name="canonicalFutureSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="trades">All trade data for the entire futures chain</param>
        /// <param name="quotes">All quote data for the entire futures chain</param>
        /// <param name="contracts">All contracts for this futures chain</param>
        /// <param name="filteredContracts">The filtered list of contracts for this futures chain</param>
        public FuturesChain(Symbol canonicalFutureSymbol, DateTime time, IEnumerable<BaseData> trades, IEnumerable<BaseData> quotes, IEnumerable<FuturesContract> contracts, IEnumerable<Symbol> filteredContracts)
        {
            Time = time;
            Symbol = canonicalFutureSymbol;
            DataType = MarketDataType.FuturesChain;
            FilteredContracts = filteredContracts.ToHashSet();

            Ticks = new Ticks(time);
            TradeBars = new TradeBars(time);
            QuoteBars = new QuoteBars(time);
            Contracts = new FuturesContracts(time);

            foreach (var trade in trades)
            {
                var tick = trade as Tick;
                if (tick != null)
                {
                    List<Tick> ticks;
                    if (!Ticks.TryGetValue(tick.Symbol, out ticks))
                    {
                        ticks = new List<Tick>();
                        Ticks[tick.Symbol] = ticks;
                    }
                    ticks.Add(tick);
                    continue;
                }
                var bar = trade as TradeBar;
                if (bar != null)
                {
                    TradeBars[trade.Symbol] = bar;
                }
            }

            foreach (var quote in quotes)
            {
                var tick = quote as Tick;
                if (tick != null)
                {
                    List<Tick> ticks;
                    if (!Ticks.TryGetValue(tick.Symbol, out ticks))
                    {
                        ticks = new List<Tick>();
                        Ticks[tick.Symbol] = ticks;
                    }
                    ticks.Add(tick);
                    continue;
                }
                var bar = quote as QuoteBar;
                if (bar != null)
                {
                    QuoteBars[quote.Symbol] = bar;
                }
            }

            foreach (var contract in contracts)
            {
                Contracts[contract.Symbol] = contract;
            }
        }

        /// <summary>
        /// Gets the auxiliary data with the specified type and symbol
        /// </summary>
        /// <typeparam name="T">The type of auxiliary data</typeparam>
        /// <param name="symbol">The symbol of the auxiliary data</param>
        /// <returns>The last auxiliary data with the specified type and symbol</returns>
        public T GetAux<T>(Symbol symbol)
        {
            List<BaseData> list;
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!_auxiliaryData.TryGetValue(typeof(T), out dictionary) || !dictionary.TryGetValue(symbol, out list))
            {
                return default(T);
            }
            return list.OfType<T>().LastOrDefault();
        }

        /// <summary>
        /// Gets all auxiliary data of the specified type as a dictionary keyed by symbol
        /// </summary>
        /// <typeparam name="T">The type of auxiliary data</typeparam>
        /// <returns>A dictionary containing all auxiliary data of the specified type</returns>
        public DataDictionary<T> GetAux<T>()
        {
            Dictionary<Symbol, List<BaseData>> d;
            if (!_auxiliaryData.TryGetValue(typeof(T), out d))
            {
                return new DataDictionary<T>();
            }
            var dictionary = new DataDictionary<T>();
            foreach (var kvp in d)
            {
                var item = kvp.Value.OfType<T>().LastOrDefault();
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
        /// <typeparam name="T">The type of auxiliary data</typeparam>
        /// <returns>A dictionary containing all auxiliary data of the specified type</returns>
        public Dictionary<Symbol, List<BaseData>> GetAuxList<T>()
        {
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!_auxiliaryData.TryGetValue(typeof(T), out dictionary))
            {
                return new Dictionary<Symbol, List<BaseData>>();
            }
            return dictionary;
        }

        /// <summary>
        /// Gets a list of auxiliary data with the specified type and symbol
        /// </summary>
        /// <typeparam name="T">The type of auxiliary data</typeparam>
        /// <param name="symbol">The symbol of the auxiliary data</param>
        /// <returns>The list of auxiliary data with the specified type and symbol</returns>
        public List<T> GetAuxList<T>(Symbol symbol)
        {
            List<BaseData> list;
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!_auxiliaryData.TryGetValue(typeof(T), out dictionary) || !dictionary.TryGetValue(symbol, out list))
            {
                return new List<T>();
            }
            return list.OfType<T>().ToList();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<FuturesContract> GetEnumerator()
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
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new FuturesChain
            {
                Ticks = Ticks,
                Contracts = Contracts,
                QuoteBars = QuoteBars,
                TradeBars = TradeBars,
                FilteredContracts = FilteredContracts,
                Symbol = Symbol,
                Time = Time,
                DataType = DataType,
                Value = Value
            };
        }

        /// <summary>
        /// Adds the specified auxiliary data to this futures chain
        /// </summary>
        /// <param name="baseData">The auxiliary data to be added</param>
        internal void AddAuxData(BaseData baseData)
        {
            var type = baseData.GetType();
            Dictionary<Symbol, List<BaseData>> dictionary;
            if (!_auxiliaryData.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<Symbol, List<BaseData>>();
                _auxiliaryData[type] = dictionary;
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