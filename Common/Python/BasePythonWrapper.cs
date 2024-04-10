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
        private readonly ConcurrentDictionary<string, string> _pythonPropertyNames;

        /// <summary>
        /// Gets the underlying python instance
        /// </summary>
        protected PyObject Instance => _instance;

        /// <summary>
        /// Creates a new instance of the <see cref="BasePythonWrapper"/> class
        /// </summary>
        public BasePythonWrapper()
        {
            _pythonMethods = new();
            _pythonPropertyNames = new();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BasePythonWrapper"/> class with the specified instance
        /// </summary>
        /// <param name="instance">The underlying python instance</param>
        public BasePythonWrapper(PyObject instance)
            : this()
        {
            _instance = instance;
        }

        /// <summary>
        /// Sets the python instance
        /// </summary>
        /// <param name="instance">The underlying python instance</param>
        public virtual void SetPythonInstance(PyObject instance)
        {
            _instance = instance;
            _pythonMethods.Clear();
            _pythonPropertyNames.Clear();
        }

        /// <summary>
        /// Gets the Python instance property with the specified name
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        public T GetProperty<T>(string propertyName)
        {
            using var _ = Py.GIL();


            return _instance.GetAttr(GetPropertyName(propertyName)).GetAndDispose<T>();
        }

        /// <summary>
        /// Sets the Python instance property with the specified name
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="value">The property value</param>
        public void SetProperty(string propertyName, object value)
        {
            using var _ = Py.GIL();
            _instance.SetAttr(GetPropertyName(propertyName), value.ToPython());
        }

        /// <summary>
        /// Gets the Python instance property with the specified name
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        public PyObject GetProperty(string propertyName)
        {
            using var _ = Py.GIL();
            return _instance.GetAttr(GetPropertyName(propertyName));
        }

        /// <summary>
        /// Determines whether the Python instance has the specified attribute
        /// </summary>
        /// <param name="name">The attribute name</param>
        /// <returns>Whether the Python instance has the specified attribute</returns>
        public bool HasAttr(string name)
        {
            using var _ = Py.GIL();
            return _instance.HasAttr(name) || _instance.HasAttr(name.ToSnakeCase());
        }

        /// <summary>
        /// Gets the Python instances method with the specified name and caches it
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <returns>The matched method</returns>
        public PyObject GetMethod(string methodName)
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
        public T InvokeMethod<T>(string methodName, params object[] args)
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
        protected PyObject InvokeMethod(string methodName, params object[] args)
        {
            using var _ = Py.GIL();
            var method = GetMethod(methodName);
            return method.Invoke(args);
        }

        private string GetPropertyName(string propertyName)
        {
            if (!_pythonPropertyNames.TryGetValue(propertyName, out var pythonPropertyName))
            {
                pythonPropertyName = propertyName.ToSnakeCase();
                if (!_instance.HasAttr(pythonPropertyName))
                {
                    pythonPropertyName = propertyName;
                }
                _pythonPropertyNames[propertyName] = pythonPropertyName;
            }
            return pythonPropertyName;
        }
    }

    /// <summary>
    /// Base class for Python wrapper classes that implement a specific interface
    /// </summary>
    public class BasePythonWrapper<TInterface> : BasePythonWrapper
    {
        /// <inheritdoc/>
        public BasePythonWrapper()
            : base()
        {
        }

        /// <inheritdoc/>
        public BasePythonWrapper(PyObject instance)
            : base()
        {
            SetPythonInstance(instance);
        }

        /// <inheritdoc/>
        public override void SetPythonInstance(PyObject instance)
        {
            base.SetPythonInstance(instance.ValidateImplementationOf<TInterface>());
        }
    }
}
