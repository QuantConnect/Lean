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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public class BackwardsCompatibilityFillModelsTests
    {
        private SubscriptionDataConfig _config;
        private Security _security;
        private static OrderEvent orderEvent;
        private static DateTime orderDateTime;

        [SetUp]
        public void SetUp()
        {
            _config = SecurityTests.CreateTradeBarConfig();
            _security = SecurityTests.GetSecurity();
            orderDateTime = new DateTime(2017, 2, 2, 13, 0, 0);
            orderEvent = new OrderEvent(
                99,
                _security.Symbol,
                orderDateTime,
                OrderStatus.Submitted,
                OrderDirection.Buy,
                1,
                1,
                new OrderFee(new CashAmount(1, Currencies.USD))
            );
            var reference = DateTime.Now;
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            _security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
        }

        #region InheritImmediateFillModel

        [Test]
        public void InheritImmediateFillModel_MarketFill()
        {
            var model = new TestFillModelInheritImmediateFillModel();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketFillWasCalled);
            Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
        }

        #endregion

        #region OldFillInterfaceModelTests

        [Test]
        public void OldInterface_MarketFill()
        {
            var model = new TestFillModelInheritInterface();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketFillWasCalled);
            Assert.AreEqual(orderEvent, result.OrderEvent);
        }

        [Test]
        public void OldInterface_StopMarketFill()
        {
            var model = new TestFillModelInheritInterface();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new StopMarketOrder(_security.Symbol, 1, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.StopMarketFillWasCalled);
            Assert.AreEqual(orderEvent, result.OrderEvent);
        }

        [Test]
        public void OldInterface_StopLimitFill()
        {
            var model = new TestFillModelInheritInterface();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new StopLimitOrder(_security.Symbol, 1, 1, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.StopLimitFillWasCalled);
            Assert.AreEqual(orderEvent, result.OrderEvent);
        }

        [Test]
        public void OldInterface_LimitFill()
        {
            var model = new TestFillModelInheritInterface();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new LimitOrder(_security.Symbol, 1, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.LimitFillWasCalled);
            Assert.AreEqual(orderEvent, result.OrderEvent);
        }

        [Test]
        public void OldInterface_MarketOnOpenFill()
        {
            var model = new TestFillModelInheritInterface();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOnOpenOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketOnOpenFillWasCalled);
            Assert.AreEqual(orderEvent, result.OrderEvent);
        }

        [Test]
        public void OldInterface_MarketOnCloseFill()
        {
            var model = new TestFillModelInheritInterface();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOnCloseOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketOnCloseFillWasCalled);
            Assert.AreEqual(orderEvent, result.OrderEvent);
        }

        #endregion

        #region OldBaseFillModelTests

        [Test]
        public void OldBaseFillModel_DoesNotOverride_MarketFill()
        {
            var model = new TestFillModelInheritBaseClassDoesNotOverride();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        [Test]
        public void OldBaseFillModel_MarketFill()
        {
            var model = new TestFillModelInheritBaseClass();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketFillWasCalled);
            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        [Test]
        public void OldBaseFillModel_StopMarketFill()
        {
            var model = new TestFillModelInheritBaseClass();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new StopMarketOrder(_security.Symbol, 1, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.StopMarketFillWasCalled);
            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        [Test]
        public void OldBaseFillModel_StopLimitFill()
        {
            var model = new TestFillModelInheritBaseClass();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new StopLimitOrder(_security.Symbol, 1, 12344, 12346, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.StopLimitFillWasCalled);
            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        [Test]
        public void OldBaseFillModel_LimitFill()
        {
            var model = new TestFillModelInheritBaseClass();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new LimitOrder(_security.Symbol, 1, 12346, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.LimitFillWasCalled);
            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        [Test]
        public void OldBaseFillModel_MarketOnOpenFill()
        {
            var model = new TestFillModelInheritBaseClass();
            _security.SetMarketPrice(new Tick(orderDateTime, _security.Symbol, 88, 88) {TickType = TickType.Trade});

            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOnOpenOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketOnOpenFillWasCalled);
            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        [Test]
        public void OldBaseFillModel_MarketOnCloseFill()
        {
            var model = new TestFillModelInheritBaseClass();
            var result = model.Fill(
                new FillModelParameters(_security,
                    new MarketOnCloseOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour));

            Assert.True(model.MarketOnCloseFillWasCalled);
            Assert.IsNotNull(result);
            Assert.True(model.GetPricesWasCalled);
            Assert.AreEqual(12345, result.OrderEvent.FillPrice);
        }

        #endregion

        #region Python

        [Test]
        public void OldImmediateFillModelModel_MarketFill_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fills import ImmediateFillModel\n" +
                    "class CustomFillModel(ImmediateFillModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.MarketFillWasCalled = False\n" +
                    "   def MarketFill(self, asset, order):\n" +
                    "       self.MarketFillWasCalled = True\n" +
                    "       return super().MarketFill(asset, order)");

                var customFillModel = module.GetAttr("CustomFillModel").Invoke();
                var wrapper = new FillModelPythonWrapper(customFillModel);

                var result = wrapper.Fill(new FillModelParameters(
                        _security,
                        new MarketOrder(_security.Symbol, 1, orderDateTime),
                        new MockSubscriptionDataConfigProvider(_config),
                        Time.OneHour
                    ));

                bool called;
                customFillModel.GetAttr("MarketFillWasCalled").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
            }
        }

        [Test]
        public void OldBaseFillModel_MarketFill_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fills import FillModel\n" +
                    "class CustomFillModel(FillModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.MarketFillWasCalled = False\n" +
                    "   def MarketFill(self, asset, order):\n" +
                    "       self.MarketFillWasCalled = True\n" +
                    "       return super().MarketFill(asset, order)");

                var customFillModel = module.GetAttr("CustomFillModel").Invoke();
                var wrapper = new FillModelPythonWrapper(customFillModel);

                var result = wrapper.Fill(new FillModelParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour
                ));

                bool called;
                customFillModel.GetAttr("MarketFillWasCalled").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
            }
        }

        [Test]
        public void NewFillContext_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fills import FillModel\n" +
                    "class CustomFillModel(FillModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.FillWasCalled = False\n" +
                    "   def Fill(self, parameters):\n" +
                    "       self.FillWasCalled = True\n" +
                    "       return super().Fill(parameters)");

                var customFillModel = module.GetAttr("CustomFillModel").Invoke();
                var wrapper = new FillModelPythonWrapper(customFillModel);

                var result = wrapper.Fill(new FillModelParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour
                ));

                bool called;
                customFillModel.GetAttr("FillWasCalled").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
            }
        }

        [Test]
        public void OldFillModel_NewFillContextAndMarketFill_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fills import FillModel, Fill\n" +
                    "class CustomFillModel(FillModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.FillWasCalled = False\n" +
                    "       self.MarketFillWasCalled = False\n" +
                    "   def Fill(self, parameters):\n" +
                    "       self.FillWasCalled = True\n" +
                    "       self.Parameters = parameters\n" +
                    "       return Fill(self.MarketFill(parameters.Security, parameters.Order))\n" +
                    "   def MarketFill(self, asset, order):\n" +
                    "       self.MarketFillWasCalled = True\n" +
                    "       return super().MarketFill(asset, order)");

                var customFillModel = module.GetAttr("CustomFillModel").Invoke();
                var wrapper = new FillModelPythonWrapper(customFillModel);

                var result = wrapper.Fill(new FillModelParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour
                ));

                bool called;
                customFillModel.GetAttr("FillWasCalled").TryConvert(out called);
                Assert.True(called);
                customFillModel.GetAttr("MarketFillWasCalled").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
            }
        }

        [Test]
        public void OldFillModel_NewFillContextAndGetPrices_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fills import FillModel, Fill\n" +
                    "from QuantConnect.Orders import OrderEvent\n" +
                    "class CustomFillModel(FillModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.FillWasCalled = False\n" +
                    "       self.GetPricesWasCalled = False\n" +
                    "   def Fill(self, parameters):\n" +
                    "       self.FillWasCalled = True\n" +
                    "       self.Parameters = parameters\n" +
                    "       return Fill(super().MarketFill(parameters.Security, parameters.Order))\n" +
                    "   def GetPrices(self, asset, direction):\n" +
                    "       self.GetPricesWasCalled = True\n" +
                    "       return super().GetPrices(asset, direction)");

                var customFillModel = module.GetAttr("CustomFillModel").Invoke();
                var wrapper = new FillModelPythonWrapper(customFillModel);

                var result = wrapper.Fill(new FillModelParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour
                ));

                bool called;
                customFillModel.GetAttr("FillWasCalled").TryConvert(out called);
                Assert.True(called);
                customFillModel.GetAttr("GetPricesWasCalled").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
            }
        }

        [Test]
        public void OldImmediateFillModel_MarketFill_Py()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect.Orders.Fills import ImmediateFillModel\n" +
                    "class CustomFillModel(ImmediateFillModel):\n" +
                    "   def __init__(self):\n" +
                    "       self.MarketFillWasCalled = False\n" +
                    "   def MarketFill(self, asset, order):\n" +
                    "       self.MarketFillWasCalled = True\n" +
                    "       return super().MarketFill(asset, order)");

                var customFillModel = module.GetAttr("CustomFillModel").Invoke();
                var wrapper = new FillModelPythonWrapper(customFillModel);

                var result = wrapper.Fill(new FillModelParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime),
                    new MockSubscriptionDataConfigProvider(_config),
                    Time.OneHour
                ));

                bool called;
                customFillModel.GetAttr("MarketFillWasCalled").TryConvert(out called);
                Assert.True(called);
                Assert.IsNotNull(result);
                Assert.AreEqual(OrderStatus.Filled, result.OrderEvent.Status);
            }
        }

        #endregion

        private class TestFillModelInheritInterface : IFillModel
        {
            public bool MarketFillWasCalled;
            public bool StopMarketFillWasCalled;
            public bool StopLimitFillWasCalled;
            public bool LimitFillWasCalled;
            public bool MarketOnOpenFillWasCalled;
            public bool MarketOnCloseFillWasCalled;

            public OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                MarketFillWasCalled = true;
                return orderEvent;
            }

            public OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
            {
                StopMarketFillWasCalled = true;
                return orderEvent;
            }

            public OrderEvent StopLimitFill(Security asset, StopLimitOrder order)
            {
                StopLimitFillWasCalled = true;
                return orderEvent;
            }

            public OrderEvent LimitFill(Security asset, LimitOrder order)
            {
                LimitFillWasCalled = true;
                return orderEvent;
            }

            public OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
            {
                MarketOnOpenFillWasCalled = true;
                return orderEvent;
            }

            public OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
            {
                MarketOnCloseFillWasCalled = true;
                return orderEvent;
            }

            public Fill Fill(FillModelParameters parameters)
            {
                var order = parameters.Order;
                OrderEvent orderEvent;
                switch (order.Type)
                {
                    case OrderType.Market:
                        orderEvent =
                            MarketFill(parameters.Security, parameters.Order as MarketOrder);
                        break;
                    case OrderType.Limit:
                        orderEvent =
                            LimitFill(parameters.Security, parameters.Order as LimitOrder);
                        break;
                    case OrderType.StopMarket:
                        orderEvent =
                            StopMarketFill(parameters.Security, parameters.Order as StopMarketOrder);
                        break;
                    case OrderType.StopLimit:
                        orderEvent =
                            StopLimitFill(parameters.Security, parameters.Order as StopLimitOrder);
                        break;
                    case OrderType.MarketOnOpen:
                        orderEvent =
                            MarketOnOpenFill(parameters.Security, parameters.Order as MarketOnOpenOrder);
                        break;
                    case OrderType.MarketOnClose:
                        orderEvent =
                            MarketOnCloseFill(parameters.Security, parameters.Order as MarketOnCloseOrder);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return new Fill(orderEvent);
            }
        }

        private class TestFillModelInheritBaseClass : FillModel
        {
            public bool MarketFillWasCalled;
            public bool StopMarketFillWasCalled;
            public bool StopLimitFillWasCalled;
            public bool LimitIfTouchFillWasCalled;
            public bool LimitFillWasCalled;
            public bool MarketOnOpenFillWasCalled;
            public bool MarketOnCloseFillWasCalled;
            public bool GetPricesWasCalled;

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                MarketFillWasCalled = true;
                return base.MarketFill(asset, order);
            }

            public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
            {
                StopMarketFillWasCalled = true;
                return base.StopMarketFill(asset, order);
            }

            public override OrderEvent StopLimitFill(Security asset, StopLimitOrder order)
            {
                StopLimitFillWasCalled = true;
                return base.StopLimitFill(asset, order);
            }
            public override OrderEvent LimitIfTouchedFill(Security asset, LimitIfTouchedOrder order)
            {
                LimitIfTouchFillWasCalled = true;
                return base.LimitIfTouchedFill(asset, order);
            }

            public override OrderEvent LimitFill(Security asset, LimitOrder order)
            {
                LimitFillWasCalled = true;
                return base.LimitFill(asset, order);
            }

            public override OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
            {
                MarketOnOpenFillWasCalled = true;
                return base.MarketOnOpenFill(asset, order);
            }

            public override OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
            {
                MarketOnCloseFillWasCalled = true;
                return base.MarketOnCloseFill(asset, order);
            }

            protected override Prices GetPrices(Security asset, OrderDirection direction)
            {
                GetPricesWasCalled = true;
                return new Prices(orderDateTime, 12345, 12345, 12345, 12345, 12345);
            }
        }

        private class TestFillModelInheritBaseClassDoesNotOverride : FillModel
        {
            public bool GetPricesWasCalled;

            protected override Prices GetPrices(Security asset, OrderDirection direction)
            {
                GetPricesWasCalled = true;
                // call base.GetPrices() just to test it show its possible
                base.GetPrices(asset, direction);
                return new Prices(orderDateTime, 12345, 12345, 12345, 12345, 12345);
            }
        }

        private class TestFillModelInheritImmediateFillModel : ImmediateFillModel
        {
            public bool MarketFillWasCalled;

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                MarketFillWasCalled = true;
                return base.MarketFill(asset, order);
            }
        }
    }
}
