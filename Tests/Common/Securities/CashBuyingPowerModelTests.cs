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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Engine;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashBuyingPowerModelTests
    {
        private Crypto _btcusd;
        private Crypto _btceur;
        private Crypto _ethusd;
        private Crypto _ethbtc;
        private SecurityPortfolioManager _portfolio;
        private BacktestingTransactionHandler _transactionHandler;
        private BacktestingBrokerage _brokerage;
        private CashBuyingPowerModel _buyingPowerModel;
        private QCAlgorithm _algorithm;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new QCAlgorithm();
            _portfolio = _algorithm.Portfolio;
            _portfolio.CashBook.Add("EUR", 0, 1.20m);
            _portfolio.CashBook.Add("BTC", 0, 15000m);
            _portfolio.CashBook.Add("ETH", 0, 1000m);

            _transactionHandler = new BacktestingTransactionHandler();
            _brokerage = new BacktestingBrokerage(_algorithm);
            _transactionHandler.Initialize(_algorithm, _brokerage, new TestResultHandler());
            new Thread(_transactionHandler.Run) { IsBackground = true }.Start();

            _algorithm.Transactions.SetOrderProcessor(_transactionHandler);

            var tz = TimeZones.NewYork;
            _btcusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook[CashBook.AccountCurrency],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("BTCUSD", "USD", 1, 0.01m, 0.00000001m))
            {
                FeeModel = new GDAXFeeModel()
            };

            _ethusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook[CashBook.AccountCurrency],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.ETHUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("ETHUSD", "USD", 1, 0.01m, 0.00000001m))
            {
                FeeModel = new GDAXFeeModel()
            };

            _btceur = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook["EUR"],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCEUR, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("BTCEUR", "EUR", 1, 0.01m, 0.00000001m))
            {
                FeeModel = new GDAXFeeModel()
            };

            _ethbtc = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook["BTC"],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.ETHBTC, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("ETHBTC", "BTC", 1, 0.00001m, 0.00000001m))
            {
                FeeModel = new GDAXFeeModel()
            };

            _buyingPowerModel = new CashBuyingPowerModel();
        }

        [TearDown]
        public void TearDown()
        {
            _transactionHandler.Exit();
        }

        [Test]
        public void InitializesCorrectly()
        {
            Assert.AreEqual(1m, _buyingPowerModel.GetLeverage(_btcusd));
            Assert.AreEqual(0m, _buyingPowerModel.GetReservedBuyingPowerForPosition(_btcusd));
        }

        [Test]
        public void SetLeverageDoesNotUpdateLeverage()
        {
            // leverage cannot be set, will always be 1x
            _buyingPowerModel.SetLeverage(_btcusd, 50m);
            Assert.AreEqual(1m, _buyingPowerModel.GetLeverage(_btcusd));
        }

        [Test]
        public void LimitBuyOrderRequiresQuoteCurrencyInPortfolio()
        {
            _portfolio.SetCash(20000);

            // Available cash = 20000, can buy 2 BTC at 10000
            var order = new LimitOrder(_btcusd.Symbol, 2m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // Available cash = 20000, cannot buy 2.1 BTC at 10000, need 21000
            order = new LimitOrder(_btcusd.Symbol, 2.1m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // Available cash = 20000, cannot buy 2 BTC at 11000, need 22000
            order = new LimitOrder(_btcusd.Symbol, 2m, 11000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void LimitSellOrderRequiresBaseCurrencyInPortfolio()
        {
            _portfolio.SetCash(0);
            _portfolio.CashBook["BTC"].SetAmount(0.5m);

            // 0.5 BTC in portfolio, can sell 0.5 BTC at any price
            var order = new LimitOrder(_btcusd.Symbol, -0.5m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 0.5 BTC in portfolio, cannot sell 0.6 BTC at any price
            order = new LimitOrder(_btcusd.Symbol, -0.6m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void LimitBuyOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.FeeModel = new GDAXFeeModel();
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.FeeModel = new GDAXFeeModel();
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD buy order decreases available USD (5000 - 1500 = 3500 USD)
            SubmitLimitOrder(_btcusd.Symbol, 0.1m, 15000m);

            // ETHUSD buy order decreases available USD (3500 - 3000 = 500 USD)
            SubmitLimitOrder(_ethusd.Symbol, 3m, 1000m);

            // 500 USD available, can buy 0.05 BTC at 10000
            var order = new LimitOrder(_btcusd.Symbol, 0.05m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 500 USD available, cannot buy 0.06 BTC at 10000
            order = new LimitOrder(_btcusd.Symbol, 0.06m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void LimitSellOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["BTC"].SetAmount(1m);
            _portfolio.CashBook["ETH"].SetAmount(3m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.FeeModel = new GDAXFeeModel();
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.FeeModel = new GDAXFeeModel();
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });

            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.FeeModel = new GDAXFeeModel();
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD sell order decreases available BTC (1 - 0.1 = 0.9 BTC)
            SubmitLimitOrder(_btcusd.Symbol, -0.1m, 15000m);

            // ETHUSD sell order decreases available ETH (3 - 1 = 2 ETH)
            SubmitLimitOrder(_ethusd.Symbol, -1m, 1000m);

            // ETHBTC buy order decreases available BTC (0.9 - 0.1 = 0.8 BTC)
            SubmitLimitOrder(_ethbtc.Symbol, 1m, 0.1m);

            // 0.8 BTC available, can sell 0.8 BTC at any price
            var order = new LimitOrder(_btcusd.Symbol, -0.8m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 0.8 BTC available, cannot sell 0.9 BTC at any price
            order = new LimitOrder(_btcusd.Symbol, -0.9m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 2 ETH available, can sell 2 ETH at any price
            order = new LimitOrder(_ethusd.Symbol, -2m, 1200m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _ethusd, order));

            // 2 ETH available, cannot sell 2.1 ETH at any price
            order = new LimitOrder(_ethusd.Symbol, -2.1m, 2000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _ethusd, order));
        }

        [Test]
        public void MarketBuyOrderRequiresQuoteCurrencyInPortfolioPlusFees()
        {
            _portfolio.SetCash(20000);

            _btcusd.SetMarketPrice(new Tick { Value = 10000m });
            _portfolio.SetCash("BTC", 0, 10000m);

            // Available cash = 20000, cannot buy 2 BTC at 10000 (fees are excluded)
            var order = new MarketOrder(_btcusd.Symbol, 2m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // Maximum we can market buy with 20000 USD is 1.995 BTC
            Assert.AreEqual(1.995m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 20000));

            _btcusd.SetMarketPrice(new Tick { Value = 9900m });
            _portfolio.SetCash("BTC", 0, 9900m);

            // Available cash = 20000, can buy 2 BTC at 9900 (plus fees)
            order = new MarketOrder(_btcusd.Symbol, 2m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void MarketSellOrderRequiresBaseCurrencyInPortfolioPlusFees()
        {
            _portfolio.SetCash(0);

            _btcusd.SetMarketPrice(new Tick { Value = 10000m });
            _portfolio.SetCash("BTC", 0.5m, 10000m);

            // 0.5 BTC in portfolio, cannot sell 0.5 BTC (fees are excluded)
            var order = new MarketOrder(_btcusd.Symbol, -0.5m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 0.5 BTC in portfolio, can sell 0.45 BTC (plus fees)
            order = new MarketOrder(_btcusd.Symbol, -0.45m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // Maximum we can market sell with 0.5 BTC is 0.49875 BTC
            Assert.AreEqual(-0.49875m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 0));
        }

        [Test]
        public void MarketBuyOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.FeeModel = new GDAXFeeModel();
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.FeeModel = new GDAXFeeModel();
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD buy order decreases available USD (5000 - 1500 = 3500 USD)
            SubmitLimitOrder(_btcusd.Symbol, 0.1m, 15000m);

            // ETHUSD buy order decreases available USD (3500 - 3000 = 500 USD)
            SubmitLimitOrder(_ethusd.Symbol, 3m, 1000m);

            // Maximum we can market buy with 500 USD is 0.03325 BTC
            Assert.AreEqual(0.03325m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 500));

            // 500 USD available, can buy 0.03 BTC at 15000
            var order = new MarketOrder(_btcusd.Symbol, 0.03m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 500 USD available, cannot buy 0.04 BTC at 15000
            order = new MarketOrder(_btcusd.Symbol, 0.04m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void MarketSellOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["BTC"].SetAmount(1m);
            _portfolio.CashBook["ETH"].SetAmount(3m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.FeeModel = new GDAXFeeModel();
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.FeeModel = new GDAXFeeModel();
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });

            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.FeeModel = new GDAXFeeModel();
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD sell order decreases available BTC (1 - 0.1 = 0.9 BTC)
            SubmitLimitOrder(_btcusd.Symbol, -0.1m, 15000m);

            // ETHBTC buy order decreases available BTC (0.9 - 0.1 = 0.8 BTC)
            SubmitLimitOrder(_ethbtc.Symbol, 1m, 0.1m);

            // Maximum we can market sell with 0.8 BTC is 0.798 BTC
            // target value = (1 - 0.8) * price
            Assert.AreEqual(-0.798m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 0.2m * 15000));

            // 0.8 BTC available, can sell 0.79 BTC at any price
            var order = new MarketOrder(_btcusd.Symbol, -0.79m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // 0.8 BTC available, cannot sell 0.8 BTC at any price
            order = new MarketOrder(_btcusd.Symbol, -0.8m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void LimitBuyOrderIncludesFees()
        {
            _portfolio.SetCash(20000);
            _btcusd.FeeModel = new ConstantFeeModel(50);

            // Available cash = 20000, cannot buy 2 BTC at 10000 because of order fee
            var order = new LimitOrder(_btcusd.Symbol, 2m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));

            // deposit another 50 USD
            _portfolio.CashBook["USD"].AddAmount(50);

            // now the order is allowed
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order));
        }

        [Test]
        public void CalculatesMaximumOrderQuantityCorrectly()
        {
            _portfolio.SetCash(10000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.FeeModel = new GDAXFeeModel();
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.FeeModel = new GDAXFeeModel();
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });

            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.FeeModel = new GDAXFeeModel();
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // 0.665 * 15000 + fees <= 10000 USD
            Assert.AreEqual(0.665m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 10000));

            // 9.97 * 1000 + fees <= 10000 USD
            Assert.AreEqual(9.97m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _ethusd, 10000));

            // no BTC in portfolio, cannot buy ETH with BTC
            Assert.AreEqual(0m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _ethbtc, 10000));
        }

        private void SubmitLimitOrder(Symbol symbol, decimal quantity, decimal limitPrice)
        {
            var resetEvent = new ManualResetEvent(false);

            _brokerage.OrderStatusChanged += (s, e) => { resetEvent.Set(); };

            _algorithm.LimitOrder(symbol, quantity, limitPrice);

            resetEvent.WaitOne();
        }
    }
}
