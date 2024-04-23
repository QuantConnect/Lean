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
using Python.Runtime;
using NUnit.Framework;
using System.Threading;
using QuantConnect.Python;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonThreadingTests
    {
        [TestCase("Field", false)]
        [TestCase("Func()", false)]
        [TestCase("Property", false)]
        [TestCase("Field", true)]
        [TestCase("Func()", true)]
        [TestCase("Property", true)]
        public void CallingCShapReleasesGil(string target, bool useInstance)
        {
            var lockInstance = new object();

            using var tookGil = new ManualResetEvent(false);
            using var tookLock = new ManualResetEvent(false);

            PyObject method;
            PyObject propertyWrapper;
            using (Py.GIL())
            {
                var module = PyModule.FromString("ReleaseGil", $@"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *
import time

def Method():
    return 1

def PropertyCaller(tookGil, tookLock, useInstance):
    tookGil.Set()
    tookLock.WaitOne()
    if useInstance:
        instance = PythonThreadingTests.TestPropertyWrapper()
        return instance.Instance{target}
    else:
        return PythonThreadingTests.TestPropertyWrapper.{target}
");
                method = module.GetAttr("Method");
                propertyWrapper = module.GetAttr("PropertyCaller");
            }

            TestPropertyWrapper.Func = () =>
            {
                lock (lockInstance)
                {
                    Thread.Sleep(500);
                    using (Py.GIL())
                    {
                        return method.Invoke().As<int>();
                    }
                }
            };

            // task1: has the GIL, go into python, go back into C# and want the C# lock, so he should release the GIL
            // task2: has the C# lock and want's the GIL
            var task1 = Task.Run(() =>
            {
                using (Py.GIL())
                {
                    propertyWrapper.Invoke(tookGil, tookLock, useInstance);
                }
            });

            var task2 = Task.Run(() =>
            {
                lock (lockInstance)
                {
                    // we take the C# lock and wait and try to get the py gil which should be taken by task 1
                    tookLock.Set();
                    tookGil.WaitOne();
                    using (Py.GIL())
                    {
                        method.Invoke();
                    }
                }
            });

            var result = Task.WaitAll(new[] { task1, task2 }, TimeSpan.FromSeconds(3));

            Assert.IsTrue(result);
        }

        [Test]
        public void ImportsCanBeExecutedFromDifferentThreads()
        {
            PythonInitializer.Initialize();

            Task.Factory.StartNew(() =>
            {
                using (Py.GIL())
                {
                    var module = Py.Import("Test_MethodOverload");
                    module.GetAttr("Test_MethodOverload").Invoke();
                }
            }).Wait();

            PythonInitializer.Initialize();
            Task.Factory.StartNew(() =>
            {
                using (Py.GIL())
                {
                    var module = Py.Import("Test_AlgorithmPythonWrapper");
                    module.GetAttr("Test_AlgorithmPythonWrapper").Invoke();
                }
            }).Wait();
        }
        public class TestPropertyWrapper
        {
            public static Func<int> Func { get; set; }
            public static int Field => Func();
            public static int Property
            {
                get
                {
                    return Func();
                }
            }

            public Func<int> InstanceFunc => Func;
            public int InstanceField => Field;
            public int InstanceProperty => Property;
        }
    }
}
