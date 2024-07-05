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
using System.Runtime.CompilerServices;
using Python.Runtime;
using QuantConnect.Exceptions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Exceptions"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.DllNotFoundPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class DllNotFoundPythonExceptionInterpreter
        {
            /// <summary>
            /// Returns a string message saying the given dynamic-link library could not be found
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DynamicLinkLibraryNotFound(string dllName, string platform)
            {
                return $"The dynamic-link library for {dllName} could not be found. " +
                    "Please visit https://github.com/QuantConnect/Lean/blob/master/Algorithm.Python/readme.md for instructions " +
                    $"on how to enable python support in {platform}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.InvalidTokenPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class InvalidTokenPythonExceptionInterpreter
        {
            public static string InvalidTokenExpectedSubstring = "invalid token";

            public static string NotPermittedExpectedSubstring = "are not permitted;";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InterpretException(PythonException exception)
            {
                var message = "Trying to include an invalid token/character in any statement throws a SyntaxError exception. " +
                    "To prevent the exception, ensure no invalid token are mistakenly included (e.g: leading zero).";
                var errorLine = exception.Message.GetStringBetweenChars('(', ')');

                return $"{message}{Environment.NewLine}  in {errorLine}{Environment.NewLine}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.KeyErrorPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class KeyErrorPythonExceptionInterpreter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string KeyNotFoundInCollection(string key)
            {
                return "Trying to retrieve an element from a collection using a key that does not exist " +
                    $@"in that collection throws a KeyError exception. To prevent the exception, ensure that the {
                        key} key exist in the collection and/or that collection is not empty.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.NoMethodMatchPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class NoMethodMatchPythonExceptionInterpreter
        {
            public static string NoMethodMatchExpectedSubstring = "No method match";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string AttemptedToAccessMethodThatDoesNotExist(string methodName)
            {
                return "Trying to dynamically access a method that does not exist throws a TypeError exception. " +
                    $@"To prevent the exception, ensure each parameter type matches those required by the {
                        methodName} method. Please checkout the API documentation.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.ScheduledEventExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class ScheduledEventExceptionInterpreter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ScheduledEventName(string eventName)
            {
                return $"In Scheduled Event '{eventName}',";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.StackExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class StackExceptionInterpreter
        {
            /// <summary>
            /// Returns a message for a Loaded Exception Interpreter
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string LoadedExceptionInterpreter(IExceptionInterpreter interpreter)
            {
                return $"Loaded ExceptionInterpreter: {interpreter.GetType().Name}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.UnsupportedOperandPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class UnsupportedOperandPythonExceptionInterpreter
        {
            /// <summary>
            /// Unsupported Operand Type Expected substring
            /// </summary>
            public static string UnsupportedOperandTypeExpectedSubstring = "unsupported operand type";

            /// <summary>
            /// Returns a message for invalid object types for operation
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidObjectTypesForOperation(string types)
            {
                return $@"Trying to perform a summation, subtraction, multiplication or division between {
                    types} objects throws a TypeError exception. To prevent the exception, ensure that both values share the same type.";
            }
        }
    }
}
