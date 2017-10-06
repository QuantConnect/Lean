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
        /// Encapsulates a python method with a <see cref="System.Func{T, TResult}"/>
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <typeparam name="TSecond">The output type</typeparam>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Func{T, TResult}"/> that encapsulates the python method</returns>
        public static Func<T, TSecond> ToFunc<T, TSecond>(PyObject pyObject)
        {
            var testMod =
               "from clr import AddReference\n" +
               "AddReference(\"System\")\n" +
               "from System import Func\n" +
               "def to_func(pyobject, in_type, out_type):\n" +
               "    return Func[in_type, out_type](pyobject)";

            using (Py.GIL())
            {
                if (!pyObject.IsCallable()) return null;
                dynamic toFunc = PythonEngine.ModuleFromString("x", testMod).GetAttr("to_func");
                return toFunc(pyObject, typeof(T), typeof(TSecond)).AsManagedObject(typeof(Func<T, TSecond>));
            }
        }
    }
}
