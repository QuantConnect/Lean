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

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Base handler that will try get an exception file and line
    /// </summary>
    public class SystemExceptionInterpreter : IExceptionInterpreter
    {
        private static Regex FileAndLineRegex = new ("(\\w+.cs:line \\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Determines the order that an instance of this class should be called
        /// </summary>
        public virtual int Order => int.MaxValue;

        /// <summary>
        /// Determines if this interpreter should be applied to the specified exception. f
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be interpreted, false otherwise</returns>
        public virtual bool CanInterpret(Exception exception) => true;

        /// <summary>
        /// Interprets the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be interpreted</param>
        /// <param name="innerInterpreter">An interpreter that should be applied to the inner exception.</param>
        /// <returns>The interpreted exception</returns>
        public virtual Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            var sanitized = new SanitizedException(exception.Message, exception.StackTrace);

            if (!TryGetLineAndFile(exception.StackTrace, out var fileAndLine))
            {
                return sanitized;
            }
            return new Exception(exception.Message + fileAndLine, innerException: sanitized);
        }

        /// <summary>
        /// Helper method to get the file and line from a C# stacktrace
        /// </summary>
        public static bool TryGetLineAndFile(string stackTrace, out string fileAndLine)
        {
            fileAndLine = null;
            if (stackTrace != null)
            {
                var match = FileAndLineRegex.Match(stackTrace);
                if (match.Success)
                {
                    foreach (Match lineCapture in match.Captures)
                    {
                        fileAndLine = $" in {lineCapture.Groups[1].Value}" ;
                        return true;
                    }
                }
            }
            return false;
        }

        private class SanitizedException : Exception
        {
            private readonly string _message;
            private readonly string _stackTrace;

            public override string Message => _message;
            public override string StackTrace => _stackTrace;

            public SanitizedException(string message, string stackTrace)
            {
                _message = message;
                _stackTrace = Extensions.ClearLeanPaths(stackTrace);
            }
        }
    }
}
