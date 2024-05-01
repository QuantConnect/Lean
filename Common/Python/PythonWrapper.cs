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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Python.Runtime;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides extension methods for managing python wrapper classes
    /// </summary>
    public static class PythonWrapper
    {
        /// <summary>
        /// Validates that the specified <see cref="PyObject"/> completely implements the provided interface type
        /// </summary>
        /// <typeparam name="TInterface">The interface type</typeparam>
        /// <param name="model">The model implementing the interface type</param>
        public static PyObject ValidateImplementationOf<TInterface>(this PyObject model)
        {
            if (!typeof(TInterface).IsInterface)
            {
                throw new ArgumentException(
                    $"{nameof(PythonWrapper)}.{nameof(ValidateImplementationOf)}(): {Messages.PythonWrapper.ExpectedInterfaceTypeParameter}");
            }

            var missingMembers = new List<string>();
            var members = typeof(TInterface).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            using (Py.GIL())
            {
                foreach (var member in members)
                {
                    if ((member is not MethodInfo method || !method.IsSpecialName) &&
                        !model.HasAttr(member.Name) && !model.HasAttr(member.Name.ToSnakeCase()))
                    {
                        missingMembers.Add(member.Name);
                    }
                }

                if (missingMembers.Any())
                {
                    throw new NotImplementedException(
                        Messages.PythonWrapper.InterfaceNotFullyImplemented(typeof(TInterface).Name, model.GetPythonType().Name, missingMembers));
                }
            }

            return model;
        }

        /// <summary>
        /// Invokes the specified method on the provided <see cref="PyObject"/> instance with the specified arguments
        /// </summary>
        /// <param name="model">The <see cref="PyObject"/> instance</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The return value of the called method converted into the <typeparamref name="T"/> type</returns>
        public static T InvokeMethod<T>(this PyObject model, string methodName, params object[] args)
        {
            using var _ = Py.GIL();
            return InvokeMethodImpl(model, methodName, args).GetAndDispose<T>();
        }

        /// <summary>
        /// Invokes the specified method on the provided <see cref="PyObject"/> instance with the specified arguments
        /// </summary>
        /// <param name="model">The <see cref="PyObject"/> instance</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="args">The arguments to call the method with</param>
        public static void InvokeMethod(this PyObject model, string methodName, params object[] args)
        {
            InvokeMethodImpl(model, methodName, args);
        }

        /// <summary>
        /// Invokes the given <see cref="PyObject"/> method with the specified arguments
        /// </summary>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The arguments to call the method with</param>
        /// <returns>The return value of the called method converted into the <typeparamref name="T"/> type</returns>
        public static T Invoke<T>(this PyObject method, params object[] args)
        {
            using var _ = Py.GIL();
            return InvokeMethodImpl(method, args).GetAndDispose<T>();
        }

        /// <summary>
        /// Invokes the given <see cref="PyObject"/> method with the specified arguments
        /// </summary>
        /// <param name="method">The method to invoke</param>
        /// <param name="args">The arguments to call the method with</param>
        public static PyObject Invoke(this PyObject method, params object[] args)
        {
            return InvokeMethodImpl(method, args);
        }

        private static PyObject InvokeMethodImpl(PyObject model, string methodName, params object[] args)
        {
            using var _ = Py.GIL();
            PyObject method = model.GetMethod(methodName);
            return InvokeMethodImpl(method, args);
        }

        private static PyObject InvokeMethodImpl(PyObject method, params object[] args)
        {
            using var _ = Py.GIL();
            return method.Invoke(args.Select(arg => arg.ToPython()).ToArray());
        }
    }
}
