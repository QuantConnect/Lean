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

using Common.Util;
using QuantConnect.Python;
using System;
using System.Collections.Generic;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Provides a base class for types holding base data instances keyed by symbol
    /// </summary>
    [PandasNonExpandable]
    public class DataDictionary<T> : BaseExtendedDictionary<Symbol, T>
    {
        /// <summary>
        /// Gets or sets the time associated with this collection of data
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class.
        /// </summary>
        public DataDictionary() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this data dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public DataDictionary(IEnumerable<T> data, Func<T, Symbol> keySelector)
            : base(data, keySelector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class.
        /// </summary>
        /// <param name="time">The time this data was emitted.</param>
        public DataDictionary(DateTime time) : base()
        {
            Time = time;
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        public override T this[Symbol symbol]
        {
            get
            {
                T data;
                if (TryGetValue(symbol, out data))
                {
                    return data;
                }
                CheckForImplicitlyCreatedSymbol(symbol);
                throw new KeyNotFoundException($"'{symbol}' wasn't found in the {GetType().GetBetterTypeName()} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{symbol}\")");
            }
            set => base[symbol] = value;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public virtual T GetValue(Symbol key)
        {
            T value;
            TryGetValue(key, out value);
            return value;
        }
    }

    /// <summary>
    /// Provides extension methods for the DataDictionary class
    /// </summary>
    public static class DataDictionaryExtensions
    {
        /// <summary>
        /// Provides a convenience method for adding a base data instance to our data dictionary
        /// </summary>
        public static void Add<T>(this DataDictionary<T> dictionary, T data)
            where T : BaseData
        {
            dictionary.Add(data.Symbol, data);
        }
    }
}
