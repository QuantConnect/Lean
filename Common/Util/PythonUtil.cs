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
using System;

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
        /// Get a string between two characters from another string
        /// </summary>
        /// <param name="value">The original string</param>
        /// <param name="left">Left bound of the substring</param>
        /// <param name="right">Right bound of the substring</param>
        /// <returns>Substring from original string bounded by the two characters</returns>
        public static string GetStringBetweenChars(string value, char left, char right)
        {
            var startIndex = 1 + value.IndexOf(left);
            var length = value.IndexOf(right, startIndex) - startIndex;
            if (length > 0)
            {
                return value.Substring(startIndex, length);
            }
            return null;
        }

        /// <summary>
        /// Parsers <see cref="PythonException.StackTrace"/> into a readable message 
        /// </summary>
        /// <param name="value">String with the stacktrace information</param>
        /// <returns>String with relevant part of the stacktrace</returns>
        public static string PythonExceptionStackParser(string value)
        {
            var errorLine = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return errorLine;
            }
            
            // Get the place where the error occurred in the PythonException.StackTrace
            var stack = value.Replace("\\\\", "/").Split(new[] { @"\n" }, StringSplitOptions.RemoveEmptyEntries);
            var baseScript = stack[0].Substring(1 + stack[0].IndexOf('\"')).Split('\"')[0];
            var directory = string.Empty;

            var length = baseScript.LastIndexOf('/');
            if (length < 0)
            {
                return errorLine;
            }

            directory = baseScript.Substring(0, length);
            baseScript = baseScript.Substring(1 + directory.Length);

            for (var i = 0; i < stack.Length; i += 2)
            {
                if (stack[i].Contains(directory))
                {
                    var index = stack[i].IndexOf(directory) + directory.Length + 1;
                    var info = stack[i].Substring(index).Split(',');

                    var script = info[0].Remove(info[0].Length - 1);
                    var line = int.Parse(info[1].Remove(0, 6));
                    var method = info[2].Replace("in", "at");
                    var statement = stack[i + 1].Trim();

                    errorLine += $"{Environment.NewLine}  {method} in {script}:line {line} :: {statement}{Environment.NewLine}";
                }
            }

            return errorLine;
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