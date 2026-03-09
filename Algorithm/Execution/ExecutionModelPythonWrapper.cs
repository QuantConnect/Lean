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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Python;
using System;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Provides an implementation of <see cref="IExecutionModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class ExecutionModelPythonWrapper : ExecutionModel
    {
        private readonly bool _onOrderEventsDefined;

        /// <summary>
        /// Constructor for initialising the <see cref="IExecutionModel"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Model defining how to execute trades to reach a portfolio target</param>
        public ExecutionModelPythonWrapper(PyObject model)
        {
            SetPythonInstance(model, false);
            foreach (var attributeName in new[] { "Execute", "OnSecuritiesChanged" })
            {
                if (!HasAttr(attributeName))
                {
                    throw new NotImplementedException($"IExecutionModel.{attributeName} must be implemented. Please implement this missing method on {model.GetPythonType()}");
                }
            }

            _onOrderEventsDefined = HasAttr("OnOrderEvent");

            var methodName = nameof(SetPythonInstance);
            if (HasAttr(methodName))
            {
                InvokeMethod(methodName, model);
            }
        }

        /// <summary>
        /// Submit orders for the specified portfolio targets.
        /// This model is free to delay or spread out these orders as it sees fit
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            InvokeMethod(nameof(Execute), algorithm, targets).Dispose();
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            InvokeMethod(nameof(OnSecuritiesChanged), algorithm, changes).Dispose();
        }

        /// <summary>
        /// New order event handler
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="orderEvent">Order event to process</param>
        public override void OnOrderEvent(QCAlgorithm algorithm, OrderEvent orderEvent)
        {
            if (_onOrderEventsDefined)
            {
                InvokeMethod(nameof(OnOrderEvent), algorithm, orderEvent).Dispose();
            }
        }
    }
}
