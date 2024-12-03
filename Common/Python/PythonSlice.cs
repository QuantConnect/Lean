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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides a data structure for all of an algorithm's data at a single time step
    /// </summary>
    public class PythonSlice : Slice
    {
        private readonly Slice _slice;

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonSlice"/> class
        /// </summary>
        /// <param name="slice">slice object to wrap</param>
        public PythonSlice(Slice slice)
            : base(slice)
        {
            _slice = slice;
        }

        /// <summary>
        /// Gets the data of the specified symbol and type.
        /// </summary>
        /// <param name="type">The type of data we seek</param>
        /// <param name="symbol">The specific symbol was seek</param>
        /// <returns>The data for the requested symbol</returns>
        public dynamic Get(PyObject type, Symbol symbol)
        {
            return GetImpl(type.CreateType(), _slice)[symbol];
        }

        /// <summary>
        /// Gets the data of the specified symbol and type.
        /// </summary>
        /// <param name="type">The type of data we seek</param>
        /// <returns>The data for the requested symbol</returns>
        public PyObject Get(PyObject type)
        {
            var result = GetImpl(type.CreateType(), _slice) as object;
            using (Py.GIL())
            {
                return result.ToPython();
            }
        }

        /// <summary>
        /// Gets the number of symbols held in this slice
        /// </summary>
        public override int Count
        {
            get { return _slice.Count; }
        }

        /// <summary>
        /// Gets all the symbols in this slice
        /// </summary>
        public override IReadOnlyList<Symbol> Keys
        {
            get { return _slice.Keys; }
        }

        /// <summary>
        /// Gets a list of all the data in this slice
        /// </summary>
        public override IReadOnlyList<BaseData> Values
        {
            get { return _slice.Values; }
        }

        /// <summary>
        /// Gets the data corresponding to the specified symbol. If the requested data
        /// is of <see cref="MarketDataType.Tick"/>, then a <see cref="List{Tick}"/> will
        /// be returned, otherwise, it will be the subscribed type, for example, <see cref="TradeBar"/>
        /// or event <see cref="UnlinkedData"/> for custom data.
        /// </summary>
        /// <param name="symbol">The data's symbols</param>
        /// <returns>The data for the specified symbol</returns>
        public override dynamic this[Symbol symbol]
        {
            get
            {
                return _slice[symbol];
            }
        }

        /// <summary>
        /// Determines whether this instance contains data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol we seek data for</param>
        /// <returns>True if this instance contains data for the symbol, false otherwise</returns>
        public override bool ContainsKey(Symbol symbol)
        {
            return _slice.ContainsKey(symbol);
        }

        /// <summary>
        /// Gets the data associated with the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol we want data for</param>
        /// <param name="data">The data for the specifed symbol, or null if no data was found</param>
        /// <returns>True if data was found, false otherwise</returns>
        public override bool TryGetValue(Symbol symbol, out dynamic data)
        {
            return _slice.TryGetValue(symbol, out data);
        }
    }
}
