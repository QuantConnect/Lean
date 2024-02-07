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
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Orders;
using Moq;
using static QuantConnect.Tests.Engine.PerformanceBenchmarkAlgorithms;
using QuantConnect.Python;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class SettlementModelPythonWrapperTests
    {
        [Test]
        public void GetsDefaultUnsettledCashFromNone()
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    @"
class CustomSettlementModel:
    def ApplyFunds(self, parameters):
        pass

    def Scan(self, parameters):
        pass

    def GetUnsettledCash(self):
        return None
        ");

                var settlementModel = new SettlementModelPythonWrapper(testModule.GetAttr("CustomSettlementModel").Invoke());
                var result = settlementModel.GetUnsettledCash();
                Assert.AreEqual(default(CashAmount), result);

            }
        }
    }
}
