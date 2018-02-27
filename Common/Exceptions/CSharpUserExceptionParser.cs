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
using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Scheduling;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Parser that converts a regular exception thrown by a C# algorithm into an <see cref="Exception"/>.
    /// </summary>
    public class CSharpUserExceptionParser : IExceptionParser
    {
        private static readonly Dictionary<string, string> _commonErrors = new Dictionary<string, string>
        {
            { "KeyNotFoundException", "Trying to retrieve an element from a collection using a key that does not exist in that collection throws a KeyNotFoundException exception. To prevent the exception, ensure that the key exist in the collection and/or that collection is not empty."},
            { "DivideByZeroException", "Trying to divide an integer or Decimal number by zero throws a DivideByZeroException exception. To prevent the exception, ensure that the denominator in a division operation with integer or Decimal values is non-zero." },
        };

        /// <summary>
        /// Parses an <see cref="Exception"/> object into an new <see cref="Exception"/> with a legible message.
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> object to parse.</param>
        /// <returns>Parsed exception</returns>
        public Exception Parse(Exception exception)
        {
            var original = exception;
            if (exception.InnerException != null)
            {
                exception = Parse(exception.InnerException);
            }

            var key = exception.GetType().Name;

            string message;
            if (_commonErrors.TryGetValue(key, out message))
            {
                return new Exception($"{message}{Environment.NewLine}", original);
            }

            message = exception.Message;

            if (original.GetType() == typeof(ScheduledEventException))
            {
                message = $"In one of your Schedule Events, {message.Substring(0, 1).ToLower()}{message.Substring(1)}";
                return new ScheduledEventException(message, original);
            }

            return original;
        }
    }
}