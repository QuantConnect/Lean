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
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Interprets <see cref="KeyErrorPythonExceptionInterpreter"/> instances
    /// </summary>
    public class KeyErrorPythonExceptionInterpreter : IExceptionInterpreter
    {
        /// <summary>
        /// Determines the order that an instance of this class should be called
        /// </summary>
        public int Order => 0;

        /// <summary>
        /// Determines if this interpreter should be applied to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be interpreted, false otherwise</returns>
        public bool CanInterpret(Exception exception)
        {
            return 
                exception?.GetType() == typeof(PythonException) &&
                exception.Message.Contains("KeyError");
        }
        /// <summary>
        /// Interprets the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be interpreted</param>
        /// <param name="innerInterpreter">An interpreter that should be applied to the inner exception.</param>
        /// <returns>The interpreted exception</returns>
        public Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            var pe = (PythonException)exception;

            var key = string.Empty;
            if (pe.Message.Contains("["))
            {
                key = pe.Message.GetStringBetweenChars('[', ']');
            }
            else if (pe.Message.Contains("\'"))
            {
                key = pe.Message.GetStringBetweenChars('\'', '\'');
            }
            var message = $"Trying to retrieve an element from a collection using a key that does not exist in that collection throws a KeyError exception. To prevent the exception, ensure that the {key} key exist in the collection and/or that collection is not empty.";

            message += PythonUtil.PythonExceptionStackParser(pe.StackTrace);

            return new KeyNotFoundException(message, pe);
        }
    }
}