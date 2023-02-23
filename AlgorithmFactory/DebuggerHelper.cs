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
using System.Threading;
using System.Diagnostics;
using QuantConnect.Python;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using System.Collections.Concurrent;

namespace QuantConnect.AlgorithmFactory
{
    /// <summary>
    /// Helper class used to start a new debugging session
    /// </summary>
    public static class DebuggerHelper
    {
        private static readonly ConcurrentQueue<Py.GILState> _threadsState = new();

        /// <summary>
        /// The different implemented debugging methods
        /// </summary>
        public enum DebuggingMethod
        {
            /// <summary>
            /// Local debugging through cmdline.
            /// <see cref="Language.Python"/> will use built in 'pdb'
            /// </summary>
            LocalCmdline,

            /// <summary>
            /// Visual studio local debugging.
            /// <see cref="Language.Python"/> will use 'Python Tools for Visual Studio',
            /// attach manually selecting `Python` code type.
            /// </summary>
            VisualStudio,

            /// <summary>
            ///  Python Tool for Visual Studio Debugger for remote python debugging.
            /// <see cref="Language.Python"/>. Deprecated, routes to DebugPy which
            /// is it's replacement. Used in the same way.
            /// </summary>
            PTVSD,

            /// <summary>
            ///  DebugPy - a debugger for Python.
            /// <see cref="Language.Python"/> can use  `Python Extension` in VS Code
            /// or attach to Python in Visual Studio
            /// </summary>
            DebugPy,

            /// <summary>
            ///  PyCharm PyDev Debugger for remote python debugging.
            /// <see cref="Language.Python"/> will use 'Python Debug Server' in PyCharm
            /// </summary>
            PyCharm
        }

        /// <summary>
        /// Will start a new debugging session
        /// </summary>
        /// <param name="language">The algorithms programming language</param>
        /// <param name="workersInitializationCallback">Optionally, the debugging method will set an action which the data stack workers should execute
        /// so we can debug code executed by them, this is specially important for python.</param>
        public static void Initialize(Language language, out Action workersInitializationCallback)
        {
            workersInitializationCallback = null;
            if (language == Language.Python)
            {
                DebuggingMethod debuggingType;
                Enum.TryParse(Config.Get("debugging-method", DebuggingMethod.LocalCmdline.ToString()), true, out debuggingType);

                Log.Trace("DebuggerHelper.Initialize(): initializing python...");
                PythonInitializer.Initialize();
                Log.Trace("DebuggerHelper.Initialize(): python initialization done");

                using (Py.GIL())
                {
                    Log.Trace("DebuggerHelper.Initialize(): starting...");
                    switch (debuggingType)
                    {
                        case DebuggingMethod.LocalCmdline:
                            PythonEngine.RunSimpleString("import pdb; pdb.set_trace()");
                            break;

                        case DebuggingMethod.VisualStudio:
                            Log.Trace("DebuggerHelper.Initialize(): waiting for debugger to attach...");
                            PythonEngine.RunSimpleString(@"import sys; import time;
while not sys.gettrace():
    time.sleep(0.25)");
                            break;

                        case DebuggingMethod.PTVSD:
                            Log.Trace("DebuggerHelper.Initialize(): waiting for PTVSD debugger to attach at localhost:5678...");
                            PythonEngine.RunSimpleString("import ptvsd; ptvsd.enable_attach(); ptvsd.wait_for_attach()");
                            break;

                        case DebuggingMethod.DebugPy:
                            PythonEngine.RunSimpleString(@"import debugpy
from AlgorithmImports import *
from QuantConnect.Logging import *

Log.Trace(""DebuggerHelper.Initialize(): debugpy waiting for attach at port 5678..."");

debugpy.listen(('0.0.0.0', 5678))
debugpy.wait_for_client()");
                            workersInitializationCallback = DebugpyThreadInitialization;
                            break;

                        case DebuggingMethod.PyCharm:
                            Log.Trace("DebuggerHelper.Initialize(): Attempting to connect to Pycharm PyDev debugger server...");
                            PythonEngine.RunSimpleString(@"import pydevd_pycharm;  import time;
count = 1
while count <= 10:
    try:
        pydevd_pycharm.settrace('localhost', port=6000, stdoutToServer=True, stderrToServer=True, suspend=False)
        print('SUCCESS: Connected to local program')
        break
    except ConnectionRefusedError:
        pass

    try:    
        pydevd_pycharm.settrace('host.docker.internal', port=6000, stdoutToServer=True, stderrToServer=True, suspend=False)
        print('SUCCESS: Connected to docker container')
        break
    except ConnectionRefusedError:
        pass

    print('\n')
    print('Failed: Ensure your PyCharm Debugger is actively waiting for a connection at port 6000!')
    print('Try ' + count.__str__() + ' out of 10')
    print('\n')
    count += 1
    time.sleep(3)");
                            break;
                    }
                    Log.Trace("DebuggerHelper.Initialize(): started");
                }
            }
            else if(language == Language.CSharp)
            {
                if (Debugger.IsAttached)
                {
                    Log.Trace("DebuggerHelper.Initialize(): debugger is already attached, triggering initial break.");
                }
                else
                {
                    Log.Trace("DebuggerHelper.Initialize(): waiting for debugger to attach...");
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(250);
                    }
                    Log.Trace("DebuggerHelper.Initialize(): debugger attached");
                }
            }
            else
            {
                throw new NotImplementedException($"DebuggerHelper.Initialize(): not implemented for {language}");
            }
        }

        /// <summary>
        /// For each thread we need to create it's python state, we do this by taking the GIL and we later release it by calling 'BeginAllowThreads'
        /// but we do not dispose of it. If we did, the state of the debugpy calls we do here are lost. So we keep a reference of the GIL we've
        /// created so it's not picked up the C# garbage collector and disposed off, which would clear the py thread state.
        /// </summary>
        private static void DebugpyThreadInitialization()
        {
            _threadsState.Enqueue(Py.GIL());
            PythonEngine.BeginAllowThreads();

            Log.Debug($"DebuggerHelper.Initialize({Thread.CurrentThread.Name}): initializing debugpy for thread...");
            using (Py.GIL())
            {
                PythonEngine.RunSimpleString("import debugpy;debugpy.debug_this_thread();debugpy.trace_this_thread(True)");
            }
        }
    }
}
