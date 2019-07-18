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
using QuantConnect.Brokerages;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides a wrapper for <see cref="IBrokerageMessageHandler"/> implementations written in python
    /// </summary>
    public class BrokerageMessageHandlerPythonWrapper : IBrokerageMessageHandler
    {
        private readonly dynamic _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageMessageHandlerPythonWrapper"/> class
        /// </summary>
        /// <param name="model">The python implementation of <see cref="IBrokerageMessageHandler"/></param>
        public BrokerageMessageHandlerPythonWrapper(PyObject model)
        {
            model.ValidateImplementationOf<IBrokerageMessageHandler>();

            _model = model;
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message">The message to be handled</param>
        public void Handle(BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                _model.Handle(message);
            }
        }
    }
}
