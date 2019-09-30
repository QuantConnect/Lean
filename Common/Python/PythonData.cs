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
using System;

namespace QuantConnect.Python
{
    /// <summary>
    /// Dynamic data class for Python algorithms.
    /// Stores properties of python instances in DynamicData dictionary
    /// </summary>
    public class PythonData : DynamicData
    {
        private readonly dynamic _pythonData;
        private readonly bool _requiresMapping;

        /// <summary>
        /// Constructor for initialising the PythonData class
        /// </summary>
        public PythonData()
        {
            //Empty constructor required for fast-reflection initialization
        }

        /// <summary>
        /// Constructor for initialising the PythonData class with wrapped PyObject
        /// </summary>
        /// <param name="pythonData"></param>
        public PythonData(PyObject pythonData)
        {
            _pythonData = pythonData;
            using (Py.GIL())
            {
                if (pythonData.HasAttr("RequiresMapping"))
                {
                    _requiresMapping = _pythonData.RequiresMapping();
                }
            }
        }

        /// <summary>
        /// Source Locator for algorithm written in Python.
        /// </summary>
        /// <param name="config">Subscription configuration object</param>
        /// <param name="date">Date of the data file we're looking for</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>STRING API Url.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            using (Py.GIL())
            {
                var source = _pythonData.GetSource(config, date, isLiveMode);
                return (source as PyObject).GetAndDispose<SubscriptionDataSource>();
            }
        }

        /// <summary>
        /// Generic Reader Implementation for Python Custom Data.
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">CSV line of data from the souce</param>
        /// <param name="date">Date of the requested line</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            using (Py.GIL())
            {
                var data = _pythonData.Reader(config, line, date, isLiveMode);
                return (data as PyObject).GetAndDispose<BaseData>();
            }
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        public override bool RequiresMapping()
        {
            return _requiresMapping;
        }

        /// <summary>
        /// Indexes into this PythonData, where index is key to the dynamic property
        /// </summary>
        /// <param name="index">the index</param>
        /// <returns>Dynamic property of a given index</returns>
        public object this[string index]
        {
            get
            {
                return GetProperty(index);
            }

            set
            {
                SetProperty(index, value is double ? value.ConvertInvariant<decimal>() : value);
            }
        }
    }
}