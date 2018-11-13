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

using System.Collections.Generic;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides an implementation of <see cref="IBrokerageMessageHandler"/> that converts specified error codes into warnings
    /// </summary>
    public class DowngradeErrorCodeToWarningBrokerageMessageHandler : IBrokerageMessageHandler
    {
        private readonly HashSet<string> _errorCodesToIgnore;
        private readonly IBrokerageMessageHandler _brokerageMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DowngradeErrorCodeToWarningBrokerageMessageHandler"/> class
        /// </summary>
        /// <param name="brokerageMessageHandler">The brokerage message handler to be wrapped</param>
        /// <param name="errorCodesToIgnore">The error codes to convert to warning messages</param>
        public DowngradeErrorCodeToWarningBrokerageMessageHandler(IBrokerageMessageHandler brokerageMessageHandler, string[] errorCodesToIgnore)
        {
            _brokerageMessageHandler = brokerageMessageHandler;
            _errorCodesToIgnore = errorCodesToIgnore.ToHashSet();
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message">The message to be handled</param>
        public void Handle(BrokerageMessageEvent message)
        {
            if (message.Type == BrokerageMessageType.Error && _errorCodesToIgnore.Contains(message.Code))
            {
                // rewrite the ignored message as a warning message
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, message.Code, message.Message);
            }

            _brokerageMessageHandler.Handle(message);
        }
    }
}