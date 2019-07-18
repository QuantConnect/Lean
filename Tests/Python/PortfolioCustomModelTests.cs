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
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using System;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PortfolioCustomModelTests
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SetMarginCallModelSuccess(bool isChild)
        {
            var algorithm = new QCAlgorithm();
            var portfolio = algorithm.Portfolio;
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(new DateTime(2018, 8, 20, 15, 0, 0));
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            
            var spy = algorithm.AddEquity("SPY", Resolution.Daily);
            spy.SetMarketPrice(new Tick(algorithm.Time, Symbols.SPY, 100m, 100m));

            // Test two custom buying power models.
            // The first inherits from C# SecurityMarginModel and the other is 100% python
            var code = isChild
                ? CreateCustomMarginCallModelFromSecurityMarginModelCode()
                : CreateCustomMarginCallModelCode();

            portfolio.SetMarginCallModel(CreateCustomMarginCallModel(code, portfolio));
            Assert.IsAssignableFrom<MarginCallModelPythonWrapper>(portfolio.MarginCallModel);

            var marginCallOrder = portfolio.MarginCallModel.GenerateMarginCallOrder(spy, 0m, 0m, 0m);
            Assert.IsNotNull(marginCallOrder);
            Assert.AreEqual(0, marginCallOrder.Quantity);

            bool issueMarginCallWarning;
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out issueMarginCallWarning);

            if (isChild)
            {
                Assert.IsFalse(issueMarginCallWarning);
                Assert.AreEqual(0, marginCallOrders.Count);
            }
            else
            {
                Assert.IsTrue(issueMarginCallWarning);
                Assert.AreEqual(3, marginCallOrders.Count);
            }
        }

        [Test]
        public void SetMarginCallModelFails()
        {
            var algorithm = new QCAlgorithm();
            var portfolio = algorithm.Portfolio;

            // Renaming GetMarginCall will cause a NotImplementedException exception
            var code = CreateCustomMarginCallModelCode();
            code = code.Replace("GetMarginCall", "SetMarginCall");
            var pyObject = CreateCustomMarginCallModel(code, portfolio);
            Assert.Throws<NotImplementedException>(() => portfolio.SetMarginCallModel(CreateCustomMarginCallModel(code, portfolio)));
        }

        private PyObject CreateCustomMarginCallModel(string code, SecurityPortfolioManager portfolio)
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString("CustomMarginCallModel", code);
                dynamic CustomMarginCallModel = module.GetAttr("CustomMarginCallModel");
                return CustomMarginCallModel(portfolio, null);
            }
        }

        private string CreateCustomMarginCallModelCode() => @"
import os, sys
sys.path.append(os.getcwd())

from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *
from QuantConnect.Securities import *
from QuantConnect.Orders import *

class CustomMarginCallModel:
    def __init__(self, portfolio, defaultOrderProperties):
        self.portfolio = portfolio
        self.defaultOrderProperties = defaultOrderProperties

    def ExecuteMarginCall(self, generatedMarginCallOrders):
        return []
    
    def GenerateMarginCallOrder(self, security, netLiquidationValue, totalMargin, maintenanceMarginRequirement):
        time = Extensions.ConvertToUtc(security.LocalTime, security.Exchange.TimeZone)
        quantity = netLiquidationValue / security.Price
        return SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, time, 'Margin Call', None)

    def GetMarginCallOrders(self, issueMarginCallWarning):
        issueMarginCallWarning = True
        spy = self.portfolio.Securities['SPY']
        totalPortfolioValue = self.portfolio.TotalPortfolioValue
        totalMarginUsed = self.portfolio.TotalMarginUsed

        order = self.GenerateMarginCallOrder(spy, totalPortfolioValue, totalMarginUsed, 0)
        
        return [order, order, order], issueMarginCallWarning";

        private string CreateCustomMarginCallModelFromSecurityMarginModelCode() => @"
import os, sys
sys.path.append(os.getcwd())

from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *
from QuantConnect.Securities import *
from QuantConnect.Orders import *

class CustomMarginCallModel(DefaultMarginCallModel):
    def __init__(self, portfolio, defaultOrderProperties):
        self.porfolio = portfolio
        self.defaultOrderProperties = defaultOrderProperties
        super().__init__(portfolio, defaultOrderProperties)

    def GenerateMarginCallOrder(self, security, netLiquidationValue, totalMargin, maintenanceMarginRequirement):
        time = Extensions.ConvertToUtc(security.LocalTime, security.Exchange.TimeZone)
        quantity = netLiquidationValue / security.Price
        return SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, time, 'Margin Call', None)";
    }
}