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
 *
*/

using System;
using System.Linq;
using Python.Runtime;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using System.Text.RegularExpressions;
using QuantConnect.Data.UniverseSelection;
using System.IO;
using System.Globalization;

namespace QuantConnect.Util
{
    /// <summary>
    /// Collection of utils for python objects processing
    /// </summary>
    public class PythonUtil
    {
        private static Regex LineRegex = new Regex("line (\\d+)", RegexOptions.Compiled);
        private static Regex StackTraceFileLineRegex = new Regex("\"(.+)\", line (\\d+), in (.+)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Lazy<dynamic> lazyInspect = new Lazy<dynamic>(() => Py.Import("inspect"));

        /// <summary>
        /// The python exception stack trace line shift to use
        /// </summary>
        public static int ExceptionLineShift { get; set; } = 0;

        /// <summary>
        /// Encapsulates a python method with a <see cref="System.Action{T1}"/>
        /// </summary>
        /// <typeparam name="T1">The input type</typeparam>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Action{T1}"/> that encapsulates the python method</returns>
        public static Action<T1> ToAction<T1>(PyObject pyObject)
        {
            using (Py.GIL())
            {
                long count = 0;
                if (!TryGetArgLength(pyObject, out count) || count != 1)
                {
                    return null;
                }
                dynamic method = GetModule().GetAttr("to_action1");
                return method(pyObject, typeof(T1)).AsManagedObject(typeof(Action<T1>));
            }
        }

        /// <summary>
        /// Encapsulates a python method with a <see cref="System.Action{T1, T2}"/>
        /// </summary>
        /// <typeparam name="T1">The first input type</typeparam>
        /// <typeparam name="T2">The second input type type</typeparam>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Action{T1, T2}"/> that encapsulates the python method</returns>
        public static Action<T1, T2> ToAction<T1, T2>(PyObject pyObject)
        {
            using (Py.GIL())
            {
                long count = 0;
                if (!TryGetArgLength(pyObject, out count) || count != 2)
                {
                    return null;
                }
                dynamic method = GetModule().GetAttr("to_action2");
                return method(pyObject, typeof(T1), typeof(T2)).AsManagedObject(typeof(Action<T1, T2>));
            }
        }

        /// <summary>
        /// Encapsulates a python method with a <see cref="System.Func{T1, T2}"/>
        /// </summary>
        /// <typeparam name="T1">The data type</typeparam>
        /// <typeparam name="T2">The output type</typeparam>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Func{T1, T2}"/> that encapsulates the python method</returns>
        public static Func<T1, T2> ToFunc<T1, T2>(PyObject pyObject)
        {
            using (Py.GIL())
            {
                long count = 0;
                if (!TryGetArgLength(pyObject, out count) || count != 1)
                {
                    return null;
                }
                dynamic method = GetModule().GetAttr("to_func1");
                return method(pyObject, typeof(T1), typeof(T2)).AsManagedObject(typeof(Func<T1, T2>));
            }
        }

        /// <summary>
        /// Encapsulates a python method with a <see cref="System.Func{T1, T2, T3}"/>
        /// </summary>
        /// <typeparam name="T1">The first argument's type</typeparam>
        /// <typeparam name="T2">The first argument's type</typeparam>
        /// <typeparam name="T3">The output type</typeparam>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Func{T1, T2, T3}"/> that encapsulates the python method</returns>
        public static Func<T1, T2, T3> ToFunc<T1, T2, T3>(PyObject pyObject)
        {
            using (Py.GIL())
            {
                long count = 0;
                if (!TryGetArgLength(pyObject, out count) || count != 2)
                {
                    return null;
                }
                dynamic method = GetModule().GetAttr("to_func2");
                return method(pyObject, typeof(T1), typeof(T2), typeof(T3)).AsManagedObject(typeof(Func<T1, T2, T3>));
            }
        }

        /// <summary>
        /// Encapsulates a python method in coarse fundamental universe selector.
        /// </summary>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="Func{T, TResult}"/> (parameter is <see cref="IEnumerable{CoarseFundamental}"/>, return value is <see cref="IEnumerable{Symbol}"/>) that encapsulates the python method</returns>
        public static Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> ToCoarseFundamentalSelector(PyObject pyObject)
        {
            var selector = ToFunc<IEnumerable<CoarseFundamental>, Symbol[]>(pyObject);
            if (selector == null)
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"{pyObject.Repr()} is not a valid coarse fundamental universe selector method.");
                }
            }
            return selector;
        }

        /// <summary>
        /// Encapsulates a python method in fine fundamental universe selector.
        /// </summary>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="Func{T, TResult}"/> (parameter is <see cref="IEnumerable{FineFundamental}"/>, return value is <see cref="IEnumerable{Symbol}"/>) that encapsulates the python method</returns>
        public static Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> ToFineFundamentalSelector(PyObject pyObject)
        {
            var selector = ToFunc<IEnumerable<FineFundamental>, Symbol[]>(pyObject);
            if (selector == null)
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"{pyObject.Repr()} is not a valid fine fundamental universe selector method.");
                }
            }
            return selector;
        }

        /// <summary>
        /// Parsers <see cref="PythonException"/> into a readable message
        /// </summary>
        /// <param name="pythonException">The exception to parse</param>
        /// <returns>String with relevant part of the stacktrace</returns>
        public static string PythonExceptionParser(PythonException pythonException)
        {
            return PythonExceptionMessageParser(pythonException.Message) + PythonExceptionStackParser(pythonException.StackTrace);
        }

        /// <summary>
        /// Parsers <see cref="PythonException.Message"/> into a readable message
        /// </summary>
        /// <param name="message">The python exception message</param>
        /// <returns>String with relevant part of the stacktrace</returns>
        public static string PythonExceptionMessageParser(string message)
        {
            var match = LineRegex.Match(message);
            if (match.Success)
            {
                foreach (Match lineCapture in match.Captures)
                {
                    var newLineNumber = int.Parse(lineCapture.Groups[1].Value) + ExceptionLineShift;
                    message = Regex.Replace(message, lineCapture.ToString(), $"line {newLineNumber}");
                }
            }
            else if (message.Contains(" value cannot be converted to ", StringComparison.InvariantCulture))
            {
                message += ": This error is often encountered when assigning to a member defined in the base QCAlgorithm class. For example, self.universe conflicts with 'QCAlgorithm.Universe' but can be fixed by prefixing private variables with an underscore, self._universe.";
            }

            return message;
        }

        /// <summary>
        /// Parsers <see cref="PythonException.StackTrace"/> into a readable message
        /// </summary>
        /// <param name="value">String with the stacktrace information</param>
        /// <returns>String with relevant part of the stacktrace</returns>
        public static string PythonExceptionStackParser(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // The stack trace info before "at Python.Runtime." is the trace we want,
            // which is for user Python code.
            var endIndex = value.IndexOf("at Python.Runtime.", StringComparison.InvariantCulture);
            var neededStackTrace = endIndex > 0 ? value.Substring(0, endIndex) : value;

            // The stack trace is separated in blocks by file
            var blocks = neededStackTrace.Split("  File ", StringSplitOptions.RemoveEmptyEntries)
                .Select(fileTrace =>
                {
                    var trimedTrace = fileTrace.Trim();
                    if (string.IsNullOrWhiteSpace(trimedTrace))
                    {
                        return string.Empty;
                    }

                    var match = StackTraceFileLineRegex.Match(trimedTrace);
                    if (!match.Success)
                    {
                        return string.Empty;
                    }

                    var capture = match.Captures[0] as Match;

                    var filePath = capture.Groups[1].Value;
                    var lastFileSeparatorIndex = Math.Max(filePath.LastIndexOf('/'), filePath.LastIndexOf('\\'));
                    if (lastFileSeparatorIndex < 0)
                    {
                        return string.Empty;
                    }

                    var fileName = filePath.Substring(lastFileSeparatorIndex + 1);
                    var lineNumber = int.Parse(capture.Groups[2].Value, CultureInfo.InvariantCulture) + ExceptionLineShift;
                    var locationAndInfo = capture.Groups[3].Value.Trim();

                    return $"  at {locationAndInfo}{Environment.NewLine} in {fileName}: line {lineNumber}";
                })
                .Where(x => !string.IsNullOrWhiteSpace(x));

            var result = string.Join(Environment.NewLine, blocks);
            result = Extensions.ClearLeanPaths(result);

            return string.IsNullOrWhiteSpace(result)
                ? string.Empty
                : $"{Environment.NewLine}{result}{Environment.NewLine}";
        }

        /// <summary>
        /// Try to get the length of arguments of a method
        /// </summary>
        /// <param name="pyObject">Object representing a method</param>
        /// <param name="length">Lenght of arguments</param>
        /// <returns>True if pyObject is a method</returns>
        private static bool TryGetArgLength(PyObject pyObject, out long length)
        {
            using (Py.GIL())
            {
                var inspect = lazyInspect.Value;
                if (inspect.isfunction(pyObject))
                {
                    var args = inspect.getfullargspec(pyObject).args as PyObject;
                    var pyList = new PyList(args);
                    length = pyList.Length();
                    pyList.Dispose();
                    args.Dispose();
                    return true;
                }

                if (inspect.ismethod(pyObject))
                {
                    var args = inspect.getfullargspec(pyObject).args as PyObject;
                    var pyList = new PyList(args);
                    length = pyList.Length() - 1;
                    pyList.Dispose();
                    args.Dispose();
                    return true;
                }
            }
            length = 0;
            return false;
        }

        /// <summary>
        /// Creates a python module with utils methods
        /// </summary>
        /// <returns>PyObject with a python module</returns>
        private static PyObject GetModule()
        {
            return PyModule.FromString("x",
                "from clr import AddReference\n" +
                "AddReference(\"System\")\n" +
                "from System import Action, Func\n" +
                "def to_action1(pyobject, t1):\n" +
                "    return Action[t1](pyobject)\n" +
                "def to_action2(pyobject, t1, t2):\n" +
                "    return Action[t1, t2](pyobject)\n" +
                "def to_func1(pyobject, t1, t2):\n" +
                "    return Func[t1, t2](pyobject)\n" +
                "def to_func2(pyobject, t1, t2, t3):\n" +
                "    return Func[t1, t2, t3](pyobject)");
        }

        /// <summary>
        /// Convert Python input to a list of Symbols
        /// </summary>
        /// <param name="input">Object with the desired property</param>
        /// <returns>List of Symbols</returns>
        public static IEnumerable<Symbol> ConvertToSymbols(PyObject input)
        {
            List<Symbol> symbolsList;
            Symbol symbol;

            using (Py.GIL())
            {
                // Handle the possible types of conversions
                if (PyList.IsListType(input))
                {
                    List<string> symbolsStringList;

                    //Check if an entry in the list is a string type, if so then try and convert the whole list
                    if (PyString.IsStringType(input[0]) && input.TryConvert(out symbolsStringList))
                    {
                        symbolsList = new List<Symbol>();
                        foreach (var stringSymbol in symbolsStringList)
                        {
                            symbol = QuantConnect.Symbol.Create(stringSymbol, SecurityType.Equity, Market.USA);
                            symbolsList.Add(symbol);
                        }
                    }
                    //Try converting it to list of symbols, if it fails throw exception
                    else if (!input.TryConvert(out symbolsList))
                    {
                        throw new ArgumentException($"Cannot convert list {input.Repr()} to symbols");
                    }
                }
                else
                {
                    //Check if its a single string, and try and convert it
                    string symbolString;
                    if (PyString.IsStringType(input) && input.TryConvert(out symbolString))
                    {
                        symbol = QuantConnect.Symbol.Create(symbolString, SecurityType.Equity, Market.USA);
                        symbolsList = new List<Symbol> { symbol };
                    }
                    else if (input.TryConvert(out symbol))
                    {
                        symbolsList = new List<Symbol> { symbol };
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot convert object {input.Repr()} to symbol");
                    }
                }
            }
            return symbolsList;
        }
    }
}
