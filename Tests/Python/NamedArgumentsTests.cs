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
using QuantConnect.Algorithm;
using QuantConnect.Securities.Equity;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class NamedArgumentsTests
    {
        [Test]
        public void AddEquityTest()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            using (Py.GIL())
            {
                // Test function that will used named args in Python -> C#
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    "def test(algorithm):\n"
                        + "   aapl = algorithm.AddEquity(ticker='AAPL')\n"
                        + "   return aapl\n"
                );

                var testFunction = module.GetAttr("test");
                var equity = testFunction.Invoke(algorithm.ToPython()).As<Equity>();

                Assert.AreEqual("AAPL", equity.Symbol.Value);
            }
        }
    }
}
