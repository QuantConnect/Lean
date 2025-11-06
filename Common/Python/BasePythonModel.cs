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

namespace QuantConnect.Python
{
    /// <summary>
    /// Base class for models that store and manage their corresponding Python instance.
    /// </summary>
    public abstract class BasePythonModel
    {
        /// <summary>
        /// Python instance handler for the model
        /// </summary>
        protected PythonInstanceHandler PythonInstance { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePythonModel"/> class
        /// </summary>
        protected BasePythonModel()
        {
            PythonInstance = new PythonInstanceHandler();
        }

        /// <summary>
        /// Sets the python instance for this model
        /// </summary>
        /// <param name="pythonInstance">The python instance to set</param>
        public void SetPythonInstance(PyObject pythonInstance)
        {
            PythonInstance.SetPythonInstance(pythonInstance);
        }
    }
}