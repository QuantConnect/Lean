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
using QuantConnect.Securities.Option;

namespace QuantConnect.Python
{
    /// <summary>
    /// Python wrapper for custom option assignment models
    /// </summary>
    public class OptionAssignmentModelPythonWrapper : BasePythonWrapper<IOptionAssignmentModel>, IOptionAssignmentModel
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="model">The python model to wrapp</param>
        public OptionAssignmentModelPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Get's the option assignments to generate if any
        /// </summary>
        /// <param name="parameters">The option assignment parameters data transfer class</param>
        /// <returns>The option assignment result</returns>
        public OptionAssignmentResult GetAssignment(OptionAssignmentParameters parameters)
        {
            return InvokeMethod<OptionAssignmentResult>(nameof(GetAssignment), parameters);
        }
    }
}
