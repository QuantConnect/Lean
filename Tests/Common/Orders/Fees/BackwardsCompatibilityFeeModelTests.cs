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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class BackwardsCompatibilityFeeModelTests
    {
        private Security _security;
        private static DateTime orderDateTime;

        [SetUp]
        public void SetUp()
        {
            _security = SecurityTests.GetSecurity();
            orderDateTime = new DateTime(2017, 2, 2, 13, 0, 0);
            var reference = DateTime.Now;
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            _security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
        }

        #region Python

        [Test]
        public void OldFeeModelModel_GetOrderFee_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "class CustomFeeModel:\n" +
                    "   def __init__(self):\n" +
                    "       self.CalledGetOrderFee = False\n" +
                    "   def GetOrderFee(self, security, order):\n" +
                    "       self.CalledGetOrderFee = True\n" +
                    "       return 15");

                var customFeeModel = module.GetAttr("CustomFeeModel").Invoke();
                var wrapper = new FeeModelPythonWrapper(customFeeModel);

                var result = wrapper.GetOrderFee(new OrderFeeParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime)
                ));

                bool called;
                customFeeModel.GetAttr("CalledGetOrderFee").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(15, result.Value.Amount);
                Assert.AreEqual(Currencies.USD, result.Value.Currency);
            }
        }

        [Test]
        public void NewFeeModelModel_GetOrderFee_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fees import *\n" +
                    "from QuantConnect.Securities import *\n" +
                    "class CustomFeeModel(FeeModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.CalledGetOrderFee = False\n" +
                    "   def GetOrderFee(self, parameters):\n" +
                    "       self.CalledGetOrderFee = True\n" +
                    "       return OrderFee(CashAmount(15, \"USD\"))");

                var customFeeModel = module.GetAttr("CustomFeeModel").Invoke();
                var wrapper = new FeeModelPythonWrapper(customFeeModel);

                var result = wrapper.GetOrderFee(new OrderFeeParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime)
                ));

                bool called;
                customFeeModel.GetAttr("CalledGetOrderFee").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(15, result.Value.Amount);
                Assert.AreEqual(Currencies.USD, result.Value.Currency);
            }
        }
        #endregion
    }
}
