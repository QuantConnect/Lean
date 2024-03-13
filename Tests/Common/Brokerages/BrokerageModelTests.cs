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
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Python;
using QuantConnect.Securities;
using Moq;
using QuantConnect.Orders.Fills;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Benchmarks;
using QuantConnect.Data;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Data.Shortable;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class BrokerageModelTests
    {
        [TestCaseSource(nameof(GetBrokerageNameTestCases))]
        public void GetsCorrectBrokerageNameFromBrokerageInstance(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            Assert.AreEqual(brokerageName, BrokerageModel.GetBrokerageName(brokerage));
        }

        [TestCaseSource(nameof(GetCustomBrokerageNameTestCases))]
        public void GetsCorrectCustomBrokerageNameFromBrokerageInstance_CSharp(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            Assert.AreEqual(brokerageName, BrokerageModel.GetBrokerageName(brokerage));
        }

        [TestCaseSource(nameof(GetBrokerageNameTestCases))]
        public void GetsCorrectCustomBrokerageNameFromBrokerageInstance_Python(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel({brokerage.GetType().Name}):
    pass
                ").GetAttr("CustomBrokerageModel");

                Assert.AreEqual(brokerageName, BrokerageModel.GetBrokerageName(new BrokerageModelPythonWrapper(PyCustomBrokerageModel())));
            }
        }

        [Test]
        public void CustomPythonBrokerageCanSubmitOrderMethodFailsWhenNoTupleIsReturned()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel(DefaultBrokerageModel):
    def CanSubmitOrder(self, security: SecurityType, order: Order, message: BrokerageMessageEvent):
        return True
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "");
                Assert.Throws<ArgumentException>(() => model.CanSubmitOrder(security, _order.Object, out message));
            }
        }

        [Test]
        public void CustomPythonBrokerageCanSubmitOrderMethodDoesNotFailWhenTupleIsReturned()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel(DefaultBrokerageModel):
    def CanSubmitOrder(self, security: SecurityType, order: Order, message: BrokerageMessageEvent):
        message = None
        return True, message
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "");
                var result = false;
                Assert.DoesNotThrow(() => result = model.CanSubmitOrder(security, _order.Object, out message));
                Assert.IsTrue(result);
                Assert.IsNull(message);
            }
        }

        [Test]
        public void CustomPythonBrokerageCanUpdateOrderMethodFailsWhenNoTupleIsReturned()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel(DefaultBrokerageModel):
    def CanUpdateOrder(self, security: SecurityType, order: Order, request: UpdateOrderRequest, message: BrokerageMessageEvent):
        return False
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var updateRequest = new UpdateOrderRequest(DateTime.Now, 1, new UpdateOrderFields());
                var message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "");
                Assert.Throws<ArgumentException>(() => model.CanUpdateOrder(security, _order.Object, updateRequest, out message));
            }
        }

        [Test]
        public void CustomPythonBrokerageCanUpdateOrderMethodDoesNotFailWhenTupleReturned()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel(DefaultBrokerageModel):
    def CanUpdateOrder(self, security: SecurityType, order: Order, request: UpdateOrderRequest, message: BrokerageMessageEvent):
        message = BrokerageMessageEvent(BrokerageMessageType.Information, """", ""Order can not be updated"")
        return False, message
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var updateRequest = new UpdateOrderRequest(DateTime.Now, 1, new UpdateOrderFields());
                var result = true;
                var message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "");
                Assert.DoesNotThrow(() => result = model.CanUpdateOrder(security, _order.Object, updateRequest, out message));
                Assert.IsFalse(result);
                Assert.AreEqual("Order can not be updated", message.Message);
            }
        }

        [TestCaseSource(nameof(GetBrokerageNameTestCases))]
        public void CustomPythonBrokerageCanSubmitOrderMethodDoesNotFailWhenIsNotOverriden(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel({brokerage.GetType().Name}):
    pass
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "Initial Message");
                Assert.DoesNotThrow(() => model.CanSubmitOrder(security, _order.Object, out message));

                if (message != null)
                {
                    Assert.AreNotEqual("Initial Message", message.Message);
                }
            }
        }

        [TestCaseSource(nameof(GetBrokerageNameTestCases))]
        public void CustomPythonBrokerageCanUpdateOrderMethodDoesNotFailWhenIsNotOverriden(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel({brokerage.GetType().Name}):
    pass
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var updateRequest = new UpdateOrderRequest(DateTime.Now, 1, new UpdateOrderFields());
                var message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "Initial Message");
                Assert.DoesNotThrow(() => model.CanUpdateOrder(security, _order.Object, updateRequest, out message));

                if (message != null)
                {
                    Assert.AreNotEqual("Initial Message", message.Message);
                }
            }
        }

        [TestCaseSource(nameof(GetBrokerageBuyingPowerModel))]
        public void GetsCorrectBuyingPowerModelForSecurityAndAccountType(IBrokerageModel brokerage, AccountType accountType, SecurityType securityType, Type type)
        {
            var security = securityType == SecurityType.Equity
                ? GetSecurity(Symbols.SPY)
                : GetSecurity(Symbols.EURUSD);

            var buyingPowerModel = brokerage?.GetBuyingPowerModel(security);

            Assert.AreEqual(buyingPowerModel.GetType(), type);
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomPythonFillModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomFillModel(ImmediateFillModel):
    def __init__(self):
        super().__init__()

    def MarketFill(self, asset, order):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetFillModel(self, security):
        return CustomFillModel()
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var fillModel = model.GetFillModel(security);
                Assert.AreEqual(typeof(FillModelPythonWrapper), fillModel.GetType());
                var ex = Assert.Throws<PythonException>(() => ((dynamic)fillModel).MarketFill(security, new Mock<MarketOrder>().Object));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCSharpFillModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetFillModel(self, security):
        return ImmediateFillModel()
                ").GetAttr("CustomBrokerageModel");

                var security = GetSecurity(Symbols.SPY);
                security.SetLocalTimeKeeper(new LocalTimeKeeper(DateTime.Now, DateTimeZone.Utc));
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var fillModel = model.GetFillModel(security);
                Assert.AreEqual(typeof(ImmediateFillModel), fillModel.GetType());
                var order = new Mock<MarketOrder>();
                var subscriptionDataConfigProvider = new Mock<ISubscriptionDataConfigProvider>();
                var securitiesForOrders = new Dictionary<Order, Security>() { { order.Object, security} };
                var fillModelParameters = new FillModelParameters(security, order.Object, subscriptionDataConfigProvider.Object, TimeSpan.Zero, securitiesForOrders);
                var result = fillModel.Fill(fillModelParameters);
                foreach( var entry in result)
                {
                    Assert.AreEqual(OrderStatus.Filled, entry.Status);
                }
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomBenchmark()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBenchmarkModel:
    def Evaluate(self, time):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetBenchmark(self, securities):
        return CustomBenchmarkModel()
                ").GetAttr("CustomBrokerageModel");
                var timeKeeper = new TimeKeeper(DateTime.Now);
                var securityManager = new SecurityManager(timeKeeper);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var benchmarkModel = model.GetBenchmark(securityManager);
                Assert.AreEqual(typeof(BenchmarkPythonWrapper), benchmarkModel.GetType());
                var ex = Assert.Throws<PythonException>(() => ((dynamic)benchmarkModel).Evaluate(DateTime.Now));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpBenchmark()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetBenchmark(self, securities):
        return super().GetBenchmark(securities)
                ").GetAttr("CustomBrokerageModel");
                var timeKeeper = new TimeKeeper(DateTime.Now);
                var subscriptionManager = new SubscriptionManager(timeKeeper);
                var dataManager = new DataManagerStub();
                subscriptionManager.SetDataManager(dataManager);
                var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                var securityService = dataManager.SecurityService;
                var securityManager = new SecurityManager(timeKeeper);
                securityManager.SetSecurityService((SecurityService)securityService);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var benchmarkModel = model.GetBenchmark(securityManager);
                Assert.AreEqual(typeof(SecurityBenchmark), benchmarkModel.GetType());
                var result = ((dynamic)benchmarkModel).Evaluate(DateTime.Now);
                Assert.AreEqual(0m, result);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomFeeModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomFeeModel:
    def GetOrderFee(self, security, order):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetFeeModel(self, securities):
        return CustomFeeModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var feeModel = model.GetFeeModel(security);
                Assert.AreEqual(typeof(FeeModelPythonWrapper), feeModel.GetType());
                var order = new Mock<Order>();
                var orderParameters = new Mock<OrderFeeParameters>(security, order.Object);
                var ex = Assert.Throws<PythonException>(() => ((dynamic)feeModel).GetOrderFee(orderParameters.Object));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpFeeModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetFeeModel(self, securities):
        return FeeModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var feeModel = model.GetFeeModel(security);
                Assert.AreEqual(typeof(FeeModel), feeModel.GetType());
                var order = new Mock<Order>();
                var orderParameters = new Mock<OrderFeeParameters>(security, order.Object);
                var result = ((FeeModel)feeModel).GetOrderFee(orderParameters.Object);
                Assert.AreEqual(Currencies.USD, result.Value.Currency);
                Assert.AreEqual(0m, result.Value.Amount);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomSettlementModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomSettlementModel:
    def ApplyFunds(self, parameters):
        raise ValueError(""Pepe"")

    def Scan(self, parameters):
        raise ValueError(""Pepe2"")

    def GetUnsettledCash(self):
        raise ValueError(""Pepe3"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetSettlementModel(self, securities):
        return CustomSettlementModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var settlementModel = model.GetSettlementModel(security);
                Assert.AreEqual(typeof(SettlementModelPythonWrapper), settlementModel.GetType());
                var algorithm = new AlgorithmStub();
                algorithm.SetDateTime(DateTime.Now);
                var portfolio = algorithm.Portfolio;
                var appyFundsParameters = new ApplyFundsSettlementModelParameters(portfolio, security, DateTime.Now, new CashAmount(1000, Currencies.USD), null);
                var ex = Assert.Throws<PythonException>(() => ((dynamic)settlementModel).ApplyFunds(appyFundsParameters));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
                var scanParameters = new ScanSettlementModelParameters(portfolio, security, DateTime.UtcNow);
                ex = Assert.Throws<PythonException>(() => ((dynamic)settlementModel).Scan(scanParameters));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe2", ex.Message);
                ex = Assert.Throws<PythonException>(() => ((dynamic)settlementModel).GetUnsettledCash());
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe3", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpSettlementModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetSettlementModel(self, security):
        return ImmediateSettlementModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var settlementModel = model.GetSettlementModel(security);
                Assert.AreEqual(typeof(ImmediateSettlementModel), settlementModel.GetType());
                var algorithm = new AlgorithmStub();
                algorithm.SetDateTime(DateTime.Now);
                var portfolio = algorithm.Portfolio;
                var appyFundsParameters = new ApplyFundsSettlementModelParameters(portfolio, security, DateTime.Now, new CashAmount(1000, Currencies.USD), null);
                Assert.DoesNotThrow(() => ((dynamic)settlementModel).ApplyFunds(appyFundsParameters));
                var scanParameters = new ScanSettlementModelParameters(portfolio, security, DateTime.UtcNow);
                Assert.DoesNotThrow(() => ((dynamic)settlementModel).Scan(scanParameters));
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomSlippageModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomSlippageModel:
    def GetSlippageApproximation(self, asset, order):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetSlippageModel(self, security):
        return CustomSlippageModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var slippageModel = model.GetSlippageModel(security);
                Assert.AreEqual(typeof(SlippageModelPythonWrapper), slippageModel.GetType());
                var order = new Mock<Order>();
                var ex = Assert.Throws<PythonException>(() => ((dynamic)slippageModel).GetSlippageApproximation(security, order.Object));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpSlippageModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetSlippageModel(self, security):
        return NullSlippageModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var slippageModel = model.GetSlippageModel(security);
                Assert.AreEqual(typeof(NullSlippageModel), slippageModel.GetType());
                var order = new Mock<Order>();
                var result = ((dynamic)slippageModel).GetSlippageApproximation(security, order.Object);
                Assert.AreEqual(0m, result);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomBuyingPowerModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBuyingPowerModel(BuyingPowerModel):
    def GetLeverage(self, security):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetBuyingPowerModel(self, security):
        return CustomBuyingPowerModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var buyingPowerModel = model.GetBuyingPowerModel(security);
                Assert.AreEqual(typeof(BuyingPowerModelPythonWrapper), buyingPowerModel.GetType());
                var ex = Assert.Throws<PythonException>(() => ((dynamic)buyingPowerModel).GetLeverage(security));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpBuyingPowerModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetBuyingPowerModel(self, security):
        return BuyingPowerModel(1)
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var buyingPowerModel = model.GetBuyingPowerModel(security);
                Assert.AreEqual(typeof(BuyingPowerModel), buyingPowerModel.GetType());
                var result = ((dynamic)buyingPowerModel).GetLeverage(security);
                Assert.AreEqual(1, result);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomShortableProvider()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomShortableProvider:
    def FeeRate(self, symbol, localTime):
        raise ValueError(""Pepe"")
    def RebateRate(self, symbol, localTime):
        raise ValueError(""Pepe"")
    def ShortableQuantity(self, symbol, localTime):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetShortableProvider(self, security):
        return CustomShortableProvider()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var shortableProvider = model.GetShortableProvider(security);
                Assert.AreEqual(typeof(ShortableProviderPythonWrapper), shortableProvider.GetType());
                var ex = Assert.Throws<PythonException>(() => ((dynamic)shortableProvider).ShortableQuantity(security.Symbol, DateTime.Now));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpShortableProvider()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetShortableProvider(self, security):
        return NullShortableProvider()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var shortableProvider = model.GetShortableProvider(security);
                Assert.AreEqual(typeof(NullShortableProvider), shortableProvider.GetType());
                var result = ((dynamic)shortableProvider).ShortableQuantity(security.Symbol, DateTime.Now);
                Assert.IsNull(result);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCustomMarginInterestRateModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomMarginInterestRateModel:
    def ApplyMarginInterestRate(self, parameters):
        raise ValueError(""Pepe"")

class CustomBrokerageModel(DefaultBrokerageModel):
    def GetMarginInterestRateModel(self, security):
        return CustomMarginInterestRateModel()
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var marginInterestRateModel = model.GetMarginInterestRateModel(security);
                Assert.AreEqual(typeof(MarginInterestRateModelPythonWrapper), marginInterestRateModel.GetType());
                var parameters = new MarginInterestRateParameters(security, DateTime.Now);
                var ex = Assert.Throws<PythonException>(() => ((dynamic)marginInterestRateModel).ApplyMarginInterestRate(parameters));
                Assert.AreEqual("ValueError", ex.Type.Name);
                Assert.AreEqual("Pepe", ex.Message);
            }
        }

        [Test]
        public void BrokerageModelPythonWrapperWorksWithCsharpMarginInterestRateModel()
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *
class CustomBrokerageModel(DefaultBrokerageModel):
    def GetMarginInterestRateModel(self, security):
        return MarginInterestRateModel.Null
                ").GetAttr("CustomBrokerageModel");
                var security = GetSecurity(Symbols.SPY);
                var model = new BrokerageModelPythonWrapper(PyCustomBrokerageModel());
                var marginInterestRate = model.GetMarginInterestRateModel(security);
                Assert.AreEqual("QuantConnect.Securities.MarginInterestRateModel+NullMarginInterestRateModel", marginInterestRate.GetType().ToString());
                var parameters = new MarginInterestRateParameters(security, DateTime.Now);
                Assert.DoesNotThrow(() => ((IMarginInterestRateModel)marginInterestRate).ApplyMarginInterestRate(parameters));
            }
        }

        private static Security GetSecurity(Symbol symbol) =>
        new(symbol,
            SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
            new Cash(Currencies.USD, 0, 1),
            SymbolProperties.GetDefault(Currencies.USD),
            ErrorCurrencyConverter.Instance,
            RegisteredSecurityDataTypesProvider.Null,
        new SecurityCache());

        private static Mock<MarketOrder> _order = new Mock<MarketOrder>
        {
            Object =
                {
                    Quantity = 100
                }
        };

        private static TestCaseData[] GetBrokerageNameTestCases()
        {
            return new[]
            {
                new TestCaseData(new InteractiveBrokersBrokerageModel(), BrokerageName.InteractiveBrokersBrokerage),
                new TestCaseData(new TradierBrokerageModel(), BrokerageName.TradierBrokerage),
                new TestCaseData(new OandaBrokerageModel(), BrokerageName.OandaBrokerage),
                new TestCaseData(new FxcmBrokerageModel(), BrokerageName.FxcmBrokerage),
                new TestCaseData(new BitfinexBrokerageModel(), BrokerageName.Bitfinex),
                new TestCaseData(new BinanceUSBrokerageModel(), BrokerageName.BinanceUS),
                new TestCaseData(new BinanceBrokerageModel(), BrokerageName.Binance),
                new TestCaseData(new CoinbaseBrokerageModel(), BrokerageName.Coinbase),
                new TestCaseData(new AlphaStreamsBrokerageModel(), BrokerageName.AlphaStreams),
                new TestCaseData(new ZerodhaBrokerageModel(), BrokerageName.Zerodha),
                new TestCaseData(new AxosClearingBrokerageModel(), BrokerageName.Axos),
                new TestCaseData(new TradingTechnologiesBrokerageModel(), BrokerageName.TradingTechnologies),
                new TestCaseData(new SamcoBrokerageModel(), BrokerageName.Samco),
                new TestCaseData(new KrakenBrokerageModel(), BrokerageName.Kraken),
                new TestCaseData(new ExanteBrokerageModel(), BrokerageName.Exante),
                new TestCaseData(new FTXUSBrokerageModel(), BrokerageName.FTXUS),
                new TestCaseData(new FTXBrokerageModel(), BrokerageName.FTX),
                new TestCaseData(new BybitBrokerageModel(), BrokerageName.Bybit),
                new TestCaseData(new DefaultBrokerageModel(), BrokerageName.Default)
            };
        }

        private class CustomInteractiveBrokersBrokerageModel : InteractiveBrokersBrokerageModel {}
        private class CustomTradierBrokerageModel : TradierBrokerageModel {}
        private class CustomOandaBrokerageModel : OandaBrokerageModel {}
        private class CustomFxcmBrokerageModel : FxcmBrokerageModel {}
        private class CustomBitfinexBrokerageModel : BitfinexBrokerageModel {}
        private class CustomBinanceUSBrokerageModel : BinanceUSBrokerageModel {}
        private class CustomBinanceBrokerageModel : BinanceBrokerageModel {}
        private class CustomCoinbaseBrokerageModel : CoinbaseBrokerageModel {}
        private class CustomAlphaStreamsBrokerageModel : AlphaStreamsBrokerageModel {}
        private class CustomZerodhaBrokerageModel : ZerodhaBrokerageModel {}
        private class CustomAxosBrokerageModel : AxosClearingBrokerageModel {}
        private class CustomTradingTechnologiesBrokerageModel : TradingTechnologiesBrokerageModel {}
        private class CustomSamcoBrokerageModel : SamcoBrokerageModel {}
        private class CustomKrakenBrokerageModel : KrakenBrokerageModel {}
        private class CustomExanteBrokerageModel : ExanteBrokerageModel {}
        private class CustomFTXUSBrokerageModel : FTXUSBrokerageModel {}
        private class CustomFTXBrokerageModel : FTXBrokerageModel {}
        private  class CustomBybitBrokerageModel : BybitBrokerageModel { }
        private class CustomDefaultBrokerageModel : DefaultBrokerageModel {}

        private static TestCaseData[] GetCustomBrokerageNameTestCases()
        {
            return new[]
            {
                new TestCaseData(new CustomInteractiveBrokersBrokerageModel(), BrokerageName.InteractiveBrokersBrokerage),
                new TestCaseData(new CustomTradierBrokerageModel(), BrokerageName.TradierBrokerage),
                new TestCaseData(new CustomOandaBrokerageModel(), BrokerageName.OandaBrokerage),
                new TestCaseData(new CustomFxcmBrokerageModel(), BrokerageName.FxcmBrokerage),
                new TestCaseData(new CustomBitfinexBrokerageModel(), BrokerageName.Bitfinex),
                new TestCaseData(new CustomBinanceUSBrokerageModel(), BrokerageName.BinanceUS),
                new TestCaseData(new CustomBinanceBrokerageModel(), BrokerageName.Binance),
                new TestCaseData(new CustomCoinbaseBrokerageModel(), BrokerageName.Coinbase),
                new TestCaseData(new CustomAlphaStreamsBrokerageModel(), BrokerageName.AlphaStreams),
                new TestCaseData(new CustomZerodhaBrokerageModel(), BrokerageName.Zerodha),
                new TestCaseData(new CustomAxosBrokerageModel(), BrokerageName.Axos),
                new TestCaseData(new CustomTradingTechnologiesBrokerageModel(), BrokerageName.TradingTechnologies),
                new TestCaseData(new CustomSamcoBrokerageModel(), BrokerageName.Samco),
                new TestCaseData(new CustomKrakenBrokerageModel(), BrokerageName.Kraken),
                new TestCaseData(new CustomExanteBrokerageModel(), BrokerageName.Exante),
                new TestCaseData(new CustomFTXUSBrokerageModel(), BrokerageName.FTXUS),
                new TestCaseData(new CustomFTXBrokerageModel(), BrokerageName.FTX),
                new TestCaseData(new CustomBybitBrokerageModel(), BrokerageName.Bybit),
                new TestCaseData(new CustomDefaultBrokerageModel(), BrokerageName.Default)
            };
        }

        private static TestCaseData[] GetBrokerageBuyingPowerModel()
        {
            return new[]
            {
                new TestCaseData(new InteractiveBrokersBrokerageModel(AccountType.Cash), AccountType.Cash, SecurityType.Equity, typeof(SecurityMarginModel)),
                new TestCaseData(new InteractiveBrokersBrokerageModel(AccountType.Margin), AccountType.Margin, SecurityType.Equity, typeof(SecurityMarginModel)),
                new TestCaseData(new InteractiveBrokersBrokerageModel(AccountType.Cash), AccountType.Cash, SecurityType.Forex, typeof(CashBuyingPowerModel)),
                new TestCaseData(new InteractiveBrokersBrokerageModel(AccountType.Margin), AccountType.Margin, SecurityType.Forex, typeof(SecurityMarginModel)),
            };
        }
    }
}
