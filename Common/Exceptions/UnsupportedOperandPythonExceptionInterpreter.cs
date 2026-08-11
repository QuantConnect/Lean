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
using System.Text.RegularExpressions;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Interprets <see cref="UnsupportedOperandPythonExceptionInterpreter"/> instances
    /// </summary>
    public class UnsupportedOperandPythonExceptionInterpreter : PythonExceptionInterpreter
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
            if (!base.CanInterpret(exception))
            {
                return false;
            }
            return exception.Message.Contains(Messages.UnsupportedOperandPythonExceptionInterpreter.UnsupportedOperandTypeExpectedSubstring) ||
                // "can't compare datetime.datetime to datetime.date", the ordering flavor of mixing datetimes and dates,
                // e.g. 'self.time.date() <= some_stored_datetime'
                (exception.Message.Contains(Messages.UnsupportedOperandPythonExceptionInterpreter.CannotCompareTypesSubstring) &&
                    HasDatetimeAndDateOperands(exception.Message));
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

            string message;
            if (pe.Message.Contains(Messages.UnsupportedOperandPythonExceptionInterpreter.UnsupportedOperandTypeExpectedSubstring))
            {
                var types = pe.Message.Split(':')[1].Trim();
                message = Messages.UnsupportedOperandPythonExceptionInterpreter.InvalidObjectTypesForOperation(types);
            }
            else
            {
                // "can't compare {left} to {right}"
                var match = Regex.Match(pe.Message, @"can't compare (?<left>\S+) to (?<right>\S+)");
                var types = match.Success
                    ? $"'{match.Groups["left"].Value}' and '{match.Groups["right"].Value}'"
                    : "'datetime.datetime' and 'datetime.date'";
                message = Messages.UnsupportedOperandPythonExceptionInterpreter.InvalidObjectTypesForComparison(types);
            }

            if (HasDatetimeAndDateOperands(pe.Message))
            {
                // The single most common shape of this error is expiry math like '(contract.id.date - self.time.date()).days',
                // so point users at the supported alternatives
                message += Messages.UnsupportedOperandPythonExceptionInterpreter.DatetimeAndDateOperandsHint;
            }
            message += PythonUtil.PythonExceptionStackParser(pe.StackTrace);

            return new Exception(message, pe);
        }

        private static bool HasDatetimeAndDateOperands(string message)
        {
            // "datetime.date" is a prefix of "datetime.datetime", so require a word boundary after ".date"
            return Regex.IsMatch(message, @"datetime\.datetime\b") && Regex.IsMatch(message, @"datetime\.date\b");
        }
    }
}
