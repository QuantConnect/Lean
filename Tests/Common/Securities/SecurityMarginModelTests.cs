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
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityMarginModelTests
    {
        private static Symbol _symbol;
        private static readonly string _cashSymbol = "USD";
        private static FakeOrderProcessor _fakeOrderProcessor;
        [Test]
        public void ZeroTargetWithZeroHoldingsIsNotAnError()
        {
            var algorithm = new QCAlgorithm();
            var security = algorithm.AddSecurity(SecurityType.Equity, "SPY");

            var model = new SecurityMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetValue(algorithm.Portfolio, security, 0);

            Assert.AreEqual(0, result.Quantity);
            Assert.AreEqual(string.Empty, result.Reason);
            Assert.AreEqual(false, result.IsError);
        }

        [Test]
        public void ZeroTargetWithNonZeroHoldingsReturnsNegativeOfQuantity()
        {
            var algorithm = new QCAlgorithm();
            var security = algorithm.AddSecurity(SecurityType.Equity, "SPY");
            security.Holdings.SetHoldings(200, 10);

            var model = new SecurityMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetValue(algorithm.Portfolio, security, 0);

            Assert.AreEqual(-10, result.Quantity);
            Assert.AreEqual(string.Empty, result.Reason);
            Assert.AreEqual(false, result.IsError);
        }

        [Test]
        public void ManuallySettingFreeBuyingPowerPercentWorksCorrectly()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // (100000 * 2) / 25 - 1 order due to fees
            Assert.AreEqual(7999m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual + security.SymbolProperties.LotSize, security, algo));
            Assert.AreEqual(security.BuyingPowerModel.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);

            security.BuyingPowerModel = new SecurityMarginModel(2, 0.5m);
            actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // (100000 * 2 * 0.5) / 25 - 1 order due to fees
            Assert.AreEqual(3999m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual + security.SymbolProperties.LotSize, security, algo));
            Assert.AreEqual(security.BuyingPowerModel.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash / 2);
        }

        [Test]
        public void FreeBuyingPowerPercentDefault_Equity()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Equity);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 2) / 25 - 1 order due to fees
            Assert.AreEqual(7999m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);
        }

        [Test]
        public void FreeBuyingPowerPercentDoesNotApplyForIBMarginAccount_Equity()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5);
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 2) / 25 - 4 order due to fees
            Assert.AreEqual(7996m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual + security.SymbolProperties.LotSize, security, algo));
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForIBCashAccount_Equity()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5);
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 2 * 0.95) / 25 - 4 order due to fees
            Assert.AreEqual(7596m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual + security.SymbolProperties.LotSize, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - algo.BrokerageModel.RequiredFreeBuyingPowerPercent);
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), expectedBuyingPower);
        }

        [Test]
        public void FreeBuyingPowerPercentDefault_Option()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Option);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1) / (25 * 100 contract multiplier) - 1 order due to fees
            Assert.AreEqual(39m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);
        }

        [Test]
        public void FreeBuyingPowerPercentDoesNotApplyForIBMarginAccount_Option()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Option);
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1) / (25 * 100 contract multiplier) - 1 order due to fees
            Assert.AreEqual(39m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForIBCashAccount_Option()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Option);
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1 * 0.95) / (25 * 100 contract multiplier) - 1 order due to fees
            Assert.AreEqual(37m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - algo.BrokerageModel.RequiredFreeBuyingPowerPercent);
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), expectedBuyingPower);
        }

        [Test]
        public void FreeBuyingPowerPercentDefault_Future()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Future);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1) / 25 - 1 order due to fees
            Assert.AreEqual(3999m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);
        }

        [Test]
        public void FreeBuyingPowerPercentDoesNotApplyForIBMarginAccount_Future()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Future);
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1 - 7K for fee) / 25
            Assert.AreEqual(3704m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), algo.Portfolio.Cash);
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForIBCashAccount_Future()
        {
            Security security;
            var algo = GetAlgorithm(out security, 5, SecurityType.Future);
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1 * 0.95 - 6.5K for fees / (25)
            Assert.AreEqual(3519m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - algo.BrokerageModel.RequiredFreeBuyingPowerPercent);
            Assert.AreEqual(model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy), expectedBuyingPower);
        }

        private static QCAlgorithm GetAlgorithm(out Security security, decimal fee, SecurityType securityType = SecurityType.Equity, string symbol = "SPY")
        {
            SymbolCache.Clear();
            // Initialize algorithm
            var algo = new QCAlgorithm();
            algo.SetCash(100000);
            algo.SetFinishedWarmingUp();
            _fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(_fakeOrderProcessor);

            if (securityType == SecurityType.Equity)
            {
                security = algo.AddEquity(symbol);
                _symbol = security.Symbol;
            }
            else if (securityType == SecurityType.Option)
            {
                security = algo.AddOption(symbol);
                _symbol = security.Symbol;
            }
            else if (securityType == SecurityType.Future)
            {
                security = algo.AddFuture(symbol);
                _symbol = security.Symbol;
            }
            else
            {
                throw new Exception("SecurityType not implemented");
            }

            security.TransactionModel = new ConstantFeeTransactionModel(fee);
            Update(algo.Portfolio.CashBook, security, 25);
            return algo;
        }
        private static void Update(CashBook cashBook, Security security, decimal close)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = close,
                High = close,
                Low = close,
                Close = close
            });
        }
        private bool HasSufficientBuyingPowerForOrder(decimal orderQuantity, Security security, IAlgorithm algo)
        {
            var order = new MarketOrder(security.Symbol, orderQuantity, DateTime.UtcNow);
            _fakeOrderProcessor.AddTicket(order.ToOrderTicket(algo.Transactions));
            var hashSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio,
                security, new MarketOrder(security.Symbol, orderQuantity, DateTime.UtcNow));
            return hashSufficientBuyingPower.IsSufficient;
        }
    }
}