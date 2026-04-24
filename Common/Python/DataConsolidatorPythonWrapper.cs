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
    public class DataConsolidatorPythonWrapper : ConsolidatorBase
    {
        private readonly BasePythonWrapper<IDataConsolidator> _pythonWrapper;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData
        {
            get { return _pythonWrapper.GetProperty<IBaseData>(nameof(WorkingData)); }
        }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public override Type InputType
        {
            get { return _pythonWrapper.GetProperty<Type>(nameof(InputType)); }
        }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType
        {
            get { return _pythonWrapper.GetProperty<Type>(nameof(OutputType)); }
        }

        /// <summary>
        /// Constructor for initialising the <see cref="DataConsolidatorPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="consolidator">Represents a custom python consolidator</param>
        public DataConsolidatorPythonWrapper(PyObject consolidator)
        {
            _pythonWrapper = new BasePythonWrapper<IDataConsolidator>(consolidator, true);
            var pythonEvent = _pythonWrapper.GetEvent("DataConsolidated");
            pythonEvent += new DataConsolidatedHandler((_, bar) => OnDataConsolidated(bar));
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public override void Scan(DateTime currentLocalTime)
        {
            _pythonWrapper.InvokeMethod(nameof(Scan), currentLocalTime);
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            _pythonWrapper.InvokeMethod(nameof(Update), data);
        }

        /// <summary>
        /// Resets the consolidator
        /// </summary>
        public override void Reset()
        {
            _pythonWrapper.InvokeMethod(nameof(Reset));
            base.Reset();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _pythonWrapper.Dispose();
        }

        /// <summary>
        /// Two wrappers are equal if they wrap the same Python object reference.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is DataConsolidatorPythonWrapper other)
            {
                return _pythonWrapper.Equals(other._pythonWrapper);
            }
            return _pythonWrapper.Equals(obj);
        }

        /// <summary>
        /// Hash code based on the underlying Python object reference.
        /// </summary>
        public override int GetHashCode() => _pythonWrapper.GetHashCode();
    }
}
