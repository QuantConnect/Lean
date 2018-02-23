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

using QuantConnect.Interfaces;
using System;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Class that represents an <see cref="UserException"/> : exception was created by <see cref="IExceptionParser.Parse(Exception)"/>.
    /// </summary>
    public class UserException : Exception
    {
        string _message;
        string _stacktrace;

        /// <summary>
        /// Creates an instance of <see cref="UserException"/> : exception created by <see cref="IExceptionParser.Parse(Exception)"/>.
        /// </summary>
        /// <param name="message">Message that describes the current exception.</param>
        /// <param name="stacktrace">String representation of the immediate frames on the call stack.</param>
        public UserException(string message, string stacktrace)
        {
            _message = message;
            _stacktrace = stacktrace;
        }

        /// <summary>
        /// Gets the message that describes the current exception.
        /// </summary>
        public override string Message => _message;

        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack.
        /// </summary>
        public override string StackTrace => _stacktrace;

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{_message}{Environment.NewLine}{_stacktrace}";
    }
}