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
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuantConnect.Logging
{
    /// <summary>
    /// Logging management class.
    /// </summary>
    public static class Log
    {
        private static string _lastTraceText = "";
        private static string _lastErrorText = "";
        private static bool _debuggingEnabled;
        private static int _level = 1;
        private static ILogHandler _logHandler = new ConsoleLogHandler();

        /// <summary>
        /// Gets or sets the ILogHandler instance used as the global logging implementation.
        /// </summary>
        public static ILogHandler LogHandler
        {
            get { return _logHandler; }
            set { _logHandler = value; }
        }

        /// <summary>
        /// Global flag whether to enable debugging logging:
        /// </summary>
        public static bool DebuggingEnabled
        {
            get { return _debuggingEnabled; }
            set { _debuggingEnabled = value; }
        }

        /// <summary>
        /// Global flag to specify file based log path
        /// </summary>
        /// <remarks>Only valid for file based loggers</remarks>
        public static string FilePath { get; set; } = "log.txt";

        /// <summary>
        /// Set the minimum message level:
        /// </summary>
        public static int DebuggingLevel
        {
            get { return _level; }
            set { _level = value; }
        }

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="error">String Error</param>
        /// <param name="overrideMessageFloodProtection">Force sending a message, overriding the "do not flood" directive</param>
        public static void Error(string error, bool overrideMessageFloodProtection = false)
        {
            try
            {
                if (error == _lastErrorText && !overrideMessageFloodProtection) return;
                _logHandler.Error(error);
                _lastErrorText = error; //Stop message flooding filling diskspace.
            }
            catch (Exception err)
            {
                Console.WriteLine("Log.Error(): Error writing error: " + err.Message);
            }
        }

        /// <summary>
        /// Log error. This overload is usefull when exceptions are being thrown from within an anonymous function.
        /// </summary>
        /// <param name="method">The method identifier to be used</param>
        /// <param name="exception">The exception to be logged</param>
        /// <param name="message">An optional message to be logged, if null/whitespace the messge text will be extracted</param>
        /// <param name="overrideMessageFloodProtection">Force sending a message, overriding the "do not flood" directive</param>
        private static void Error(string method, Exception exception, string message = null, bool overrideMessageFloodProtection = false)
        {
            message = method + "(): " + (message ?? string.Empty) + " " + exception;
            Error(message, overrideMessageFloodProtection);
        }

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="exception">The exception to be logged</param>
        /// <param name="message">An optional message to be logged, if null/whitespace the messge text will be extracted</param>
        /// <param name="overrideMessageFloodProtection">Force sending a message, overriding the "do not flood" directive</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(Exception exception, string message = null, bool overrideMessageFloodProtection = false)
        {
            Error(WhoCalledMe.GetMethodName(1), exception, message, overrideMessageFloodProtection);
        }

        /// <summary>
        /// Log trace
        /// </summary>
        public static void Trace(string traceText, bool overrideMessageFloodProtection = false)
        {
            try
            {
                if (traceText == _lastTraceText && !overrideMessageFloodProtection) return;
                _logHandler.Trace(traceText);
                _lastTraceText = traceText;
            }
            catch (Exception err)
            {
                Console.WriteLine("Log.Trace(): Error writing trace: "  +err.Message);
            }
        }

        /// <summary>
        /// Writes the message in normal text
        /// </summary>
        public static void Trace(string format, params object[] args)
        {
            Trace(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// Writes the message in red
        /// </summary>
        public static void Error(string format, params object[] args)
        {
            Error(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// Output to the console
        /// </summary>
        /// <param name="text">The message to show</param>
        /// <param name="level">debug level</param>
        public static void Debug(string text, int level = 1)
        {
            try
            {
                if (!_debuggingEnabled || level < _level) return;
                _logHandler.Debug(text);
            }
            catch (Exception err)
            {
                Console.WriteLine("Log.Debug(): Error writing debug: " + err.Message);
            }
        }

        /// <summary>
        /// C# Equivalent of Print_r in PHP:
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="recursion"></param>
        /// <returns></returns>
        public static string VarDump(object obj, int recursion = 0)
        {
            var result = new StringBuilder();

            // Protect the method against endless recursion
            if (recursion < 5)
            {
                // Determine object type
                var t = obj.GetType();

                // Get array with properties for this object
                var properties = t.GetProperties();

                foreach (var property in properties)
                {
                    try
                    {
                        // Get the property value
                        var value = property.GetValue(obj, null);

                        // Create indenting string to put in front of properties of a deeper level
                        // We'll need this when we display the property name and value
                        var indent = String.Empty;
                        var spaces = "|   ";
                        var trail = "|...";

                        if (recursion > 0)
                        {
                            indent = new StringBuilder(trail).Insert(0, spaces, recursion - 1).ToString();
                        }

                        if (value != null)
                        {
                            // If the value is a string, add quotation marks
                            var displayValue = value.ToString();
                            if (value is string) displayValue = String.Concat('"', displayValue, '"');

                            // Add property name and value to return string
                            result.AppendFormat(CultureInfo.InvariantCulture, "{0}{1} = {2}\n", indent, property.Name, displayValue);

                            try
                            {
                                if (!(value is ICollection))
                                {
                                    // Call var_dump() again to list child properties
                                    // This throws an exception if the current property value
                                    // is of an unsupported type (eg. it has not properties)
                                    result.Append(VarDump(value, recursion + 1));
                                }
                                else
                                {
                                    // 2009-07-29: added support for collections
                                    // The value is a collection (eg. it's an arraylist or generic list)
                                    // so loop through its elements and dump their properties
                                    var elementCount = 0;
                                    foreach (var element in ((ICollection)value))
                                    {
                                        var elementName = $"{property.Name}[{elementCount}]";
                                        indent = new StringBuilder(trail).Insert(0, spaces, recursion).ToString();

                                        // Display the collection element name and type
                                        result.AppendFormat(CultureInfo.InvariantCulture, "{0}{1} = {2}\n", indent, elementName, element.ToString());

                                        // Display the child properties
                                        result.Append(VarDump(element, recursion + 2));
                                        elementCount++;
                                    }

                                    result.Append(VarDump(value, recursion + 1));
                                }
                            } catch { }
                        }
                        else
                        {
                            // Add empty (null) property to return string
                            result.AppendFormat(CultureInfo.InvariantCulture, "{0}{1} = {2}\n", indent, property.Name, "null");
                        }
                    }
                    catch
                    {
                        // Some properties will throw an exception on property.GetValue()
                        // I don't know exactly why this happens, so for now i will ignore them...
                    }
                }
            }

            return result.ToString();
        }
    }
}
