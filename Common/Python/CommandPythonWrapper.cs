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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Commands;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Python wrapper for a python defined command type
    /// </summary>
    public class CommandPythonWrapper : BasePythonWrapper<Command>
    {
        /// <summary>
        /// Constructor for initialising the <see cref="CommandPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="type">Python command type</param>
        /// <param name="data">Command data</param>
        public CommandPythonWrapper(PyObject type, string data = null)
            : base()
        {
            using var _ = Py.GIL();

            var instance = type.Invoke();

            SetPythonInstance(instance);
            if (data != null)
            {
                foreach (var kvp in JsonConvert.DeserializeObject<Dictionary<string, object>>(data))
                {
                    if (kvp.Value is JArray jArray)
                    {
                        SetProperty(kvp.Key, jArray.ToObject<List<object>>());
                    }
                    else if (kvp.Value is JObject jobject)
                    {
                        SetProperty(kvp.Key, jobject.ToObject<Dictionary<string, object>>());
                    }
                    else
                    {
                        SetProperty(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Run this command using the target algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>True if success, false otherwise. Returning null will disable command feedback</returns>
        public bool? Run(IAlgorithm algorithm)
        {
            var result = InvokeMethod(nameof(Run), algorithm);
            return result.GetAndDispose<bool?>();
        }
    }
}
