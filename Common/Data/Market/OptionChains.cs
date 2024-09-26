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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Collection of <see cref="OptionChain"/> keyed by canonical option symbol
    /// </summary>
    public class OptionChains : DataDictionary<OptionChain>
    {
        private static readonly IEnumerable<string> _indexNames = new[] { "canonical", "symbol" };

        private readonly Lazy<PyObject> _dataframe;

        /// <summary>
        /// Creates a new instance of the <see cref="OptionChains"/> dictionary
        /// </summary>
        public OptionChains()
            : this(default)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OptionChains"/> dictionary
        /// </summary>
        public OptionChains(DateTime time)
            : base(time)
        {
            _dataframe = new Lazy<PyObject>(InitializeDataFrame);
        }

        /// <summary>
        /// The data frame representation of the option chains
        /// </summary>
        public PyObject DataFrame => _dataframe.Value;

        /// <summary>
        /// Gets or sets the OptionChain with the specified ticker.
        /// </summary>
        /// <returns>
        /// The OptionChain with the specified ticker.
        /// </returns>
        /// <param name="ticker">The ticker of the element to get or set.</param>
        /// <remarks>Wraps the base implementation to enable indexing in python algorithms due to pythonnet limitations</remarks>
        public new OptionChain this[string ticker] { get { return base[ticker]; } set { base[ticker] = value; } }

        /// <summary>
        /// Gets or sets the OptionChain with the specified Symbol.
        /// </summary>
        /// <returns>
        /// The OptionChain with the specified Symbol.
        /// </returns>
        /// <param name="symbol">The Symbol of the element to get or set.</param>
        /// <remarks>Wraps the base implementation to enable indexing in python algorithms due to pythonnet limitations</remarks>
        public new OptionChain this[Symbol symbol] { get { return base[symbol]; } set { base[symbol] = value; } }

        private PyObject InitializeDataFrame()
        {
            var dataFrames = this.Select(kvp => kvp.Value.DataFrame).ToList();
            var canonicalSymbols = this.Select(kvp => kvp.Key);

            return PandasConverter.ConcatDataFrames(dataFrames, keys: canonicalSymbols, names: _indexNames);
        }
    }
}
