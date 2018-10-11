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
using QuantConnect.Interfaces;

namespace QuantConnect
{
    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.ErrorMessage"/> event
    /// </summary>
    public sealed class ErrorMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error stack trace
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorMessageEventArgs"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="stackTrace">The error stack trace</param>
        public ErrorMessageEventArgs(string message, string stackTrace = "")
        {
            Message = message;
            StackTrace = stackTrace;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.DebugMessage"/> event
    /// </summary>
    public sealed class DebugMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugMessageEventArgs"/> class
        /// </summary>
        /// <param name="message">The debug message</param>
        public DebugMessageEventArgs(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.RuntimeError"/> event
    /// </summary>
    public sealed class RuntimeErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error stack trace
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeErrorEventArgs"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="stackTrace">The error stack trace</param>
        public RuntimeErrorEventArgs(string message, string stackTrace = "")
        {
            Message = message;
            StackTrace = stackTrace;
        }
    }
}
