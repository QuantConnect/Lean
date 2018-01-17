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
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Engine;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashAccountMarginModelTests
    {
        private QuantConnect.Securities.Equity.Equity _spy;
        private Crypto _btcusd;
        private Crypto _btceur;
        private Crypto _ethusd;
        private Crypto _ethbtc;
        private SecurityPortfolioManager _portfolio;
        private BacktestingTransactionHandler _transactionHandler;
        private BacktestingBrokerage _brokerage;
        private CashAccountMarginModel _marginModel;
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
            _spy = new QuantConnect.Securities.Equity.Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                _portfolio.CashBook[CashBook.AccountCurrency],
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            _btcusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook[CashBook.AccountCurrency],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute, tz, tz, true, false, false),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            _ethusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook[CashBook.AccountCurrency],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.ETHUSD, Resolution.Minute, tz, tz, true, false, false),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            _btceur = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook["EUR"],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCEUR, Resolution.Minute, tz, tz, true, false, false),
                SymbolProperties.GetDefault("EUR"));

            _ethbtc = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook["BTC"],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.ETHBTC, Resolution.Minute, tz, tz, true, false, false),
                SymbolProperties.GetDefault("BTC"));

            _marginModel = new CashAccountMarginModel(_portfolio.CashBook);
        }

        [TearDown]
        public void TearDown()
        {
            _transactionHandler.Exit();
        }

        [Test]
        public void InitializesCorrectly()
        {
            Assert.AreEqual(1m, _marginModel.GetLeverage(_spy));
            Assert.AreEqual(1m, _marginModel.GetInitialMarginRequirement(_spy));
            Assert.AreEqual(0m, _marginModel.GetMaintenanceMargin(_spy));
            Assert.AreEqual(0m, _marginModel.GetMaintenanceMarginRequirement(_spy));
        }

        [Test]
        public void SetLeverageDoesNotUpdateLeverage()
        {
            // leverage cannot be set, will always be 1x
            _marginModel.SetLeverage(_spy, 50m);
            Assert.AreEqual(1m, _marginModel.GetLeverage(_spy));
        }

        [Test]
        public void EquityBuyOrderRequiresFullMargin()
        {
            _spy.SetMarketPrice(new Tick { Value = 250m });

            var order = new MarketOrder(_spy.Symbol, 2, DateTime.UtcNow);
            var initialMargin = _marginModel.GetInitialMarginRequiredForOrder(_spy, order);

            Assert.AreEqual(501m, initialMargin);
        }

        [Test]
        public void EquitySellOrderRequiresHolding()
        {
            _spy = _algorithm.AddEquity("SPY");
            _portfolio[_spy.Symbol].SetHoldings(150, 2);
            _spy.SetMarketPrice(new Tick { Value = 250m });
            _algorithm.SetFinishedWarmingUp();

            var order = new MarketOrder(_spy.Symbol, -2, DateTime.UtcNow);
            var initialMargin = _marginModel.GetInitialMarginRequiredForOrder(_spy, order);

            Assert.AreEqual(-499m, initialMargin);
        }

        [Test]
        public void MarginToBuyEquityEqualsCashAvailable()
        {
            _portfolio.SetCash(5000);

            Assert.AreEqual(5000m, _marginModel.GetMarginRemaining(_portfolio, _spy, OrderDirection.Buy));
        }

        [Test]
        public void MarginToBuyCryptoEqualsQuoteCurrencyAvailable()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["EUR"].SetAmount(1000);
            _portfolio.CashBook["BTC"].SetAmount(0.5m);

            Assert.AreEqual(5000m, _marginModel.GetMarginRemaining(_portfolio, _btcusd, OrderDirection.Buy));
            Assert.AreEqual(1000m, _marginModel.GetMarginRemaining(_portfolio, _btceur, OrderDirection.Buy));
            Assert.AreEqual(0.5m, _marginModel.GetMarginRemaining(_portfolio, _ethbtc, OrderDirection.Buy));
        }

        [Test]
        public void MarginToSellEquityEqualsZeroWithNoHoldings()
        {
            _portfolio.SetCash(5000);

            Assert.AreEqual(0m, _marginModel.GetMarginRemaining(_portfolio, _spy, OrderDirection.Sell));
        }

        [Test]
        public void MarginToSellCryptoEqualsZeroWithNoHoldings()
        {
            _portfolio.SetCash(5000);

            Assert.AreEqual(0m, _marginModel.GetMarginRemaining(_portfolio, _btcusd, OrderDirection.Sell));
            Assert.AreEqual(0m, _marginModel.GetMarginRemaining(_portfolio, _btceur, OrderDirection.Sell));
            Assert.AreEqual(0m, _marginModel.GetMarginRemaining(_portfolio, _ethbtc, OrderDirection.Sell));
        }

        [Test]
        public void MarginToSellEquityEqualsHoldingsCost()
        {
            _portfolio.SetCash(0);

            _spy = _algorithm.AddEquity("SPY");
            _portfolio[_spy.Symbol].SetHoldings(150, 2);

            // price goes up, margin does not change
            _spy.SetMarketPrice(new Tick { Value = 250m });
            Assert.AreEqual(300m, _marginModel.GetMarginRemaining(_portfolio, _spy, OrderDirection.Sell));

            // price goes down, margin does not change
            _spy.SetMarketPrice(new Tick { Value = 100m });
            Assert.AreEqual(300m, _marginModel.GetMarginRemaining(_portfolio, _spy, OrderDirection.Sell));
        }

        [Test]
        public void MarginToSellCryptoEqualsBaseCurrencyAvailable()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["BTC"].SetAmount(0.5m);
            _portfolio.CashBook["ETH"].SetAmount(2m);

            Assert.AreEqual(0.5m, _marginModel.GetMarginRemaining(_portfolio, _btcusd, OrderDirection.Sell));
            Assert.AreEqual(0.5m, _marginModel.GetMarginRemaining(_portfolio, _btceur, OrderDirection.Sell));
            Assert.AreEqual(2m, _marginModel.GetMarginRemaining(_portfolio, _ethbtc, OrderDirection.Sell));
        }

        [Test]
        public void MarginToBuyEquityIncludesOpenOrders()
        {
            _portfolio.SetCash(5000);

            _spy = _algorithm.AddEquity("SPY");
            _spy.SetMarketPrice(new Tick { Value = 250m });
            _algorithm.SetFinishedWarmingUp();

            SubmitLimitOrder(_spy.Symbol, 2, 200m);

            Assert.AreEqual(4600m, _marginModel.GetMarginRemaining(_portfolio, _spy, OrderDirection.Buy));
        }

        [Test]
        public void MarginToSellEquityIncludesOpenOrders()
        {
            _portfolio.SetCash(0);

            _spy = _algorithm.AddEquity("SPY");
            _portfolio[_spy.Symbol].SetHoldings(150, 5);
            _spy.SetMarketPrice(new Tick { Value = 250m });
            _algorithm.SetFinishedWarmingUp();

            SubmitLimitOrder(_spy.Symbol, -1, 300m);

            Assert.AreEqual(600m, _marginModel.GetMarginRemaining(_portfolio, _spy, OrderDirection.Sell));
        }

        [Test]
        public void MarginToBuyCryptoWithUsdIncludesOpenOrders()
        {
            _portfolio.SetCash(5000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });
            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD buy order decreases available USD
            SubmitLimitOrder(_btcusd.Symbol, 0.1m, 15000m);

            // ETHUSD buy order decreases available USD
            SubmitLimitOrder(_ethusd.Symbol, 3m, 1000m);

            Assert.AreEqual(500m, _marginModel.GetMarginRemaining(_portfolio, _btcusd, OrderDirection.Buy));
            Assert.AreEqual(500m, _marginModel.GetMarginRemaining(_portfolio, _ethusd, OrderDirection.Buy));
        }

        [Test]
        public void MarginToBuyCryptoWithBtcIncludesOpenOrders()
        {
            _portfolio.CashBook["BTC"].SetAmount(1m);
            _portfolio.CashBook["ETH"].SetAmount(1m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });
            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD sell order decreases available BTC
            SubmitLimitOrder(_btcusd.Symbol, -0.2m, 15000m);

            // ETHUSD sell order decreases available ETH
            SubmitLimitOrder(_ethusd.Symbol, -1m, 1000m);

            // ETHBTC buy order decreases available BTC
            SubmitLimitOrder(_ethbtc.Symbol, 1m, 0.1m);

            Assert.AreEqual(0.7m, _marginModel.GetMarginRemaining(_portfolio, _ethbtc, OrderDirection.Buy));
        }

        [Test]
        public void MarginToSellCryptoIncludesOpenOrders()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["BTC"].SetAmount(1m);
            _portfolio.CashBook["ETH"].SetAmount(3m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });
            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD sell order decreases available BTC
            SubmitLimitOrder(_btcusd.Symbol, -0.1m, 15000m);

            // ETHUSD sell order decreases available ETH
            SubmitLimitOrder(_ethusd.Symbol, -1m, 1000m);

            // ETHBTC buy order decreases available BTC
            SubmitLimitOrder(_ethbtc.Symbol, 1m, 0.1m);

            Assert.AreEqual(0.8m, _marginModel.GetMarginRemaining(_portfolio, _btcusd, OrderDirection.Sell));
            Assert.AreEqual(2m, _marginModel.GetMarginRemaining(_portfolio, _ethusd, OrderDirection.Sell));
            Assert.AreEqual(2m, _marginModel.GetMarginRemaining(_portfolio, _ethbtc, OrderDirection.Sell));
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
