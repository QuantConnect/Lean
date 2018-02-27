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

using NUnit.Framework;
using Python.Runtime;
using System;
using System.IO;

namespace QuantConnect.Tests.Python
{
    [TestFixture, Ignore]
    public class MethodOverloadTests
    {
        private dynamic _algorithm;

        /// <summary>
        /// Run before every test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var pythonPath = new DirectoryInfo("RegressionAlgorithms");
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath.FullName);

            using (Py.GIL())
            {
                var module = Py.Import("Test_MethodOverload");
                _algorithm = module.GetAttr("Test_MethodOverload").Invoke();
                _algorithm.Initialize();
            }
        }

        [Test]
        public void CallPlotStdTest()
        {
            Assert.DoesNotThrow(() => _algorithm.call_plot_std_test());
        }
    }
}