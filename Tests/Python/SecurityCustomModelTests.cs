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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Tests.Common.Securities;
using System;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class SecurityCustomModelTests
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SetBuyingPowerModelSuccess(bool isChild)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(new DateTime(2018, 8, 20, 15, 0, 0));
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());

            var spy = algorithm.AddEquity("SPY", Resolution.Daily);
            spy.SetMarketPrice(new Tick(algorithm.Time, Symbols.SPY, 100m, 100m));

            // Test two custom buying power models.
            // The first inherits from C# SecurityMarginModel and the other is 100% python
            var code = isChild
                ? CreateCustomBuyingPowerModelFromSecurityMarginModelCode()
                : CreateCustomBuyingPowerModelCode();

            spy.SetBuyingPowerModel(CreateCustomBuyingPowerModel(code));
            Assert.IsAssignableFrom<BuyingPowerModelPythonWrapper>(spy.MarginModel);
            Assert.AreEqual(1, spy.MarginModel.GetLeverage(spy));

            spy.SetLeverage(2);
            Assert.AreEqual(2, spy.MarginModel.GetLeverage(spy));

            var quantity = algorithm.CalculateOrderQuantity(spy.Symbol, 1m);
            Assert.AreEqual(isChild ? 100 : 200, quantity);
        }

        [Test]
        public void SetBuyingPowerModelFails()
        {
            var spy = GetSecurity<Equity>(Symbols.SPY, Resolution.Daily);

            // Renaming GetBuyingPower will cause a NotImplementedException exception
            var code = CreateCustomBuyingPowerModelCode();
            code = code.Replace("GetBuyingPower", "SetBuyingPower");
            var pyObject = CreateCustomBuyingPowerModel(code);
            Assert.Throws<NotImplementedException>(() => spy.SetBuyingPowerModel(pyObject));
        }

        private PyObject CreateCustomBuyingPowerModel(string code)
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString("CustomBuyingPowerModel", code);
                return module.GetAttr("CustomBuyingPowerModel").Invoke();
            }
        }

        private string CreateCustomBuyingPowerModelCode() => @"
import os, sys
sys.path.append(os.getcwd())

from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *
from QuantConnect.Securities import *

class CustomBuyingPowerModel:
    def __init__(self):
        self.margin = 1.0

    def GetBuyingPower(self, context):
        return BuyingPower(context.Portfolio.MarginRemaining)

    def GetLeverage(self, security):
        return 1.0 / self.margin

    def GetMaximumOrderQuantityForTargetValue(self, context):
        return GetMaximumOrderQuantityForTargetValueResult(200)

    def GetReservedBuyingPowerForPosition(self, context):
        return ReservedBuyingPowerForPosition(context.Security.Holdings.AbsoluteHoldingsCost * self.margin)

    def HasSufficientBuyingPowerForOrder(self, context):
        return HasSufficientBuyingPowerForOrderResult(True)

    def SetLeverage(self, security, leverage):
        self.margin = 1.0 / float(leverage)";

        private string CreateCustomBuyingPowerModelFromSecurityMarginModelCode() => @"
import os, sys
sys.path.append(os.getcwd())

from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *
from QuantConnect.Securities import *

class CustomBuyingPowerModel(SecurityMarginModel):
    def GetMaximumOrderQuantityForTargetValue(self, context):
        return GetMaximumOrderQuantityForTargetValueResult(100)";

        private Security GetSecurity<T>(Symbol symbol, Resolution resolution)
        {
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);

            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }
    }
}