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
using Krs.Ats.IBNet;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Represents an error returnd from the Interactive Broker's server
    /// </summary>
    public class InteractiveBrokersException : BrokerageException
    {
        /// <summary>
        /// The error code returned from the Interactive Brokers server
        /// </summary>
        public ErrorMessage Error { get; private set; }

        /// <summary>
        /// The order ID or the ticker ID that generated the error
        /// </summary>
        public int TickerID { get; private set; }

        /// <summary>
        /// Creates a new InteractiveBrokersException with the specified error and message
        /// </summary>
        /// <param name="error">The error code returned from the Interactive Broker's server</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InteractiveBrokersException(ErrorMessage error, string message)
            : base(message)
        {
            Error = error;
        }

        /// <summary>
        /// Creates a new InteractiveBrokersException with the specified error and message
        /// </summary>
        /// <param name="error">The error code returned from the Interactive Broker's server</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public InteractiveBrokersException(ErrorMessage error, string message, Exception inner)
            : base(message, inner)
        {
            Error = error;
        }

        /// <summary>
        /// Creates a new InteractiveBrokersException with the specified error and message
        /// </summary>
        /// <param name="error">The error code returned from the Interactive Broker's server</param>
        /// <param name="tickerID">The order ID or the ticker ID that generated the error, or zero if the error is not associated with any order or ticker</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InteractiveBrokersException(ErrorMessage error, int tickerID, string message)
            : base(message)
        {
            Error = error;
            TickerID = tickerID;
        }

        /// <summary>
        /// Creates a new InteractiveBrokersException with the specified error and message
        /// </summary>
        /// <param name="error">The error code returned from the Interactive Broker's server</param>
        /// <param name="tickerID">The order ID or the ticker ID that generated the error, or zero if the error is not associated with any order or ticker</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public InteractiveBrokersException(ErrorMessage error, int tickerID, string message, Exception inner)
            : base(message, inner)
        {
            Error = error;
            TickerID = tickerID;
        }
    }
}
