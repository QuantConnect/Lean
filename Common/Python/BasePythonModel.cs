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

namespace QuantConnect.Python
{
    /// <summary>
    /// Base class for models that store and manage their corresponding Python instance.
    /// </summary>
    public abstract class BasePythonModel
    {
        private PyObject _pythonInstance;
        private readonly Dictionary<string, PyObject> _cachedMethods;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePythonModel"/> class
        /// </summary>
        protected BasePythonModel()
        {
            _cachedMethods = new Dictionary<string, PyObject>();
        }

        /// <summary>
        /// Attempts to execute a method on the stored Python instance if it exists and is callable.
        /// </summary>
        /// <typeparam name="T">The expected return type of the Python method.</typeparam>
        /// <param name="methodName">The name of the method to call on the Python instance.</param>
        /// <param name="result">When this method returns, contains the method result if the call succeeded.</param>
        /// <param name="args">The arguments to pass to the Python method.</param>
        /// <returns>true if the Python method was successfully invoked, otherwise, false.</returns>
        public bool TryExecuteMethod<T>(string methodName, out T result, params object[] args)
        {
            lock (_lockObject)
            {
                var method = GetCachedMethod(methodName);
                if (method != null)
                {
                    using (Py.GIL())
                    {
                        result = method.Invoke<T>(args);
                        return true;
                    }
                }
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Retrieves a cached Python method. 
        /// If it has not been cached yet, it is fetched from the Python instance and stored for future access.
        /// </summary>
        /// <param name="methodName">The name of the Python method to retrieve.</param>
        /// <returns>
        /// The cached <see cref="PyObject"/> representing the Python method, 
        /// or <c>null</c> if no Python instance is currently set.
        /// </returns>
        private PyObject GetCachedMethod(string methodName)
        {
            if (_pythonInstance == null)
            {
                return null;
            }

            if (_cachedMethods.TryGetValue(methodName, out var cachedMethod))
            {
                return cachedMethod;
            }

            using (Py.GIL())
            {
                var method = _pythonInstance.GetPythonMethod(methodName);
                _cachedMethods[methodName] = method;
                return method;
            }
        }

        /// <summary>
        /// Disposes of all cached Python methods and clears the cache.
        /// Should be called when the Python instance changes or when cleanup is needed.
        /// </summary>
        private void ClearCachedMethods()
        {
            foreach (var method in _cachedMethods.Values)
            {
                method?.Dispose();
            }
            _cachedMethods.Clear();
        }

        /// <summary>
        /// Assigns a Python object instance to this handler and clears any previously cached methods.
        /// </summary>
        /// <param name="pythonInstance">The Python object instance to manage.</param>
        public void SetPythonInstance(PyObject pythonInstance)
        {
            lock (_lockObject)
            {
                ClearCachedMethods();
                _pythonInstance = pythonInstance;
            }
        }
    }
}