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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    /// <summary>
    /// Test mixed call order combinations of SetSecurityInitializer, SetBrokerageModel and AddSecurity
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class AlgorithmInitializeTests
    {
        private const string Ticker = "EURUSD";
        private const Resolution Resolution = QuantConnect.Resolution.Second;
        private const string Market = QuantConnect.Market.FXCM;
        private const BrokerageName BrokerageName = QuantConnect.Brokerages.BrokerageName.FxcmBrokerage;
        private const int RoundingPrecision = 20;
        private readonly MarketOrder _order = new MarketOrder { Quantity = 1000 };

        [Test]
        public void NiftyExampleRegistersOneMovingAveragePerSymbol()
        {
            var algorithm = new CustomDataNiftyAlgorithm();
            // DataManagerStub shares one type registry across tests, where other tests register their own Nifty type
            var registeredTypes = new RegisteredSecurityDataTypesProvider();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var securityService = new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(),
                algorithm, registeredTypes, new SecurityCacheProvider(algorithm.Portfolio), algorithm: algorithm);
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var universeSelection = new UniverseSelection(algorithm, securityService, dataPermissionManager, TestGlobals.DataProvider);
            algorithm.SubscriptionManager.SetDataManager(new DataManager(new NullDataFeed(), universeSelection, algorithm, algorithm.TimeKeeper,
                marketHoursDatabase, false, registeredTypes, dataPermissionManager));
            algorithm.SetHistoryProvider(new FakeHistoryProvider());

            algorithm.Initialize();

            var subscriptions = algorithm.SubscriptionManager.Subscriptions.ToList();
            var rupee = subscriptions.Single(config => config.Type == typeof(DollarRupee));
            var nifty = subscriptions.Single(config => config.Type == typeof(Nifty));

            Assert.AreEqual(1, rupee.Consolidators.Count);
            Assert.AreEqual(1, nifty.Consolidators.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Validates_SetEndTime(bool explicitSet)
        {
            var algorithm = GetAlgorithm();

            // We initialize the algorithm with `now` in the algorithm default time zone
            var end = DateTime.UtcNow.ConvertFromUtc(algorithm.TimeZone);

            if (explicitSet)
            {
                algorithm.SetEndDate(end);
            }
            var excepted = end.RoundDown(TimeSpan.FromDays(1)).AddTicks(-1);
            Assert.AreEqual(algorithm.EndDate, excepted);
        }

        [Test]
        public void Validates_SetBrokerageModel_AddForex()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName);
            var security = algorithm.AddForex(Ticker, Resolution, Market);

            // Leverage and FeeModel from BrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetBrokerageModel_IB_AddForex()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            var security = algorithm.AddForex(Ticker, Resolution, Market);

            // Leverage and FeeModel from BrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForex_SetBrokerageModel()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market);
            algorithm.SetBrokerageModel(BrokerageName);

            // Leverage and FeeModel from BrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetBrokerageModel_AddForexWithLeverage()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName);
            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from BrokerageModel
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForexWithLeverage_SetBrokerageModel()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);
            algorithm.SetBrokerageModel(BrokerageName);

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from BrokerageModel
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetSecurityInitializer_AddForex()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            var security = algorithm.AddForex(Ticker, Resolution, Market);

            // Leverage and FeeModel from SecurityInitializer
            Assert.AreEqual(100, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForex_SetSecurityInitializer()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });

            // SetSecurityInitializer does not apply to securities added before the call
            // Leverage and FeeModel from DefaultBrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(ConstantFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetSecurityInitializer_AddForexWithLeverage()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from SecurityInitializer
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForexWithLeverage_SetSecurityInitializer()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });

            // Leverage passed to AddForex always takes precedence
            // SetSecurityInitializer does not apply to securities added before the call
            // Leverage from AddForex, FeeModel from DefaultBrokerageModel
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(ConstantFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetSecurityInitializer_AddForex_SetBrokerageModel()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            var security = algorithm.AddForex(Ticker, Resolution, Market);
            algorithm.SetBrokerageModel(BrokerageName);

            // SetSecurityInitializer overrides the brokerage model
            // Leverage and FeeModel from SecurityInitializer
            Assert.AreEqual(100, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetSecurityInitializer_SetBrokerageModel_AddForex()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            algorithm.SetBrokerageModel(BrokerageName);
            var security = algorithm.AddForex(Ticker, Resolution, Market);

            // SetSecurityInitializer overrides the brokerage model
            // Leverage and FeeModel from SecurityInitializer
            Assert.AreEqual(100, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetBrokerageModel_SetSecurityInitializer_AddForex()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            var security = algorithm.AddForex(Ticker, Resolution, Market);

            // SetSecurityInitializer overrides the brokerage model
            // Leverage and FeeModel from SecurityInitializer
            Assert.AreEqual(100, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetBrokerageModel_AddForex_SetSecurityInitializer()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName);
            var security = algorithm.AddForex(Ticker, Resolution, Market);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });

            // SetSecurityInitializer does not apply to securities added before the call
            // Leverage and FeeModel from BrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForex_SetSecurityInitializer_SetBrokerageModel()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            algorithm.SetBrokerageModel(BrokerageName);

            // SetSecurityInitializer does not apply to securities added before the call
            // Leverage and FeeModel from DefaultBrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(ConstantFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForex_SetBrokerageModel_SetSecurityInitializer()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market);
            algorithm.SetBrokerageModel(BrokerageName);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });

            // SetSecurityInitializer does not apply to securities added before the call
            // Leverage and FeeModel from BrokerageModel
            Assert.AreEqual(50, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetSecurityInitializer_AddForexWithLeverage_SetBrokerageModel()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);
            algorithm.SetBrokerageModel(BrokerageName);

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from Initializer
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetSecurityInitializer_SetBrokerageModel_AddForexWithLeverage()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            algorithm.SetBrokerageModel(BrokerageName);
            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);

            // Leverage passed to AddForex always takes precedence
            // SetSecurityInitializer overrides the brokerage model
            // Leverage from AddForex, FeeModel from SecurityInitializer
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetBrokerageModel_SetSecurityInitializer_AddForexWithLeverage()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);

            // Leverage passed to AddForex always takes precedence
            // SetSecurityInitializer overrides the brokerage model
            // Leverage from AddForex, FeeModel from SecurityInitializer
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(InteractiveBrokersFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(2, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_SetBrokerageModel_AddForexWithLeverage_SetSecurityInitializer()
        {
            var algorithm = GetAlgorithm();

            algorithm.SetBrokerageModel(BrokerageName);
            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from BrokerageModel
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForexWithLeverage_SetSecurityInitializer_SetBrokerageModel()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });
            algorithm.SetBrokerageModel(BrokerageName);

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from DefaultBrokerageModel
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(ConstantFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        public void Validates_AddForexWithLeverage_SetBrokerageModel_SetSecurityInitializer()
        {
            var algorithm = GetAlgorithm();

            var security = algorithm.AddForex(Ticker, Resolution, Market, true, 25);
            algorithm.SetBrokerageModel(BrokerageName);
            algorithm.SetSecurityInitializer(x =>
            {
                x.SetLeverage(100);
                x.FeeModel = new InteractiveBrokersFeeModel();
            });

            // Leverage passed to AddForex always takes precedence
            // Leverage from AddForex, FeeModel from BrokerageModel
            Assert.AreEqual(25, Math.Round(security.Leverage, RoundingPrecision));
            Assert.IsInstanceOf(typeof(FxcmFeeModel), security.FeeModel);
            var fee = security.FeeModel.GetOrderFee(
                new OrderFeeParameters(security, _order));
            Assert.AreEqual(0.04, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        private QCAlgorithm GetAlgorithm()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            return algorithm;
        }
    }
}
