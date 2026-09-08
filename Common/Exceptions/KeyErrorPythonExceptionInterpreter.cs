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
using Python.Runtime;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Interprets <see cref="KeyErrorPythonExceptionInterpreter"/> instances
    /// </summary>
    public class KeyErrorPythonExceptionInterpreter : PythonExceptionInterpreter
    {
        /// <summary>
        /// Determines the order that an instance of this class should be called
        /// </summary>
        public override int Order => 0;

        /// <summary>
        /// Determines if this interpreter should be applied to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be interpreted, false otherwise</returns>
        public override bool CanInterpret(Exception exception)
        {
            var pythonException = exception as PythonException;
            if (pythonException == null)
            {
                return false;
            }

            using (Py.GIL())
            {
                return base.CanInterpret(exception) &&
                    pythonException.Type.Name.Contains("KeyError", StringComparison.InvariantCultureIgnoreCase);
            }
        }
        /// <summary>
        /// Interprets the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be interpreted</param>
        /// <param name="innerInterpreter">An interpreter that should be applied to the inner exception.</param>
        /// <returns>The interpreted exception</returns>
        public override Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            var pe = (PythonException)exception;

            // KeyError's message is the repr() of the missing key, so string keys come through quoted ("'SPY'")
            // but object keys (e.g. a Symbol used on a plain dict) come through as
            // "<QuantConnect.Symbol object at 0x...>", which the quote/bracket parsing below can't handle and
            // used to render a blank key in the final message ("ensure that the  key exist in the collection").
            // Read the key from the exception value instead, where str() yields a meaningful name in both cases.
            // Depending on whether the error indicator was normalized, the value is either the KeyError instance
            // (the key is args[0]) or the raw args tuple the exception was created with.
            var key = string.Empty;
            using (Py.GIL())
            {
                try
                {
                    using var args = pe.Value != null && pe.Value.HasAttr("args") ? pe.Value.GetAttr("args") : null;
                    var container = args ?? pe.Value;
                    if (container != null && !container.IsNone())
                    {
                        if (PyTuple.IsTupleType(container))
                        {
                            if (container.Length() > 0)
                            {
                                using var firstArg = container[0];
                                key = firstArg.ToString();
                            }
                        }
                        else
                        {
                            key = container.ToString();
                        }
                    }
                }
                catch (PythonException)
                {
                    // best effort, fall back to parsing the message below
                }
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                if (pe.Message.Contains('[', StringComparison.InvariantCulture))
                {
                    key = pe.Message.GetStringBetweenChars('[', ']');
                }
                else if (pe.Message.Contains('\'', StringComparison.InvariantCulture))
                {
                    key = pe.Message.GetStringBetweenChars('\'', '\'');
                }
            }
            var message = Messages.KeyErrorPythonExceptionInterpreter.KeyNotFoundInCollection(key);

            message += PythonUtil.PythonExceptionStackParser(pe.StackTrace);

            return new KeyNotFoundException(message, pe);
        }
    }
}
