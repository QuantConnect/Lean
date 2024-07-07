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
using System.Text;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Historical data abstraction
    /// </summary>
    /// <typeparam name="T">The data this collection can enumerate</typeparam>
    public class DataHistory<T> : IEnumerable<T>
    {
        private readonly Lazy<int> _count;
        private readonly Lazy<PyObject> _dataframe;

        /// <summary>
        /// The data we hold
        /// </summary>
        protected IEnumerable<T> Data { get; }

        /// <summary>
        /// The current data point count
        /// </summary>
        public int Count => _count.Value;

        /// <summary>
        /// This data pandas data frame
        /// </summary>
        public PyObject DataFrame => _dataframe.Value;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DataHistory(IEnumerable<T> data, Lazy<PyObject> dataframe)
        {
            Data = data.Memoize();
            _dataframe = dataframe;
            // let's be lazy
            _count = new(() => Data.Count());
        }

        /// <summary>
        /// Default to string implementation
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var dataPoint in Data)
            {
                builder.AppendLine(dataPoint.ToString());
            }
            return builder.ToString();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
