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

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Interprets <see cref="NoMethodMatchPythonExceptionInterpreter"/> instances
    /// </summary>
    public class NoMethodMatchPythonExceptionInterpreter : PythonExceptionInterpreter
    {
        /// <summary>
        /// Attribute names pythonnet attaches to the bind-failure TypeError, carrying the
        /// data its message is built from: the snake_case method name and the rendered
        /// overloads hint block exactly as it appears at the end of the message.
        /// </summary>
        private const string MethodNameAttribute = "_clr_method_name";
        private const string OverloadsHintAttribute = "_clr_overloads_hint";

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
            return base.CanInterpret(exception) &&
                exception.Message.Contains(Messages.NoMethodMatchPythonExceptionInterpreter.NoMethodMatchExpectedSubstring);
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

            // Prefer the structured data pythonnet attaches to the bind-failure TypeError,
            // falling back to parsing the message for versions that do not attach it.
            TryGetStructuredBindFailureData(pe, out var methodName, out var overloadsHint);

            methodName ??= GetMethodName(pe.Message);
            var message = Messages.NoMethodMatchPythonExceptionInterpreter.AttemptedToAccessMethodThatDoesNotExist(methodName);

            overloadsHint ??= GetOverloadsHint(pe.Message);
            if (!string.IsNullOrEmpty(overloadsHint))
            {
                message += $" {overloadsHint}";
            }

            message += PythonUtil.PythonExceptionStackParser(pe.StackTrace);

            return new MissingMethodException(message, pe);
        }

        /// <summary>
        /// Reads the structured bind-failure data pythonnet attaches to the TypeError so
        /// the method name and overloads hint do not have to be parsed out of the message.
        /// Both outputs are null when the attributes are absent (raised by a pythonnet
        /// version that does not attach them) or cannot be read.
        /// </summary>
        private static void TryGetStructuredBindFailureData(PythonException exception, out string methodName,
            out string overloadsHint)
        {
            methodName = null;
            overloadsHint = null;

            try
            {
                var value = exception.Value;
                if (value == null)
                {
                    return;
                }

                using (Py.GIL())
                {
                    if (value.HasAttr(MethodNameAttribute))
                    {
                        using var nameAttribute = value.GetAttr(MethodNameAttribute);
                        methodName = nameAttribute.As<string>();
                    }
                    if (value.HasAttr(OverloadsHintAttribute))
                    {
                        using var hintAttribute = value.GetAttr(OverloadsHintAttribute);
                        overloadsHint = hintAttribute.As<string>();
                    }
                }

                // Normalize so the caller's null-coalescing fallback kicks in
                if (string.IsNullOrEmpty(methodName))
                {
                    methodName = null;
                }
                if (string.IsNullOrEmpty(overloadsHint))
                {
                    overloadsHint = null;
                }
            }
            catch
            {
                // Fall back to message parsing on any failure reading the attributes
                methodName = null;
                overloadsHint = null;
            }
        }

        /// <summary>
        /// Extracts the name of the method that failed to resolve from the Python exception message.
        /// The message has the form: "No method matches given arguments for {methodName}: ({argumentTypes})",
        /// so the method name sits between the "for " keyword and the following ":".
        /// </summary>
        private static string GetMethodName(string exceptionMessage)
        {
            const string forKeyword = "for ";
            var forIndex = exceptionMessage.IndexOfInvariant(forKeyword);
            if (forIndex == -1)
            {
                // Unexpected format, fall back to the whole message
                return exceptionMessage.Trim();
            }

            var methodNameStart = forIndex + forKeyword.Length;
            var colonIndex = exceptionMessage.IndexOf(':', methodNameStart);
            var methodName = colonIndex > methodNameStart
                ? exceptionMessage.Substring(methodNameStart, colonIndex - methodNameStart)
                : exceptionMessage.Substring(methodNameStart);

            return methodName.Trim();
        }

        /// <summary>
        /// Extracts the candidate-signatures hint pythonnet appends to the binding-failure
        /// message ("The expected signature is:" or "The following overloads are available:"
        /// followed by the signatures), so the interpreted message can keep it.
        /// </summary>
        private static string GetOverloadsHint(string exceptionMessage)
        {
            var hintIndex = exceptionMessage.IndexOfInvariant("The expected signature is:");
            if (hintIndex == -1)
            {
                hintIndex = exceptionMessage.IndexOfInvariant("The following overloads are available:");
            }

            return hintIndex == -1 ? null : exceptionMessage.Substring(hintIndex).Trim();
        }
    }
}
