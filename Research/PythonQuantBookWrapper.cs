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
using QuantConnect.AlgorithmFactory.Python.Wrappers;

namespace QuantConnect.Research
{
    public class PythonQuantBookWrapper : QuantBook
    {
        /// <summary>
        /// Python Wrapper for QuantBook, allows user to use custom Python QuantBook as algorithm
        /// Used by PythonQuantBook
        /// </summary>
        public PythonQuantBookWrapper() : base(false) {}

        /// <summary>
        /// For setting a Python QuantBook algorithm, used by subclass PythonQuantBook
        /// </summary>
        /// <param name="obj"></param>
        protected void Setup(PyObject obj)
        {
            Algorithm = new AlgorithmPythonWrapper(obj);
            base.Setup();
        }
    }
}
