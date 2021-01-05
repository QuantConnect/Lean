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
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Wraps a <see cref="PyObject"/> object that represents a model that simulates market order slippage
    /// </summary>
    public class SlippageModelPythonWrapper : ISlippageModel
    {
        private readonly dynamic _model;

        /// <summary>
        /// Constructor for initialising the <see cref="SlippageModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Represents a model that simulates market order slippage</param>
        public SlippageModelPythonWrapper(PyObject model)
        {
            _model = model;
        }

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        /// <param name="asset">The security matching the order</param>
        /// <param name="order">The order to compute slippage for</param>
        /// <returns>The slippage of the order in units of the account currency</returns>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            using (Py.GIL())
            {
                return (_model.GetSlippageApproximation(asset, order) as PyObject).GetAndDispose<decimal>();
            }
        }
    }
}