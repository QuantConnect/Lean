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
using System.Reflection;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.Python
{
    /// <summary>
    /// This is the initial implementation of the Python.NET Runtime to pipe .NET events through to a Python Script.
    /// </summary>
    public class PythonAlgorithm : QCAlgorithm
    {

        private IntPtr _gs;

        /// <summary>
        /// Initialize the Python Algorithm
        /// </summary>
        public override void Initialize()
        {
            //Start the Python Connector:
            PythonEngine.Initialize();
            _gs = PythonEngine.AcquireLock();

            const string s = @"../../../../tests";
            Type RTClass = typeof(Runtime.Runtime);

            /* pyStrPtr = PyString_FromString(s); */
            MethodInfo PyString_FromString = RTClass.GetMethod("PyString_FromString", BindingFlags.NonPublic | BindingFlags.Static);
            object[] funcArgs = new object[1];
            funcArgs[0] = s;
            IntPtr pyStrPtr = (IntPtr)PyString_FromString.Invoke(null, funcArgs);

        }

        /// <summary>
        /// Pass Data TradeBars Events Through to Python Algorithm Instance
        /// </summary>
        /// <param name="data"></param>
        public void OnData(TradeBars data)
        {
            
        }


        /// <summary>
        /// Tear down the Python engine:
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            PythonEngine.ReleaseLock(_gs);
            PythonEngine.Shutdown();
        }
    }
}
