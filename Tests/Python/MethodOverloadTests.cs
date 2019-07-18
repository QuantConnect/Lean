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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Python;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class MethodOverloadTests
    {
        private dynamic _algorithm;

        /// <summary>
        /// Run before every test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            PythonInitializer.Initialize();

            using (Py.GIL())
            {
                var module = Py.Import("Test_MethodOverload");
                _algorithm = module.GetAttr("Test_MethodOverload").Invoke();
                // this is required else will get a 'RuntimeBinderException' because fails to match constructor method
                dynamic algo = _algorithm.AsManagedObject((Type)_algorithm.GetPythonType().AsManagedObject(typeof(Type)));
                _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
                _algorithm.Initialize();
            }
        }

        [Test]
        public void CallPlotTests()
        {
            using (Py.GIL())
            {
                // self.Plot('NUMBER', 0.1)
                Assert.DoesNotThrow(() => _algorithm.call_plot_number_test());

                // self.Plot('STD', self.std), where self.sma = self.SMA('SPY', 20)
                Assert.DoesNotThrow(() => _algorithm.call_plot_sma_test());

                // self.Plot('SMA', self.sma), where self.std = self.STD('SPY', 20)
                Assert.DoesNotThrow(() => _algorithm.call_plot_std_test());

                // self.Plot("ERROR", self.Name), where self.Name is IAlgorithm.Name: string
                Assert.Throws<PythonException>(() => _algorithm.call_plot_throw_test());

                // self.Plot("ERROR", self.Portfolio), where self.Portfolio is IAlgorithm.Portfolio: instance of SecurityPortfolioManager
                Assert.Throws<PythonException>(() => _algorithm.call_plot_throw_managed_test());

                // self.Plot("ERROR", self.a), where self.a is an instance of a python object
                Assert.Throws<PythonException>(() => _algorithm.call_plot_throw_pyobject_test());
            }
        }
    }
}