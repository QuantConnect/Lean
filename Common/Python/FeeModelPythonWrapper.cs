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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides an order fee model that wraps a <see cref="PyObject"/> object that represents a model that simulates order fees
    /// </summary>
    public class FeeModelPythonWrapper : FeeModel
    {
        private readonly BasePythonWrapper<FeeModel> _model;
        private bool _extendedVersion = true;

        /// <summary>
        /// Constructor for initialising the <see cref="FeeModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Represents a model that simulates order fees</param>
        public FeeModelPythonWrapper(PyObject model)
        {
            _model = new BasePythonWrapper<FeeModel>(model, false);
        }

        /// <summary>
        /// Get the fee for this order
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            using (Py.GIL())
            {
                if (_extendedVersion)
                {
                    try
                    {
                        return _model.InvokeMethod<OrderFee>(nameof(GetOrderFee), parameters);
                    }
                    catch (PythonException)
                    {
                        _extendedVersion = false;
                    }
                }
                var fee =  _model.InvokeMethod<decimal>(nameof(GetOrderFee), parameters.Security, parameters.Order);
                return new OrderFee(new CashAmount(fee, "USD"));
            }
        }
    }
}
