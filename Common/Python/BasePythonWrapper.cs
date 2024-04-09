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

using System.Collections.Concurrent;
using Python.Runtime;

namespace QuantConnect.Python
{
    /// <summary>
    /// Base class for Python wrapper classes
    /// </summary>
    public class BasePythonWrapper
    {
        private PyObject _instance;
        private readonly ConcurrentDictionary<string, PyObject> _pythonMethods;

        /// <summary>
        /// Creates a new instance of the <see cref="BasePythonWrapper"/> class
        /// </summary>
        public BasePythonWrapper()
        {
            _pythonMethods = new();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BasePythonWrapper"/> class with the specified instance
        /// </summary>
        /// <param name="instance">The underlying python instance</param>
        public BasePythonWrapper(PyObject instance)
            : base()
        {
            _instance = instance;
        }

        /// <summary>
        /// Sets the python instance
        /// </summary>
        /// <param name="instance">The underlying python instance</param>
        protected void SetPythonInstance(PyObject instance)
        {
            _instance = instance;
            _pythonMethods.Clear();
        }

        /// <summary>
        /// Gets the Python instances method with the specified name and caches it
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <returns>The matched method</returns>
        protected PyObject GetMethod(string methodName)
        {
            if (!_pythonMethods.TryGetValue(methodName, out var method))
            {
                _pythonMethods[methodName] = method = _instance.GetMethod(methodName);
            }

            return method;
        }

        /// <summary>
        /// Invokes the specified method with the specified arguments
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The returned valued converted to the given type</returns>
        protected T InvokeMethod<T>(string methodName, params object[] args)
        {
            using var _ = Py.GIL();
            var method = GetMethod(methodName);
            return method.Invoke<T>(args);
        }

        /// <summary>
        /// Invokes the specified method with the specified arguments
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="args">The arguments to call the method with</param>
        protected void InvokeMethod(string methodName, params object[] args)
        {
            using var _ = Py.GIL();
            var method = GetMethod(methodName);
            method.Invoke(args);
        }
    }
}
