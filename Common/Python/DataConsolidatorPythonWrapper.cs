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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides an Data Consolidator that wraps a <see cref="PyObject"/> object that represents a custom Python consolidator
    /// </summary>
    public class DataConsolidatorPythonWrapper : IDataConsolidator
    {
        private readonly dynamic _consolidator;

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated 
        { 
            get { using (Py.GIL()) {return _consolidator.Consolidated;} }
        }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public IBaseData WorkingData 
        { 
            get { using (Py.GIL()) {return _consolidator.WorkingData;} }
        }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType
        {
            get { using (Py.GIL()) {return _consolidator.InputType;} }
        }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public Type OutputType 
        { 
            get { using (Py.GIL()) {return _consolidator.OutputType;} } 
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated
        {
            add { using (Py.GIL()) {_consolidator.DataConsolidated += value;} }
            remove { using (Py.GIL()) {_consolidator.DataConsolidated -= value;} }
        }

        /// <summary>
        /// Constructor for initialising the <see cref="DataConsolidatorPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="consolidator">Represents a custom python consolidator</param>
        public DataConsolidatorPythonWrapper(PyObject consolidator)
        {
            using (Py.GIL())
            {
                foreach (var attributeName in new[] { "InputType", "OutputType", "WorkingData", "Consolidated" })
                {
                    if (!consolidator.HasAttr(attributeName))
                    {
                        throw new NotImplementedException($"IDataConsolidator.{attributeName} must be implemented. Please implement this missing method on {consolidator.GetPythonType()}");
                    }
                }
            }
            _consolidator = consolidator;
        }
        
        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
            using (Py.GIL())
            {
                _consolidator.Scan(currentLocalTime);
            }
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            using (Py.GIL())
            {
                _consolidator.Update(data);
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }
    }
}