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

using System.Collections.Generic;
using Python.Runtime;

namespace QuantConnect.Algorithm.Selection
{
    /// <summary>
    /// Handles Python model instances and method caching for selection models
    /// </summary>
    public class PythonSelectionModelHandler
    {
        private PyObject _pythonModel;
        private readonly Dictionary<string, PyObject> _cachedMethods;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonSelectionModelHandler"/> class
        /// </summary>
        public PythonSelectionModelHandler()
        {
            _cachedMethods = new Dictionary<string, PyObject>();
        }

        /// <summary>
        /// Sets the python model instance and clears any previously cached methods
        /// </summary>
        /// <param name="pythonModel">The python model</param>
        public void SetPythonModel(PyObject pythonModel)
        {
            lock (_lockObject)
            {
                ClearCachedMethods();
                _pythonModel = pythonModel;
            }
        }

        /// <summary>
        /// Gets a cached method from the python model. If the method is not already cached,
        /// it will be retrieved from the python model and cached for future use.
        /// </summary>
        /// <param name="methodName">The name of the method to get from the python model</param>
        /// <returns>The cached PyObject representing the method, or null if no python model is set</returns>
        public PyObject GetCachedMethod(string methodName)
        {
            if (_pythonModel == null) return null;

            lock (_lockObject)
            {
                if (_cachedMethods.TryGetValue(methodName, out var cachedMethod))
                {
                    return cachedMethod;
                }

                using (Py.GIL())
                {
                    var method = _pythonModel.GetPythonMethod(methodName);
                    _cachedMethods[methodName] = method;
                    return method;
                }
            }
        }

        /// <summary>
        /// Clears all cached methods and disposes of the PyObject instances.
        /// This should be called when the python model is changed or when the wrapper is no longer needed.
        /// </summary>
        public void ClearCachedMethods()
        {
            lock (_lockObject)
            {
                foreach (var method in _cachedMethods.Values)
                {
                    method?.Dispose();
                }
                _cachedMethods.Clear();
            }
        }
    }
}