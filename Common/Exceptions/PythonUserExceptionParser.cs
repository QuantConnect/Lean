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
using System.Linq;
using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Scheduling;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Parser that converts a regular exception throw by a python algorithm into a <see cref="UserException"/>.
    /// </summary>
    public class PythonUserExceptionParser : IExceptionParser
    {
        private static readonly Dictionary<string, string> _commonErrors = new Dictionary<string, string>
        {
            { "KeyError", "Trying to retrieve an element from a collection using a key that does not exist in that collection throws a KeyError exception. To prevent the exception, ensure that the key exist in the collection and/or that collection is not empty."},
            { "UnsupportedOperandError", "Trying to perform a summation, subtraction, multiplication or division between a decimal.Decimal and a float throws a TypeError exception. To prevent the exception, ensure that both values share the same type, either decimal.Decimal or float."},
            { "ZeroDivisionError", "Trying to divide an integer or Decimal number by zero throws a DivideByZeroException exception. To prevent the exception, ensure that the denominator in a division operation with integer or Decimal values is non-zero." },
        };

        /// <summary>
        /// Parses an <see cref="PythonException"/> object into an <see cref="UserException"/> one
        /// </summary>
        /// <param name="exception"><see cref="PythonException"/> object to parse into an <see cref="UserException"/> one.</param>
        /// <returns>Parsed exception</returns>
        public Exception Parse(Exception exception)
        {
            var pythonException = exception as PythonException;
            if (pythonException == null)
            {
                pythonException = exception.InnerException as PythonException;
                if (pythonException == null)
                {
                    throw new ArgumentException("The given exception is not valid since it is of type PythonException nor its InnerException is");
                }
            }

            var message = CreateLegibleMessage(pythonException.Message);

            if (exception.GetType() == typeof(ScheduledEventException))
            {
                message = $"In one of your Schedule Events, {message}";
            }

            // Get the place where the error occurred in the PythonException.StackTrace
            var stack = pythonException.StackTrace.Replace("\\\\", "/").Split(new[] { @"\n" }, StringSplitOptions.RemoveEmptyEntries);
            var baseScript = stack[0].Substring(1 + stack[0].IndexOf('\"')).Split('\"')[0];
            var directory = baseScript.Substring(0, baseScript.LastIndexOf('/'));
            baseScript = baseScript.Substring(1 + directory.Length);

            var stacktrace = string.Empty;

            for (var i = 0; i < stack.Length; i += 2)
            {
                if (stack[i].Contains(directory))
                {
                    var index = stack[i].IndexOf(directory) + directory.Length + 1;
                    var info = stack[i].Substring(index).Split(',');

                    var script = info[0].Remove(info[0].Length - 1);
                    var line = int.Parse(info[1].Remove(0, 6));
                    var method = info[2].Replace("in", "at");
                    var statement = stack[i + 1].Trim();

                    // Adds offset to account headers
                    // Yields wrong error line if running Lean locally 
                    line += script == baseScript ? 37 : 0;

                    stacktrace = $"  {method} in {script}:line {line} :: {statement}";
                }
            }

            var lines = exception.ToString()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Contains("QuantConnect")).Skip(1);

            stacktrace = $"{stacktrace}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";

            return new UserException(message, stacktrace);
        }

        private string CreateLegibleMessage(string value)
        {
            var colon = value.IndexOf(':');
            var type = value.Substring(0, colon).Trim();
            var input = value.Substring(1 + colon).Trim();
            var message = value;

            switch (type)
            {
                case "KeyError":
                    var key = GetStringBetweenChar(input, '[', ']');
                    if (key == null)
                    {
                        key = GetStringBetweenChar(input, '\'', '\'');
                    }
                    message = $"{_commonErrors[type]} Key: {key}.";
                    break;
                case "TypeError":
                    if (input.Contains("unsupported operand"))
                    {
                        message = _commonErrors["UnsupportedOperandError"];
                    }
                    break;
                case "ZeroDivisionError":
                    message = _commonErrors[type];
                    break;
                default:
                    break;
            }

            return message;
        }

        private string GetStringBetweenChar(string value, char left, char right)
        {
            var startIndex = 1 + value.IndexOf(left);
            var length = value.IndexOf(right, startIndex) - startIndex;
            if (length > 0)
            {
                return value.Substring(startIndex, length);
            }
            return null;
        }
    }
}