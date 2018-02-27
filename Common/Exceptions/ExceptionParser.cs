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

using Python.Runtime;
using QuantConnect.Interfaces;
using System;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Parser that converts a regular exception into a new one.
    /// </summary>
    public class ExceptionParser : IExceptionParser
    {
        private CSharpUserExceptionParser _cSharpUserExceptionParser;
        private PythonUserExceptionParser _pythonUserExceptionParser;

        /// <summary>
        /// Creates an instance of <see cref="ExceptionParser"/> that sorts which parser will be applied to an incoming exception 
        /// </summary>
        public ExceptionParser()
        {
            _cSharpUserExceptionParser = new CSharpUserExceptionParser();
            _pythonUserExceptionParser = new PythonUserExceptionParser();
        }

        /// <summary>
        /// Parses an <see cref="Exception"/> object into a new one
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> object to parse into a new one.</param>
        /// <returns>Parsed exception</returns>
        public Exception Parse(Exception exception)
        {
            try
            {
                if (exception is PythonException || exception.InnerException is PythonException)
                {
                    return _pythonUserExceptionParser.Parse(exception);
                }
                else
                {
                    return _cSharpUserExceptionParser.Parse(exception);
                }
            }
            catch
            {
                return exception;
            }
        }
    }
}