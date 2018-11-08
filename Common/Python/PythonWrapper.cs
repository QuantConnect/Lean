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
        /// <typeparam name="TInterface">The inteface type</typeparam>
        /// <param name="model">The model implementing the interface type</param>
        public static void ValidateImplementationOf<TInterface>(this PyObject model)
        {
            if (!typeof(TInterface).IsInterface)
            {
                throw new ArgumentException($"{nameof(PythonWrapper)}.{nameof(ValidateImplementationOf)} expected an interface type parameter.");
            }

            var missingMembers = new List<string>();
            var members = typeof(TInterface).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            using (Py.GIL())
            {
                foreach (var member in members)
                {
                    if (!model.HasAttr(member.Name))
                    {
                        missingMembers.Add(member.Name);
                    }
                }
            }
            if (missingMembers.Any())
            {
                throw new NotImplementedException($"{nameof(TInterface)} must be fully implemented. Missing implementations: {string.Join(", ", missingMembers)}");
            }
        }
    }
}