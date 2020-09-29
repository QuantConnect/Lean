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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Configuration;
using QuantConnect.Data;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    class QuantBookAlgorithmTests
    {
        [Test]
        public void PythonAlgorithmTest()
        {
            using (Py.GIL())
            {
                Config.Set("research-is-python", true);
                var module = Py.Import("Test_QuantBookAlgorithm");
                dynamic PythonQB = module.GetAttr("PythonQB").Invoke();

                var initializeCalled = PythonQB.InitializeCalled.AsManagedObject(typeof(bool));
                var onDataCalled = PythonQB.OnDataCalled.AsManagedObject(typeof(bool));

                Assert.IsFalse(initializeCalled);
                Assert.IsFalse(onDataCalled);

                PythonQB.Step();

                initializeCalled = PythonQB.InitializeCalled.AsManagedObject(typeof(bool));
                onDataCalled = PythonQB.OnDataCalled.AsManagedObject(typeof(bool));

                Assert.IsTrue(initializeCalled);
                Assert.IsTrue(onDataCalled);

                var currentSlice = PythonQB.CurrentSlice.AsManagedObject(typeof(Slice));
                Assert.AreEqual(new DateTime(2013, 10, 7, 9, 31, 0), currentSlice.Time);
                Assert.AreEqual(153.01779001600, currentSlice.Values[0].Close);
            }
        }
    }
}
