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
using System.Diagnostics;
using Python.Runtime;

namespace QuantConnect.Python
{
    /// <summary>
    /// Helper class for Python initialization
    /// </summary>
    public static class PythonInitializer
    {
        // Used to allow multiple Python unit and regression tests to be run in the same test run
        private static bool _isBeginAllowThreadsCalled;

        /// <summary>
        /// Initialize the Python.NET library
        /// </summary>
        public static void Initialize()
        {
            if (!_isBeginAllowThreadsCalled)
            {
                var benchmark = Stopwatch.StartNew();
                PythonEngine.Initialize();
                benchmark.Restart();
                Logging.Log.Trace("PythonInitializer(): Python Engine Initialized in " + benchmark.Elapsed.TotalSeconds + "s. Thread: " + System.Threading.Thread.CurrentThread.Name);

                // required for multi-threading usage
                PythonEngine.BeginAllowThreads();
                Logging.Log.Trace("PythonInitializer():  Allow threading completed in " + benchmark.Elapsed.TotalSeconds + "s. Thread: " + System.Threading.Thread.CurrentThread.Name);
                _isBeginAllowThreadsCalled = true;
            }
        }
    }
}
