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

using Python.Runtime;
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides an implementation of <see cref="ISettlementModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class SettlementModelPythonWrapper : BasePythonWrapper<ISettlementModel>, ISettlementModel
    {
        /// Constructor for initialising the <see cref="SettlementModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Settlement Python Model</param>
        public SettlementModelPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Applies cash settlement rules using the method defined in the Python class
        /// </summary>
        /// <param name="applyFundsParameters">The funds application parameters</param>
        public void ApplyFunds(ApplyFundsSettlementModelParameters applyFundsParameters)
        {
            InvokeMethod(nameof(ApplyFunds), applyFundsParameters);
        }

        /// <summary>
        /// Scan for pending settlements using the method defined in the Python class
        /// </summary>
        /// <param name="settlementParameters">The settlement parameters</param>
        public void Scan(ScanSettlementModelParameters settlementParameters)
        {
            InvokeMethod(nameof(Scan), settlementParameters);
        }

        /// <summary>
        /// Gets the unsettled cash amount for the security
        /// </summary>
        public CashAmount GetUnsettledCash()
        {
            using (Py.GIL())
            {
                var result = InvokeMethod<CashAmount?>(nameof(GetUnsettledCash));
                if (result == null)
                {
                    return default;
                }

                return result.Value;
            }
        }
    }
}
