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
using System;
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Base class for Python wrapper classes
    /// </summary>
    public class BasePythonWrapper<TInterface> : IEquatable<BasePythonWrapper<TInterface>>
    {
        private PyObject _instance;
        private object _underlyingClrObject;
        private Dictionary<string, PyObject> _pythonMethods;
        private Dictionary<string, string> _pythonPropertyNames;

        private readonly bool _validateInterface;

        /// <summary>
        /// Gets the underlying python instance
        /// </summary>
        protected PyObject Instance => _instance;

        /// <summary>
        /// Creates a new instance of the <see cref="BasePythonWrapper{TInterface}" /> class
        /// </summary>
        /// <param name="validateInterface">Whether to perform validations for interface implementation</param>
        public BasePythonWrapper(bool validateInterface = true)
        {
            _pythonMethods = new();
            _pythonPropertyNames = new();
            _validateInterface = validateInterface;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BasePythonWrapper{TInterface}"/> class with the specified instance
        /// </summary>
        /// <param name="instance">The underlying python instance</param>
        /// <param name="validateInterface">Whether to perform validations for interface implementation</param>
        public BasePythonWrapper(PyObject instance, bool validateInterface = true)
            : this(validateInterface)
        {
            SetPythonInstance(instance);
        }

        /// <summary>
        /// Sets the python instance
        /// </summary>
        /// <param name="instance">The underlying python instance</param>
        public void SetPythonInstance(PyObject instance)
        {
            if (_instance != null)
            {
                _pythonMethods.Clear();
                _pythonPropertyNames.Clear();
            }

            _instance = _validateInterface ? instance.ValidateImplementationOf<TInterface>() : instance;
            _instance.TryConvert(out _underlyingClrObject);
        }

        /// <summary>
        /// Gets the Python instance property with the specified name
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        public T GetProperty<T>(string propertyName)
        {
            using var _ = Py.GIL();
            return GetProperty(propertyName).GetAndDispose<T>();
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
        /// Gets the Python instance event with the specified name
        /// </summary>
        /// <param name="name">The name of the event</param>
        public dynamic GetEvent(string name)
        {
            using var _ = Py.GIL();
            return _instance.GetAttr(GetPropertyName(name, true));
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
                method = _instance.GetMethod(methodName);
                _pythonMethods = AddToDictionary(_pythonMethods, methodName, method);
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
        public PyObject InvokeMethod(string methodName, params object[] args)
        {
            using var _ = Py.GIL();
            var method = GetMethod(methodName);
            return method.Invoke(args);
        }

        private string GetPropertyName(string propertyName, bool isEvent = false)
        {
            if (!_pythonPropertyNames.TryGetValue(propertyName, out var pythonPropertyName))
            {
                var snakeCasedPropertyName = propertyName.ToSnakeCase();

                // If the object is actually a C# object (e.g. a child class of a C# class),
                // we check which property was defined in the Python class (if any), either the snake-cased or the original name.
                if (!isEvent && _underlyingClrObject != null)
                {
                    var underlyingClrObjectType = _underlyingClrObject.GetType();
                    var property = underlyingClrObjectType.GetProperty(propertyName);
                    if (property != null)
                    {
                        var clrPropertyValue = property.GetValue(_underlyingClrObject);
                        var pyObjectSnakeCasePropertyValue = _instance.GetAttr(snakeCasedPropertyName);

                        if (!pyObjectSnakeCasePropertyValue.TryConvert(out object pyObjectSnakeCasePropertyClrValue, true) ||
                            !ReferenceEquals(clrPropertyValue, pyObjectSnakeCasePropertyClrValue))
                        {
                            pythonPropertyName = snakeCasedPropertyName;
                        }
                        else
                        {
                            pythonPropertyName = propertyName;
                        }
                    }
                }

                if (pythonPropertyName == null)
                {
                    pythonPropertyName = snakeCasedPropertyName;
                    if (!_instance.HasAttr(pythonPropertyName))
                    {
                        pythonPropertyName = propertyName;
                    }
                }

                _pythonPropertyNames = AddToDictionary(_pythonPropertyNames, propertyName, pythonPropertyName);
            }

            return pythonPropertyName;
        }

        /// <summary>
        /// Adds a key-value pair to the dictionary by copying the original one first and returning a new dictionary
        /// containing the new key-value pair along with the original ones.
        /// We do this in order to avoid the overhead of using locks or concurrent dictionaries and still be thread-safe.
        /// </summary>
        private static Dictionary<string, T> AddToDictionary<T>(Dictionary<string, T> dictionary, string key, T value)
        {
            return new Dictionary<string, T>(dictionary)
            {
                [key] = value
            };
        }

        /// <summary>
        /// Determines whether the specified instance wraps the same Python object reference as this instance,
        /// which would indicate that they are equal.
        /// </summary>
        /// <param name="other">The other object to compare this with</param>
        /// <returns>True if both instances are equal, that is if both wrap the same Python object reference</returns>
        public virtual bool Equals(BasePythonWrapper<TInterface> other)
        {
            return other is not null && (ReferenceEquals(this, other) || Equals(other._instance));
        }

        /// <summary>
        /// Determines whether the specified object is an instance of <see cref="BasePythonWrapper{TInterface}"/>
        /// and wraps the same Python object reference as this instance, which would indicate that they are equal.
        /// </summary>
        /// <param name="obj">The other object to compare this with</param>
        /// <returns>True if both instances are equal, that is if both wrap the same Python object reference</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as PyObject) || Equals(obj as BasePythonWrapper<TInterface>);
        }

        /// <summary>
        /// Gets the hash code for the current instance
        /// </summary>
        /// <returns>The hash code of the current instance</returns>
        public override int GetHashCode()
        {
            using var _ = Py.GIL();
            return PythonReferenceComparer.Instance.GetHashCode(_instance);
        }

        /// <summary>
        /// Determines whether the specified <see cref="PyObject"/> is equal to the current instance's underlying Python object.
        /// </summary>
        private bool Equals(PyObject other)
        {
            if (other is null) return false;
            if (ReferenceEquals(_instance, other)) return true;

            using var _ = Py.GIL();
            // We only care about the Python object reference, not the underlying C# object reference for comparison
            return PythonReferenceComparer.Instance.Equals(_instance, other);
        }
    }
}
