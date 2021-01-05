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
using QuantConnect.Util;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Interprets <see cref="DllNotFoundPythonExceptionInterpreter"/> instances
    /// </summary>
    public class DllNotFoundPythonExceptionInterpreter : IExceptionInterpreter
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
                exception?.GetType() == typeof(DllNotFoundException) &&
                exception.Message.Contains("python");
        }
        /// <summary>
        /// Interprets the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be interpreted</param>
        /// <param name="innerInterpreter">An interpreter that should be applied to the inner exception.</param>
        /// <returns>The interpreted exception</returns>
        public Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            var dnfe = (DllNotFoundException)exception;

            var startIndex = dnfe.Message.IndexOfInvariant("python");
            var length = Math.Min(dnfe.Message.Length - startIndex, 10);
            var dllName = dnfe.Message.Substring(startIndex, length);

            length = dllName.IndexOfInvariant('\'');
            if (length > 0)
            {
                dllName = dllName.Substring(0, length);
            }

            var platform = Environment.OSVersion.Platform.ToString();
            var message = $"The dynamic-link library for {dllName} could not be found. Please visit https://github.com/QuantConnect/Lean/blob/master/Algorithm.Python/readme.md for instructions on how to enable python support in {platform}";
            return new DllNotFoundException(message, dnfe);
        }
    }
}