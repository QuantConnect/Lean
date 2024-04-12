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
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Wraps a <see cref="PyObject"/> object that represents a type capable of initializing a new security
    /// </summary>
    public class SecurityInitializerPythonWrapper : BasePythonWrapper<ISecurityInitializer>, ISecurityInitializer
    {
        /// <summary>
        /// Constructor for initialising the <see cref="SecurityInitializerPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Represents a type capable of initializing a new security</param>
        public SecurityInitializerPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Initializes the specified security
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        public void Initialize(Security security)
        {
            InvokeMethod(nameof(Initialize), security);
        }
    }
}
