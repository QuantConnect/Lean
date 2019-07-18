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

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides methods for creating new instances of python custom data objects
    /// </summary>
    public class PythonActivator
    {
        /// <summary>
        /// <see cref="System.Type"/> of the object we wish to create
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Method to return an instance of object
        /// </summary>
        public Func<object[], object> Factory { get;  }

        /// <summary>
        /// Creates a new instance of <see cref="PythonActivator"/>
        /// </summary>
        /// <param name="type"><see cref="System.Type"/> of the object we wish to create</param>
        /// <param name="value"><see cref="PyObject"/> that contains the python type</param>
        public PythonActivator(Type type, PyObject value)
        {
            Type = type;

            var isPythonQuandl = false;

            using (Py.GIL())
            {
                var pythonType = value.Invoke().GetPythonType();
                isPythonQuandl = pythonType.As<Type>() == typeof(PythonQuandl);
                pythonType.Dispose();
            }

            if (isPythonQuandl)
            {
                Factory = x =>
                {
                    using (Py.GIL())
                    {
                        var instance = value.Invoke();
                        var pyValueColumnName = instance.GetAttr("ValueColumnName");
                        var valueColumnName = pyValueColumnName.ToString();
                        instance.Dispose();
                        pyValueColumnName.Dispose();
                        return new PythonQuandl(valueColumnName);
                    }
                };
            }
            else
            {
                Factory = x =>
                {
                    using (Py.GIL())
                    {
                        var instance = value.Invoke();
                        return new PythonData(instance);
                    }
                };
            };
        }
    }
}