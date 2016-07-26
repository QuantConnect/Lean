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

using System;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents an error retuned from a broker's server
    /// </summary>
    public class BrokerageException : Exception
    {
        /// <summary>
        /// Creates a new BrokerageException with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public BrokerageException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new BrokerageException with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public BrokerageException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
