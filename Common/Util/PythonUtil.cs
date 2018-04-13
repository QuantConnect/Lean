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

using Python.Runtime;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Collection of utils for python objects processing
    /// </summary>
    public class PythonUtil
    {
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
                int count = 0;
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
                int count = 0;
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
                int count = 0;
                if (!TryGetArgLength(pyObject, out count) || count != 1)
                {
                    return null;
                }
                dynamic method = GetModule().GetAttr("to_func");
                return method(pyObject, typeof(T1), typeof(T2)).AsManagedObject(typeof(Func<T1, T2>));
            }
        }

        /// <summary>
        /// Encapsulates a python method in coarse fundamental universe selector.
        /// </summary>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Func{IEnumerable{CoarseFundamental}, IEnumerable{Symbol}}"/> that encapsulates the python method</returns>
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
        /// <returns>A <see cref="System.Func{IEnumerable{FineFundamental}, IEnumerable{Symbol}}"/> that encapsulates the python method</returns>
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

            // Get the directory where the user files are located
            var baseScript = value.GetStringBetweenChars('\"', '\"');
            var length = Math.Max(baseScript.LastIndexOf('/'), baseScript.LastIndexOf('\\'));
            if (length < 0)
            {
                return string.Empty;
            }
            var directory = baseScript.Substring(0, 1 + length);

            // Format the information in every line
            var lines = value.Substring(1, value.Length - 1)
                .Split(new[] { "\'  File " }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Contains(directory))
                .Where(x => x.Split(',').Length > 2)
                .Select(x =>
                {
                    var info = x.Replace(directory, string.Empty).Split(',');
                    var line = info[0].GetStringBetweenChars('\"', '\"');
                    line = $" in {line}:{info[1].Trim()}";

                    info = info[2].Split(new[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
                    line = $" {info[0].Replace(" in ", " at ")}{line}";

                    // If we have the exact statement, add it to the error line
                    if (info.Length > 2) line += $" :: {info[1].Trim()}";

                    return line;
                });

            var errorLine = string.Join(Environment.NewLine, lines);

            return string.IsNullOrWhiteSpace(errorLine)
                ? string.Empty
                : $"{Environment.NewLine}{errorLine}{Environment.NewLine}";
        }

        /// <summary>
        /// Try to get the length of arguments of a method
        /// </summary>
        /// <param name="pyObject">Object representing a method</param>
        /// <param name="length">Lenght of arguments</param>
        /// <returns>True if pyObject is a method</returns>
        private static bool TryGetArgLength(PyObject pyObject, out int length)
        {
            using (Py.GIL())
            {
                dynamic inspect = Py.Import("inspect");

                if (inspect.isfunction(pyObject))
                {
                    var args = inspect.getargspec(pyObject).args;
                    length = new PyList(args).Length();
                    return true;
                }

                if (inspect.ismethod(pyObject))
                {
                    var args = inspect.getargspec(pyObject).args;
                    length = new PyList(args).Length() - 1;
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
            return PythonEngine.ModuleFromString("x",
                "from clr import AddReference\n" +
                "AddReference(\"System\")\n" +
                "from System import Action, Func\n" +
                "def to_action1(pyobject, t1):\n" +
                "    return Action[t1](pyobject)\n" +
                "def to_action2(pyobject, t1, t2):\n" +
                "    return Action[t1, t2](pyobject)\n" +
                "def to_func(pyobject, t1, t2):\n" +
                "    return Func[t1, t2](pyobject)");
        }
    }
}