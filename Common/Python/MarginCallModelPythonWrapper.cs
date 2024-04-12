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
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides a margin call model that wraps a <see cref="PyObject"/> object that represents the model responsible for picking which orders should be executed during a margin call
    /// </summary>
    public class MarginCallModelPythonWrapper : BasePythonWrapper<IMarginCallModel>, IMarginCallModel
    {
        /// <summary>
        /// Constructor for initialising the <see cref="MarginCallModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Represents the model responsible for picking which orders should be executed during a margin call</param>
        public MarginCallModelPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Executes synchronous orders to bring the account within margin requirements.
        /// </summary>
        /// <param name="generatedMarginCallOrders">These are the margin call orders that were generated
        /// by individual security margin models.</param>
        /// <returns>The list of orders that were actually executed</returns>
        public List<OrderTicket> ExecuteMarginCall(IEnumerable<SubmitOrderRequest> generatedMarginCallOrders)
        {
            using (Py.GIL())
            {
                var marginCalls = InvokeMethod(nameof(ExecuteMarginCall), generatedMarginCallOrders);

                // Since ExecuteMarginCall may return a python list
                // Need to convert to C# list
                var tickets = new List<OrderTicket>();
                using var iterator = marginCalls.GetIterator();
                foreach (PyObject pyObject in iterator)
                {
                    OrderTicket ticket;
                    if (pyObject.TryConvert(out ticket))
                    {
                        tickets.Add(ticket);
                    }
                    pyObject.Dispose();
                }
                iterator.Dispose();
                marginCalls.Dispose();
                return tickets;
            }
        }

        /// <summary>
        /// Scan the portfolio and the updated data for a potential margin call situation which may get the holdings below zero!
        /// If there is a margin call, liquidate the portfolio immediately before the portfolio gets sub zero.
        /// </summary>
        /// <param name="issueMarginCallWarning">Set to true if a warning should be issued to the algorithm</param>
        /// <returns>True for a margin call on the holdings.</returns>
        public List<SubmitOrderRequest> GetMarginCallOrders(out bool issueMarginCallWarning)
        {
            using (Py.GIL())
            {
                var value = InvokeMethod(nameof(GetMarginCallOrders), false);

                // Since pythonnet does not support out parameters, the methods return
                // a tuple where the out parameter comes after the other returned values
                if (!PyTuple.IsTupleType(value))
                {
                    throw new ArgumentException($@"{(Instance as dynamic).__class__.__name__}.GetMarginCallOrders(): {
                        Messages.MarginCallModelPythonWrapper.GetMarginCallOrdersMustReturnTuple}");
                }

                // In this case, the first item holds the list of margin calls
                // and the second the out parameter 'issueMarginCallWarning'
                var marginCallOrders = value[0] as PyObject;
                issueMarginCallWarning = (value[1] as PyObject).GetAndDispose<bool>();

                // Since GetMarginCallOrders may return a python list
                // Need to convert to C# list
                var requests = new List<SubmitOrderRequest>();
                using var iterator = marginCallOrders.GetIterator();
                foreach (PyObject pyObject in iterator)
                {
                    SubmitOrderRequest request;
                    if (pyObject.TryConvert(out request))
                    {
                        requests.Add(request);
                    }
                }
                issueMarginCallWarning |= requests.Count > 0;
                marginCallOrders.Dispose();
                (value as PyObject).Dispose();
                return requests;
            }
        }
    }
}
