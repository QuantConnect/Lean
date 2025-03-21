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
using QuantConnect.Python;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Collection of <see cref="BaseChain{T, TContractsCollection}"/> keyed by canonical option symbol
    /// </summary>
    public class BaseChains<T, TContract, TContractsCollection> : DataDictionary<T>
        where T : BaseChain<TContract, TContractsCollection>
        where TContract : BaseContract
        where TContractsCollection : DataDictionary<TContract>, new()
    {
        private static readonly IEnumerable<string> _flattenedDfIndexNames = new[] { "canonical", "symbol" };

        private readonly Lazy<PyObject> _dataframe;
        private readonly bool _flatten;

        /// <summary>
        /// The data frame representation of the option chains
        /// </summary>
        public PyObject DataFrame => _dataframe.Value;

        /// <summary>
        /// Creates a new instance of the <see cref="BaseChains{T, TContract, TContractsCollection}"/> dictionary
        /// </summary>
        protected BaseChains()
            : this(default, true)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BaseChains{T, TContract, TContractsCollection}"/> dictionary
        /// </summary>
        protected BaseChains(bool flatten)
            : this(default, flatten)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BaseChains{T, TContract, TContractsCollection}"/> dictionary
        /// </summary>
        protected BaseChains(DateTime time, bool flatten)
            : base(time)
        {
            _flatten = flatten;
            _dataframe = new Lazy<PyObject>(InitializeDataFrame, isThreadSafe: false);
        }

        private PyObject InitializeDataFrame()
        {
            if (!PythonEngine.IsInitialized)
            {
                return null;
            }

            var dataFrames = this.Select(kvp => kvp.Value.DataFrame).ToList();

            if (_flatten)
            {
                var canonicalSymbols = this.Select(kvp => kvp.Key);
                return PandasConverter.ConcatDataFrames(dataFrames, keys: canonicalSymbols, names: _flattenedDfIndexNames, sort: false);
            }

            return PandasConverter.ConcatDataFrames(dataFrames, sort: false);
        }
    }
}
