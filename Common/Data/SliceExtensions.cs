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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides extension methods to slices and slice enumerables
    /// </summary>
    public static class SliceExtensions
    {
        /// <summary>
        /// Selects into the slice and returns the TradeBars that have data in order
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <returns>An enumerable of TradeBars</returns>
        public static IEnumerable<TradeBars> TradeBars(this IEnumerable<Slice> slices)
        {
            return slices.Where(x => x.Bars.Count > 0).Select(x => x.Bars);
        }

        /// <summary>
        /// Selects into the slice and returns the Ticks that have data in order
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <returns>An enumerable of Ticks</returns>
        public static IEnumerable<Ticks> Ticks(this IEnumerable<Slice> slices)
        {
            return slices.Where(x => x.Ticks.Count > 0).Select(x => x.Ticks);
        }

        /// <summary>
        /// Gets the data dictionaries or points of the requested type in each slice
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="type">Data type of the data that will be fetched</param>
        /// <returns>An enumerable of data dictionary or data point of the requested type</returns>
        public static IEnumerable<DataDictionary<BaseDataCollection>> GetUniverseData(this IEnumerable<Slice> slices)
        {
            return slices.SelectMany(x => x.AllData).Select(x =>
            {
                // we wrap the universe data collection into a data dictionary so it fits the api pattern
                return new DataDictionary<BaseDataCollection>(new[] { (BaseDataCollection)x }, (y) => y.Symbol);
            });
        }

        /// <summary>
        /// Gets the data dictionaries or points of the requested type in each slice
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="type">Data type of the data that will be fetched</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of data dictionary or data point of the requested type</returns>
        public static IEnumerable<dynamic> Get(this IEnumerable<Slice> slices, Type type, Symbol symbol = null)
        {
            var result = slices.Select(x => x.Get(type));

            if (symbol == null)
            {
                return result;
            }

            return result.Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of TradeBar for the given symbol. This method does not verify
        /// that the specified symbol points to a TradeBar
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of TradeBar for the matching symbol, of no TradeBar found for symbol, empty enumerable is returned</returns>
        public static IEnumerable<TradeBar> Get(this IEnumerable<Slice> slices, Symbol symbol)
        {
            return slices.TradeBars().Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of T for the given symbol. This method does not vify
        /// that the specified symbol points to a T
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="dataDictionaries">The data dictionary enumerable to access</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of T for the matching symbol, if no T is found for symbol, empty enumerable is returned</returns>
        public static IEnumerable<T> Get<T>(this IEnumerable<DataDictionary<T>> dataDictionaries, Symbol symbol)
            where T : IBaseData
        {
            return dataDictionaries.Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of decimals by accessing the specified field on data for the symbol
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="dataDictionaries">An enumerable of data dictionaries</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <param name="field">The field to access</param>
        /// <returns>An enumerable of decimals</returns>
        public static IEnumerable<decimal> Get<T>(this IEnumerable<DataDictionary<T>> dataDictionaries, Symbol symbol, string field)
        {
            Func<T, decimal> selector;
            if (typeof (DynamicData).IsAssignableFrom(typeof (T)))
            {
                selector = data =>
                {
                    var dyn = (DynamicData) (object) data;
                    return (decimal) dyn.GetProperty(field);
                };
            }
            else if (typeof (T) == typeof (List<Tick>))
            {
                // perform the selection on the last tick
                // NOTE: This is a known bug, should be updated to perform the selection on each item in the list
                var dataSelector = (Func<Tick, decimal>) ExpressionBuilder.MakePropertyOrFieldSelector(typeof (Tick), field).Compile();
                selector = ticks => dataSelector(((List<Tick>) (object) ticks).Last());
            }
            else
            {
                selector = (Func<T, decimal>) ExpressionBuilder.MakePropertyOrFieldSelector(typeof (T), field).Compile();
            }

            foreach (var dataDictionary in dataDictionaries)
            {
                T item;
                if (dataDictionary.TryGetValue(symbol, out item))
                {
                    yield return selector(item);
                }
            }
        }

        /// <summary>
        /// Gets the data dictionaries of the requested type in each slice
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="slices">The enumerable of slice</param>
        /// <returns>An enumerable of data dictionary of the requested type</returns>
        public static IEnumerable<DataDictionary<T>> Get<T>(this IEnumerable<Slice> slices)
            where T : IBaseData
        {
            return slices.Select(x => x.Get<T>()).Where(x => x.Count > 0);
        }

        /// <summary>
        /// Gets an enumerable of T by accessing the slices for the requested symbol
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of T by accessing each slice for the requested symbol</returns>
        public static IEnumerable<T> Get<T>(this IEnumerable<Slice> slices, Symbol symbol)
            where T : IBaseData
        {
            return slices.Select(x => x.Get<T>()).Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of decimal by accessing the slice for the symbol and then retrieving the specified
        /// field on each piece of data
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <param name="field">The field selector used to access the dats</param>
        /// <returns>An enumerable of decimal</returns>
        public static IEnumerable<decimal> Get(this IEnumerable<Slice> slices, Symbol symbol, Func<BaseData, decimal> field)
        {
            foreach (var slice in slices)
            {
                dynamic item;
                if (slice.TryGetValue(symbol, out item))
                {
                    if (item is List<Tick>) yield return field(item.Last());
                    else yield return field(item);
                }
            }
        }

        /// <summary>
        /// Tries to get the data for the specified symbol and type
        /// </summary>
        /// <typeparam name="T">The type of data we want, for example, <see cref="TradeBar"/> or <see cref="UnlinkedData"/>, etc...</typeparam>
        /// <param name="slice">The slice</param>
        /// <param name="symbol">The symbol data is sought for</param>
        /// <param name="data">The found data</param>
        /// <returns>True if data was found for the specified type and symbol</returns>
        public static bool TryGet<T>(this Slice slice, Symbol symbol, out T data)
            where T : IBaseData
        {
            data = default(T);
            var typeData = slice.Get(typeof(T)) as DataDictionary<T>;
            if (typeData.ContainsKey(symbol))
            {
                data = typeData[symbol];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the data for the specified symbol and type
        /// </summary>
        /// <param name="slice">The slice</param>
        /// <param name="type">The type of data we seek</param>
        /// <param name="symbol">The symbol data is sought for</param>
        /// <param name="data">The found data</param>
        /// <returns>True if data was found for the specified type and symbol</returns>
        public static bool TryGet(this Slice slice, Type type, Symbol symbol, out dynamic data)
        {
            data = null;
            var typeData = slice.Get(type);
            if (typeData.ContainsKey(symbol))
            {
                data = typeData[symbol];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts the specified enumerable of decimals into a double array
        /// </summary>
        /// <param name="decimals">The enumerable of decimal</param>
        /// <returns>Double array representing the enumerable of decimal</returns>
        public static double[] ToDoubleArray(this IEnumerable<decimal> decimals)
        {
            return decimals.Select(x => (double) x).ToArray();
        }

        /// <summary>
        /// Loops through the specified slices and pushes the data into the consolidators. This can be used to
        /// easily warm up indicators from a history call that returns slice objects.
        /// </summary>
        /// <param name="slices">The data to send into the consolidators, likely result of a history request</param>
        /// <param name="consolidatorsBySymbol">Dictionary of consolidators keyed by symbol</param>
        public static void PushThroughConsolidators(this IEnumerable<Slice> slices, Dictionary<Symbol, IDataConsolidator> consolidatorsBySymbol)
        {
            PushThroughConsolidators(slices, symbol =>
            {
                IDataConsolidator consolidator;
                consolidatorsBySymbol.TryGetValue(symbol, out consolidator);
                return consolidator;
            });
        }

        /// <summary>
        /// Loops through the specified slices and pushes the data into the consolidators. This can be used to
        /// easily warm up indicators from a history call that returns slice objects.
        /// </summary>
        /// <param name="slices">The data to send into the consolidators, likely result of a history request</param>
        /// <param name="consolidatorsProvider">Delegate that fetches the consolidators by a symbol</param>
        public static void PushThroughConsolidators(this IEnumerable<Slice> slices, Func<Symbol, IDataConsolidator> consolidatorsProvider)
        {
            slices.PushThrough(data => consolidatorsProvider(data?.Symbol)?.Update(data));
        }

        /// <summary>
        /// Loops through the specified slices and pushes the data into the consolidators. This can be used to
        /// easily warm up indicators from a history call that returns slice objects.
        /// </summary>
        /// <param name="slices">The data to send into the consolidators, likely result of a history request</param>
        /// <param name="handler">Delegate handles each data piece from the slice</param>
        public static void PushThrough(this IEnumerable<Slice> slices, Action<BaseData> handler)
        {
            foreach (var slice in slices)
            {
                foreach (var symbol in slice.Keys)
                {
                    dynamic value;
                    if (!slice.TryGetValue(symbol, out value))
                    {
                        continue;
                    }

                    var list = value as IList;
                    var data = (BaseData)(list != null ? list[list.Count - 1] : value);

                    handler(data);
                }
            }
        }
    }
}
