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
                new TestCaseData(new GDAXBrokerageModel(), BrokerageName.GDAX),
                new TestCaseData(new AlphaStreamsBrokerageModel(), BrokerageName.AlphaStreams),
                new TestCaseData(new ZerodhaBrokerageModel(), BrokerageName.Zerodha),
                new TestCaseData(new AtreyuBrokerageModel(), BrokerageName.Atreyu),
                new TestCaseData(new TradingTechnologiesBrokerageModel(), BrokerageName.TradingTechnologies),
                new TestCaseData(new SamcoBrokerageModel(), BrokerageName.Samco),
                new TestCaseData(new KrakenBrokerageModel(), BrokerageName.Kraken),
                new TestCaseData(new ExanteBrokerageModel(), BrokerageName.Exante),
                new TestCaseData(new FTXUSBrokerageModel(), BrokerageName.FTXUS),
                new TestCaseData(new FTXBrokerageModel(), BrokerageName.FTX),
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
        private class CustomGDAXBrokerageModel : GDAXBrokerageModel {}
        private class CustomAlphaStreamsBrokerageModel : AlphaStreamsBrokerageModel {}
        private class CustomZerodhaBrokerageModel : ZerodhaBrokerageModel {}
        private class CustomAtreyuBrokerageModel : AtreyuBrokerageModel {}
        private class CustomTradingTechnologiesBrokerageModel : TradingTechnologiesBrokerageModel {}
        private class CustomSamcoBrokerageModel : SamcoBrokerageModel {}
        private class CustomKrakenBrokerageModel : KrakenBrokerageModel {}
        private class CustomExanteBrokerageModel : ExanteBrokerageModel {}
        private class CustomFTXUSBrokerageModel : FTXUSBrokerageModel {}
        private class CustomFTXBrokerageModel : FTXBrokerageModel {}
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
                new TestCaseData(new CustomGDAXBrokerageModel(), BrokerageName.GDAX),
                new TestCaseData(new CustomAlphaStreamsBrokerageModel(), BrokerageName.AlphaStreams),
                new TestCaseData(new CustomZerodhaBrokerageModel(), BrokerageName.Zerodha),
                new TestCaseData(new CustomAtreyuBrokerageModel(), BrokerageName.Atreyu),
                new TestCaseData(new CustomTradingTechnologiesBrokerageModel(), BrokerageName.TradingTechnologies),
                new TestCaseData(new CustomSamcoBrokerageModel(), BrokerageName.Samco),
                new TestCaseData(new CustomKrakenBrokerageModel(), BrokerageName.Kraken),
                new TestCaseData(new CustomExanteBrokerageModel(), BrokerageName.Exante),
                new TestCaseData(new CustomFTXUSBrokerageModel(), BrokerageName.FTXUS),
                new TestCaseData(new CustomFTXBrokerageModel(), BrokerageName.FTX),
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
