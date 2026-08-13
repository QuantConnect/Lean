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
            /// <summary>
            /// String message saying: invalid token
            /// </summary>
            public static string InvalidTokenExpectedSubstring = "invalid token";

            /// <summary>
            /// String message saying: are not permitted
            /// </summary>
            public static string NotPermittedExpectedSubstring = "are not permitted;";

            /// <summary>
            /// Returns a string message saying: Tring to include an invalid token/character in any statement throws s SyntaxError
            /// exception. It also contains an advice to prevent that exception
            /// </summary>
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
        /// Provides user-facing messages for the <see cref="Exceptions.AttributeErrorPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class AttributeErrorPythonExceptionInterpreter
        {
            /// <summary>
            /// Returns a hint explaining that the accessed attribute belongs to TradeBar, not QuoteBar, and how to get it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string QuoteBarHasNoTradeData(string attribute)
            {
                return $"QuoteBar holds quote data (bid/ask bars and sizes) and has no '{attribute}': trade data like volume comes with " +
                    "TradeBar. Use data.bars.get(symbol) for the TradeBar, and note that data[symbol] returns a QuoteBar when only " +
                    "quote data exists at that moment (common for forex, futures and crypto).";
            }

            /// <summary>
            /// Returns a hint explaining that the accessed attribute belongs to QuoteBar, not TradeBar, and how to get it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TradeBarHasNoQuoteData(string attribute)
            {
                return $"TradeBar holds trade data (open/high/low/close/volume) and has no '{attribute}': bid/ask quotes come with " +
                    "QuoteBar. Use data.quote_bars.get(symbol) for the QuoteBar.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.KeyErrorPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class KeyErrorPythonExceptionInterpreter
        {
            /// <summary>
            /// Returns a string message naming the key that was not found in the collection (when it could be extracted
            /// from the KeyError) and advising the user on how to prevent this exception
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string KeyNotFoundInCollection(string key)
            {
                var keyDescription = string.IsNullOrWhiteSpace(key) ? "The requested key" : $"The key '{key}'";
                return $"{keyDescription} was not found in the collection, which raises a KeyError exception. " +
                    "To prevent the exception, use collection.get(key), which returns None when the key is not found, " +
                    "or guard the access with 'if key in collection:'.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.ModuleNotFoundPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class ModuleNotFoundPythonExceptionInterpreter
        {
            /// <summary>
            /// Returns a string message saying the given module could not be found, with advice on how to fix it.
            /// The .NET assembly advice is only included when the module name looks like a .NET namespace
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ModuleNotFound(string moduleName, bool isModuleNameCamelCased)
            {
                var message = $"No module named '{moduleName}'. If it is a Python package, ensure it is installed in the environment.";
                if (isModuleNameCamelCased)
                {
                    message += " If it is a .NET assembly, importing it is not supported, use an equivalent Python package instead.";
                }
                return message;
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.MultipleInheritancePythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class MultipleInheritancePythonExceptionInterpreter
        {
            /// <summary>
            /// String message saying: cannot use multiple inheritance with managed classes
            /// </summary>
            public static string MultipleInheritanceExpectedSubstring = "cannot use multiple inheritance with managed classes";

            /// <summary>
            /// String message saying a Python class cannot inherit from multiple classes when one of them is a C# class.
            /// It also contains an advice on how to fix it
            /// </summary>
            public static string InvalidMultipleInheritance =
                "A Python class cannot inherit from multiple classes when one of them is a C# class, like QCAlgorithm. " +
                "Keep the C# class as the only base and move the other bases' members into helper classes used via " +
                "composition (e.g. self._helper = MyHelper()).";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.NoMethodMatchPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class NoMethodMatchPythonExceptionInterpreter
        {
            /// <summary>
            /// String message saying: No method match
            /// </summary>
            public static string NoMethodMatchExpectedSubstring = "No method match";

            /// <summary>
            /// Returns a string message saying the given method does not exists. It also contains the exception
            /// thrown is this case and an advice on how to prevent it
            /// </summary>
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
            /// <summary>
            /// Returns a string message with the given event name
            /// </summary>
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

            /// <summary>
            /// Hint appended when the invalid operands are a datetime.datetime and a datetime.date
            /// </summary>
            public static string DatetimeDateOperationHint =
                " To operate between them, use the date part of the datetime value, e.g.: (expiry.date() - today).days or self.time.date().";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Exceptions.DatetimeDateComparisonPythonExceptionInterpreter"/> class and its consumers or related classes
        /// </summary>
        public static class DatetimeDateComparisonPythonExceptionInterpreter
        {
            /// <summary>
            /// Expected substring of the TypeError raised when comparing a datetime.datetime with a datetime.date
            /// </summary>
            public static string CantCompareExpectedSubstring = "can't compare datetime.datetime to datetime.date";

            /// <summary>
            /// User-facing message for datetime.datetime vs datetime.date comparisons
            /// </summary>
            public static string InvalidDatetimeDateComparison =
                "Trying to compare 'datetime.datetime' and 'datetime.date' objects throws a TypeError exception. " +
                "To prevent the exception, compare using the date part of the datetime value, e.g.: self.time.date() <= some_date.";
        }
    }
}
