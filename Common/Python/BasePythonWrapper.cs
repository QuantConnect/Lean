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
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Base class for Python wrapper classes
    /// </summary>
    public class BasePythonWrapper<TInterface> : IEquatable<BasePythonWrapper<TInterface>>, IDisposable
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
            return PythonRuntimeChecker.ConvertAndDispose<T>(GetProperty(propertyName), propertyName, isMethod: false);
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
            var method = GetMethod(methodName);
            return PythonRuntimeChecker.InvokeMethod<T>(method, methodName, args);
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

        /// <summary>
        /// Invokes the specified method with the specified arguments without returning a value
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="args">The arguments to call the method with</param>
        public void InvokeVoidMethod(string methodName, params object[] args)
        {
            InvokeMethod(methodName, args).Dispose();
        }

        /// <summary>
        /// Invokes the specified method with the specified arguments and iterates over the returned values
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The returned valued converted to the given type</returns>
        public IEnumerable<T> InvokeMethodAndEnumerate<T>(string methodName, params object[] args)
        {
            var method = GetMethod(methodName);
            return PythonRuntimeChecker.InvokeMethodAndEnumerate<T>(method, methodName, args);
        }

        /// <summary>
        /// Invokes the specified method with the specified arguments and iterates over the returned values
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The returned valued converted to the given type</returns>
        public Dictionary<TKey, TValue> InvokeMethodAndGetDictionary<TKey, TValue>(string methodName, params object[] args)
        {
            var method = GetMethod(methodName);
            return PythonRuntimeChecker.InvokeMethodAndGetDictionary<TKey, TValue>(method, methodName, args);
        }

        /// <summary>
        /// Invokes the specified method with the specified arguments and out parameters
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="outParametersTypes">The types of the out parameters</param>
        /// <param name="outParameters">The out parameters values</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The returned valued converted to the given type</returns>
        public T InvokeMethodWithOutParameters<T>(string methodName, Type[] outParametersTypes, out object[] outParameters, params object[] args)
        {
            var method = GetMethod(methodName);
            return PythonRuntimeChecker.InvokeMethodAndGetOutParameters<T>(method, methodName, outParametersTypes, out outParameters, args);
        }

        /// <summary>
        /// Invokes the specified method with the specified arguments and wraps the result
        /// by calling the given function if the result is not a C# object
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <param name="wrapResult">Method that wraps a Python object in the corresponding Python Wrapper</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The returned value wrapped using the given method if the result is not a C# object</returns>
        public T InvokeMethodAndWrapResult<T>(string methodName, Func<PyObject, T> wrapResult, params object[] args)
        {
            var method = GetMethod(methodName);
            return PythonRuntimeChecker.InvokeMethodAndWrapResult(method, methodName, wrapResult, args);
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

        /// <summary>
        /// Dispose of this instance
        /// </summary>
        public virtual void Dispose()
        {
            using var _ = Py.GIL();
            if (_pythonMethods != null)
            {
                foreach (var methods in _pythonMethods.Values)
                {
                    methods.Dispose();
                }
                _pythonMethods.Clear();
            }
            _instance?.Dispose();
        }

        /// <summary>
        /// Set of helper methods to invoke Python methods with runtime checks for return values and out parameter's conversions.
        /// </summary>
        public class PythonRuntimeChecker
        {
            /// <summary>
            /// Invokes method <paramref name="method"/> and converts the returned value to type <typeparamref name="TResult"/>
            /// </summary>
            public static TResult InvokeMethod<TResult>(PyObject method, string pythonMethodName, params object[] args)
            {
                using var _ = Py.GIL();
                using var result = method.Invoke(args);

                return Convert<TResult>(result, pythonMethodName);
            }

            /// <summary>
            /// Invokes method <paramref name="method"/>, expecting an enumerable or generator as return value,
            /// converting each item to type <typeparamref name="TItem"/> on demand.
            /// </summary>
            public static IEnumerable<TItem> InvokeMethodAndEnumerate<TItem>(PyObject method, string pythonMethodName, params object[] args)
            {
                using var _ = Py.GIL();
                var result = method.Invoke(args);

                foreach (var item in EnumerateAndDisposeItems<TItem>(result, pythonMethodName))
                {
                    yield return item;
                }

                result.Dispose();
            }

            /// <summary>
            /// Invokes method <paramref name="method"/>, expecting a dictionary as return value,
            /// which then will be converted to a managed dictionary, with type checking on each item conversion.
            /// </summary>
            public static Dictionary<TKey, TValue> InvokeMethodAndGetDictionary<TKey, TValue>(PyObject method, string pythonMethodName, params object[] args)
            {
                using var _ = Py.GIL();
                using var result = method.Invoke(args);

                Dictionary<TKey, TValue> dict;
                if (result.TryConvert(out dict))
                {
                    // this is required if the python implementation is actually returning a C# dict, not common,
                    // but could happen if its actually calling a base C# implementation
                    return dict;
                }

                dict = new();
                Func<PyObject, string> keyErrorMessageFunc =
                    (pyItem) => Messages.BasePythonWrapper.InvalidDictionaryKeyType(pythonMethodName, typeof(TKey), pyItem.GetPythonType());
                foreach (var (managedKey, pyKey) in Enumerate<TKey>(result, pythonMethodName, keyErrorMessageFunc))
                {
                    var pyValue = result.GetItem(pyKey);
                    try
                    {
                        dict[managedKey] = pyValue.GetAndDispose<TValue>();
                    }
                    catch (InvalidCastException ex)
                    {
                        throw new InvalidCastException(
                            Messages.BasePythonWrapper.InvalidDictionaryValueType(pythonMethodName, typeof(TValue), pyValue.GetPythonType()),
                            ex);
                    }
                }

                return dict;
            }

            /// <summary>
            /// Invokes method <paramref name="method"/> and tries to convert the returned value to type <typeparamref name="TResult"/>.
            /// If conversion is not possible, the returned PyObject is passed to the provided <paramref name="wrapResult"/> method,
            /// which should try to do the proper conversion, wrapping or handling of the PyObject.
            /// </summary>
            public static TResult InvokeMethodAndWrapResult<TResult>(PyObject method, string pythonMethodName, Func<PyObject, TResult> wrapResult,
                params object[] args)
            {
                using var _ = Py.GIL();
                var result = method.Invoke(args);

                if (!result.TryConvert<TResult>(out var managedResult))
                {
                    return wrapResult(result);
                }

                result.Dispose();
                return managedResult;
            }

            /// <summary>
            /// Invokes method <paramref name="method"/> and converts the returned value to type <typeparamref name="TResult"/>.
            /// It also makes sure the Python method returns values for the out parameters, converting them into the expected types
            /// in <paramref name="outParametersTypes"/> and placing them in the <paramref name="outParameters"/> array.
            /// </summary>
            public static TResult InvokeMethodAndGetOutParameters<TResult>(PyObject method, string pythonMethodName, Type[] outParametersTypes,
                out object[] outParameters, params object[] args)
            {
                using var _ = Py.GIL();
                using var result = method.Invoke(args);

                // Since pythonnet does not support out parameters, the methods return
                // a tuple where the out parameter come after the other returned values
                if (!PyTuple.IsTupleType(result))
                {
                    throw new ArgumentException(
                        Messages.BasePythonWrapper.InvalidReturnTypeForMethodWithOutParameters(pythonMethodName, result.GetPythonType()));
                }

                if (result.Length() < outParametersTypes.Length + 1)
                {
                    throw new ArgumentException(Messages.BasePythonWrapper.InvalidReturnTypeTupleSizeForMethodWithOutParameters(
                        pythonMethodName, outParametersTypes.Length + 1, result.Length()));
                }

                var managedResult = Convert<TResult>(result[0], pythonMethodName);

                outParameters = new object[outParametersTypes.Length];
                var i = 0;
                try
                {
                    for (; i < outParametersTypes.Length; i++)
                    {
                        outParameters[i] = result[i + 1].AsManagedObject(outParametersTypes[i]);
                    }
                }
                catch (InvalidCastException exception)
                {
                    throw new InvalidCastException(
                        Messages.BasePythonWrapper.InvalidOutParameterType(pythonMethodName, i, outParametersTypes[i], result[i + 1].GetPythonType()),
                        exception);
                }

                return managedResult;
            }

            /// <summary>
            /// Converts the given PyObject into the provided <typeparamref name="T"/> type,
            /// generating an exception with a user-friendly message if conversion is not possible.
            /// </summary>
            public static T Convert<T>(PyObject pyObject, string pythonName, bool isMethod = true)
            {
                var type = typeof(T);
                try
                {
                    if (type == typeof(void))
                    {
                        return default;
                    }

                    if (type == typeof(PyObject))
                    {
                        return (T)(object)pyObject;
                    }

                    return (T)pyObject.AsManagedObject(type);
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException(Messages.BasePythonWrapper.InvalidReturnType(pythonName, type, pyObject.GetPythonType(), isMethod), e);
                }
            }

            /// <summary>
            /// Converts the given PyObject into the provided <typeparamref name="T"/> type,
            /// generating an exception with a user-friendly message if conversion is not possible.
            /// It will dispose of the source PyObject.
            /// </summary>
            public static T ConvertAndDispose<T>(PyObject pyObject, string pythonName, bool isMethod = true)
            {
                try
                {
                    return Convert<T>(pyObject, pythonName, isMethod);
                }
                finally
                {
                    pyObject.Dispose();
                }
            }

            /// <summary>
            /// Verifies that the <paramref name="result"/> value is iterable and converts each item into the <typeparamref name="TItem"/> type,
            /// returning also the corresponding source PyObject for each one of them.
            /// </summary>
            private static IEnumerable<(TItem, PyObject)> Enumerate<TItem>(PyObject result, string pythonMethodName,
                Func<PyObject, string> getInvalidCastExceptionMessage = null)
            {
                if (!result.IsIterable())
                {
                    throw new InvalidCastException(Messages.BasePythonWrapper.InvalidIterable(pythonMethodName, typeof(TItem), result.GetPythonType()));
                }

                using var iterator = result.GetIterator();
                foreach (PyObject item in iterator)
                {
                    TItem managedItem;

                    try
                    {
                        managedItem = item.As<TItem>();
                    }
                    catch (InvalidCastException ex)
                    {
                        var message = getInvalidCastExceptionMessage?.Invoke(item) ??
                            Messages.BasePythonWrapper.InvalidMethodIterableItemType(pythonMethodName, typeof(TItem), item.GetPythonType());
                        throw new InvalidCastException(message, ex);
                    }

                    yield return (managedItem, item);
                }
            }

            /// <summary>
            /// Verifies that the <paramref name="result"/> value is iterable and converts each item into the <typeparamref name="TItem"/> type.
            /// </summary>
            private static IEnumerable<TItem> EnumerateAndDisposeItems<TItem>(PyObject result, string pythonMethodName)
            {
                foreach (var (managedItem, pyItem) in Enumerate<TItem>(result, pythonMethodName))
                {
                    pyItem.Dispose();
                    yield return managedItem;
                }
            }
        }
    }
}
