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
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Dynamic data class for Python algorithms.
    /// Stores properties of python instances in DynamicData dictionary
    /// </summary>
    public class PythonData : DynamicData
    {
        private readonly string _pythonTypeName;
        private readonly dynamic _pythonReader;
        private readonly dynamic _pythonGetSource;
        private readonly dynamic _pythonData;
        private readonly dynamic _defaultResolution;
        private readonly dynamic _supportedResolutions;
        private readonly dynamic _isSparseData;
        private readonly dynamic _requiresMapping;
        private DateTime _endTime;

        /// <summary>
        /// The end time of this data. Some data covers spans (trade bars)
        /// and as such we want to know the entire time span covered
        /// </summary>
        /// <remarks>
        /// This property is overriden to allow different values for Time and EndTime
        /// if they are set in the Reader. In the base implementation EndTime equals Time
        /// </remarks>
        public override DateTime EndTime
        {
            get
            {
                return _endTime == default ? Time : _endTime;
            }
            set
            {
                _endTime = value;
                if(Time == default)
                {
                    // if Time hasn't been set let's set it, like BaseData does. If the user overrides it that's okay
                    Time = value;
                }
            }
        }

        /// <summary>
        /// Constructor for initializing the PythonData class
        /// </summary>
        public PythonData()
        {
            //Empty constructor required for fast-reflection initialization
        }

        /// <summary>
        /// Constructor for initializing the PythonData class with wrapped PyObject
        /// </summary>
        /// <param name="pythonData"></param>
        public PythonData(PyObject pythonData)
        {
            _pythonData = pythonData;
            using (Py.GIL())
            {
                // these methods rely on the Symbol so we can call them yet but we can get them
                _requiresMapping = pythonData.GetMethod("RequiresMapping");
                _isSparseData = pythonData.GetMethod("IsSparseData");
                _defaultResolution = pythonData.GetMethod("DefaultResolution");
                _supportedResolutions = pythonData.GetMethod("SupportedResolutions");
                _pythonReader = pythonData.GetMethod("Reader");
                _pythonGetSource = pythonData.GetMethod("GetSource");
                _pythonTypeName = pythonData.GetPythonType().GetAssemblyName().Name;
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
                var source = _pythonGetSource(config, date, isLiveMode);
                return (source as PyObject).GetAndDispose<SubscriptionDataSource>();
            }
        }

        /// <summary>
        /// Generic Reader Implementation for Python Custom Data.
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">CSV line of data from the source</param>
        /// <param name="date">Date of the requested line</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            using (Py.GIL())
            {
                var data = _pythonReader(config, line, date, isLiveMode);
                var result = (data as PyObject).GetAndDispose<BaseData>();

                (result as PythonData)?.SetProperty("__typename", _pythonTypeName);

                return result;
            }
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        public override bool RequiresMapping()
        {
            if (_requiresMapping == null)
            {
                return base.RequiresMapping();
            }
            using (Py.GIL())
            {
                return _requiresMapping();
            }
        }

        /// <summary>
        /// Indicates that the data set is expected to be sparse
        /// </summary>
        /// <remarks>Relies on the <see cref="Symbol"/> property value</remarks>
        /// <returns>True if the data set represented by this type is expected to be sparse</returns>
        public override bool IsSparseData()
        {
            if (_isSparseData == null)
            {
                return base.IsSparseData();
            }
            using (Py.GIL())
            {
                return _isSparseData();
            }
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        public override Resolution DefaultResolution()
        {
            if (_defaultResolution == null)
            {
                return base.DefaultResolution();
            }
            using (Py.GIL())
            {
                return _defaultResolution();
            }
        }

        /// <summary>
        /// Gets the supported resolution for this data and security type
        /// </summary>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        public override List<Resolution> SupportedResolutions()
        {
            if (_supportedResolutions == null)
            {
                return base.SupportedResolutions();
            }
            using (Py.GIL())
            {
                return _supportedResolutions();
            }
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

        /// <summary>
        /// Helper method to determine if the current instance is of the provided type
        /// </summary>
        /// <param name="type">Target type to check against</param>
        /// <returns>True if this instance is of the provided type</returns>
        public bool IsOfType(Type type)
        {
            if (HasProperty("__typename"))
            {
                return (string)GetProperty("__typename") == type.FullName;
            }
            return GetType() == type;
        }
    }
}
