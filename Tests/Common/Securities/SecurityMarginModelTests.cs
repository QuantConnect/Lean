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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;
using Option = QuantConnect.Securities.Option.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityMarginModelTests
    {
        private static Symbol _symbol;
        private static FakeOrderProcessor _fakeOrderProcessor;

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(50)]
        public void MarginRemainingForLeverage(decimal leverage)
        {
            var algorithm = GetAlgorithm();
            algorithm.SetCash(1000);

            var spy = InitAndGetSecurity(algorithm, 0);
            spy.Holdings.SetHoldings(25, 100);
            spy.SetLeverage(leverage);

            var spyMarginAvailable = spy.Holdings.HoldingsValue - spy.Holdings.HoldingsValue * (1 / leverage);

            var marginRemaining = algorithm.Portfolio.MarginRemaining;
            Assert.AreEqual(1000 + spyMarginAvailable, marginRemaining);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(50)]
        public void MarginUsedForPositionWhenPriceDrops(decimal leverage)
        {
            var algorithm = GetAlgorithm();

            // (1000 * 20) = 20k
            // Initial and maintenance margin = (1000 * 20) / leverage = X
            var spy = InitAndGetSecurity(algorithm, 0);
            spy.Holdings.SetHoldings(20, 1000);
            spy.SetLeverage(leverage);

            // Drop 40% price from $20 to $12
            // 1000 * 12 = 12k
            Update(spy, 12);

            var marginForPosition = spy.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(spy)).AbsoluteUsedBuyingPower;
            Assert.AreEqual(1000 * 12 / leverage, marginForPosition);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(50)]
        public void MarginUsedForPositionWhenPriceIncreases(decimal leverage)
        {
            var algorithm = GetAlgorithm();
            algorithm.SetCash(1000);

            // (1000 * 20) = 20k
            // Initial and maintenance margin = (1000 * 20) / leverage = X
            var spy = InitAndGetSecurity(algorithm, 0);
            spy.Holdings.SetHoldings(25, 1000);
            spy.SetLeverage(leverage);

            // Increase from $20 to $40
            // 1000 * 40 = 400000
            Update(spy, 40);

            var marginForPosition = spy.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(spy)).AbsoluteUsedBuyingPower;
            Assert.AreEqual(1000 * 40 / leverage, marginForPosition);
        }

        [Test]
        public void ZeroTargetWithZeroHoldingsIsNotAnError()
        {
            var algorithm = GetAlgorithm();
            var security = InitAndGetSecurity(algorithm, 0);

            var model = new SecurityMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetBuyingPower(algorithm.Portfolio, security, 0, 0);

            Assert.AreEqual(0, result.Quantity);
            Assert.IsTrue(result.Reason.IsNullOrEmpty());
            Assert.IsFalse(result.IsError);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        public void ReturnsMinimumOrderValueReason(decimal holdings)
        {
            var algorithm = GetAlgorithm();
            var security = InitAndGetSecurity(algorithm, 0);
            var model = new SecurityMarginModel();
            security.Holdings.SetHoldings(security.Price, holdings);
            var currentSignedUsedMargin = model.GetInitialMarginRequirement(security, security.Holdings.Quantity);
            var totalPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            var sign = Math.Sign(security.Holdings.Quantity) == 0 ? 1 : Math.Sign(security.Holdings.Quantity);
            // we increase it slightly, should not trigger a new order because it's increasing final margin usage, rounds down
            var newTarget = currentSignedUsedMargin / (totalPortfolioValue) + 0.00001m * sign;

            var result = model.GetMaximumOrderQuantityForTargetBuyingPower(algorithm.Portfolio, security, newTarget, 0);
            Assert.AreEqual(0m, result.Quantity);
            Assert.IsFalse(result.IsError);
            Assert.IsTrue(result.Reason.Contains("The order quantity is less than the lot size of", StringComparison.InvariantCultureIgnoreCase));
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void ReducesPositionWhenMarginAboveTargetWhenNegativeFreeMargin(decimal holdings)
        {
            var algorithm = GetAlgorithm();
            var security = InitAndGetSecurity(algorithm, 0);
            var model = new SecurityMarginModel();
            security.Holdings.SetHoldings(security.Price, holdings);

            var security2 = InitAndGetSecurity(algorithm, 0, symbol: "AAPL");
            // eat up all our TPV
            security2.Holdings.SetHoldings(security.Price, (algorithm.Portfolio.TotalPortfolioValue / security.Price) * 2);

            var currentSignedUsedMargin = model.GetInitialMarginRequirement(security, security.Holdings.Quantity);
            var totalPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            var sign = Math.Sign(security.Holdings.Quantity) == 0 ? 1 : Math.Sign(security.Holdings.Quantity);
            // we inverse the sign here so that new target is less than current, we expect a reduction
            var newTarget = currentSignedUsedMargin / (totalPortfolioValue) + 0.00001m * sign * -1;

            Assert.IsTrue(0 > algorithm.Portfolio.MarginRemaining);
            var result = model.GetMaximumOrderQuantityForTargetBuyingPower(algorithm.Portfolio, security, newTarget, 0);
            // Reproduces GH issue #5763 a small Reduction in the target should reduce the position
            Assert.AreEqual(1m * sign * -1, result.Quantity);
            Assert.IsFalse(result.IsError);
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(1, 0.001d)]
        [TestCase(-1, 0.001d)]
        public void ReducesPositionWhenMarginAboveTargetBasedOnSetting(decimal holdings, decimal minimumOrderMarginPortfolioPercentage)
        {
            var algorithm = GetAlgorithm();
            var security = InitAndGetSecurity(algorithm, 0);
            var model = new SecurityMarginModel();
            security.Holdings.SetHoldings(security.Price, holdings);

            var currentSignedUsedMargin = model.GetInitialMarginRequirement(security, security.Holdings.Quantity);
            var totalPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            var sign = Math.Sign(security.Holdings.Quantity) == 0 ? 1 : Math.Sign(security.Holdings.Quantity);
            // we inverse the sign here so that new target is less than current, we expect a reduction
            var newTarget = currentSignedUsedMargin / (totalPortfolioValue) + 0.00001m * sign * -1;

            var result = model.GetMaximumOrderQuantityForTargetBuyingPower(algorithm.Portfolio, security, newTarget, minimumOrderMarginPortfolioPercentage);

            if (minimumOrderMarginPortfolioPercentage == 0)
            {
                // Reproduces GH issue #5763 a small Reduction in the target should reduce the position
                Assert.AreEqual(1m * sign * -1, result.Quantity);
                Assert.IsFalse(result.IsError);
            }
            else
            {
                Assert.AreEqual(0, result.Quantity);
                Assert.IsFalse(result.IsError);
            }
        }

        [Test]
        public void ZeroTargetWithNonZeroHoldingsReturnsNegativeOfQuantity()
        {
            var algorithm = GetAlgorithm();
            var security = InitAndGetSecurity(algorithm, 0);
            security.Holdings.SetHoldings(200, 10);

            var model = new SecurityMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetBuyingPower(algorithm.Portfolio, security, 0, 0);

            Assert.AreEqual(-10, result.Quantity);
            Assert.IsTrue(result.Reason.IsNullOrEmpty());
            Assert.IsFalse(result.IsError);
        }

        [Test]
        public void SetHoldings_ZeroToFullLong()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5);
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // (100000 * 2 * 0.9975 setHoldingsBuffer) / 25 - fee ~=7979m
            Assert.AreEqual(7979m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_ZeroToFullLong_NonAccountCurrency_ZeroQuoteCurrency()
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            // We don't have quote currency - we will get a "loan"
            algorithm.Portfolio.SetCash(Currencies.USD, 0, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);

            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            var actual = algorithm.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // (10000 * 2 * 0.9975 setHoldingsBuffer) / 25 * 0.88 conversion rate - 5 USD fee * 0.88 conversion rate ~=906m
            Assert.AreEqual(906m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algorithm));
        }

        [TestCase("Long")]
        [TestCase("Short")]
        public void GetReservedBuyingPowerForPosition_NonAccountCurrency_ZeroQuoteCurrency(string position)
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, 0, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);
            security.Holdings.SetHoldings(security.Price,
                (position == "Long" ? 1 : -1) * 100);

            var actual = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(new ReservedBuyingPowerForPositionParameters(security));
            // 100quantity * 25price * 0.88rate * 0.5 MaintenanceMarginRequirement = 1100
            Assert.AreEqual(1100, actual.AbsoluteUsedBuyingPower);
        }

        [Test]
        public void SetHoldings_ZeroToFullLong_NonAccountCurrency()
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            // We have 1000 USD too
            algorithm.Portfolio.SetCash(Currencies.USD, 1000, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);

            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            var actual = algorithm.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // ((10000 + 1000 USD * 0.88 rate) * 2 * 0.9975 setHoldingsBuffer) / 25 * 0.88 rate - 5 USD fee * 0.88 rate ~=986m
            Assert.AreEqual(986m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algorithm));
        }

        [Test]
        public void SetHoldings_Long_TooBigOfATarget()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5);
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security) + 0.1m);
            // (100000 * 2.1* 0.9975 setHoldingsBuffer) / 25 - fee ~=8378m
            Assert.AreEqual(8378m, actual);
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_Long_TooBigOfATarget_NonAccountCurrency()
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            // We don't have quote currency - we will get a "loan"
            algorithm.Portfolio.SetCash(Currencies.USD, 0, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);

            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            var actual = algorithm.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security) + 0.1m);
            // (10000 * 2.1 * 0.9975 setHoldingsBuffer) / 25 * 0.88 conversion rate - 5 USD fee * 0.88 conversion rate ~=951m
            Assert.AreEqual(951m, actual);
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual, security, algorithm));
        }

        [Test]
        public void SetHoldings_ZeroToFullShort()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5);
            var actual = algo.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security));
            // (100000 * 2 * 0.9975 setHoldingsBuffer) / 25 - fee~=-7979m
            Assert.AreEqual(-7979m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_ZeroToFullShort_NonAccountCurrency_ZeroQuoteCurrency()
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, 0, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);

            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            var actual = algorithm.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security));
            // (10000 * - 2 * 0.9975 setHoldingsBuffer) / 25 * 0.88 conversion rate - 5 USD fee * 0.88 conversion rate ~=906m
            Assert.AreEqual(-906m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algorithm));
        }

        [Test]
        public void SetHoldings_ZeroToFullShort_NonAccountCurrency()
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, 1000, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);

            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            var actual = algorithm.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security));
            // ((10000 + 1000 * 0.88)* - 2 * 0.9975 setHoldingsBuffer) / 25 * 0.88 conversion rate - 5 USD fee * 0.88 conversion rate ~=986m
            Assert.AreEqual(-986m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algorithm));
        }

        [Test]
        public void SetHoldings_Short_TooBigOfATarget()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5);
            var actual = algo.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security) - 0.1m);
            // (100000 * - 2.1m * 0.9975 setHoldingsBuffer) / 25 - fee~=-8378m
            Assert.AreEqual(-8378m, actual);
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_Short_TooBigOfATarget_NonAccountCurrency()
        {
            var algorithm = GetAlgorithm();
            algorithm.Portfolio.CashBook.Clear();
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, 0, 0.88m);
            var security = InitAndGetSecurity(algorithm, 5);

            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            var actual = algorithm.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security) - 0.1m);
            // (10000 * - 2.1 * 0.9975 setHoldingsBuffer) / 25 * 0.88 conversion rate - 5 USD fee * 0.88 conversion rate ~=951m
            Assert.AreEqual(-951m, actual);
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual, security, algorithm));
        }

        [Test]
        public void SetHoldings_ZeroToFullLong_NoFee()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // (100000 * 2 * 0.9975 setHoldingsBuffer) / 25 =7980m
            Assert.AreEqual(7980m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_Long_TooBigOfATarget_NoFee()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security) + 0.1m);
            // (100000 * 2.1m* 0.9975 setHoldingsBuffer) / 25 = 8379m
            Assert.AreEqual(8379m, actual);
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_ZeroToFullShort_NoFee()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security));
            var order = new MarketOrder(_symbol, actual, DateTime.UtcNow);
            // (100000 * 2 * 0.9975 setHoldingsBuffer) / 25 = -7980m
            Assert.AreEqual(-7980m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void SetHoldings_Short_TooBigOfATarget_NoFee()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, -1m * security.BuyingPowerModel.GetLeverage(security) - 0.1m);
            // (100000 * -2.1 * 0.9975 setHoldingsBuffer) / 25 =  -8379m
            Assert.AreEqual(-8379m, actual);
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual, security, algo));
        }

        [Test]
        public void FreeBuyingPowerPercentDefault_Equity()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5, SecurityType.Equity);
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 2 * 0.9975) / 25 - 1 order due to fees
            Assert.AreEqual(7979m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(algo.Portfolio.Cash, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForCashAccount_Equity()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Equity);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(1, requiredFreeBuyingPowerPercent);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1 * 0.95 * 0.9975) / 25 - 1 order due to fees
            Assert.AreEqual(3790m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual * 1.0025m, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual * 1.0025m + security.SymbolProperties.LotSize + 9, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - requiredFreeBuyingPowerPercent);
            Assert.AreEqual(expectedBuyingPower, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForMarginAccount_Equity()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Equity);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(2, requiredFreeBuyingPowerPercent);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 2 * 0.95 * 0.9975) / 25 - 1 order due to fees
            Assert.AreEqual(7580m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual * 1.0025m, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual * 1.0025m + security.SymbolProperties.LotSize + 9, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - requiredFreeBuyingPowerPercent);
            Assert.AreEqual(expectedBuyingPower, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentCashAccountWithLongHoldings_Equity()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Equity);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(1, requiredFreeBuyingPowerPercent);
            security.Holdings.SetHoldings(25, 2000);
            security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(algo.Portfolio, security, DateTime.UtcNow.AddDays(-10), new CashAmount(-2000 * 25, Currencies.USD), null));

            // Margin remaining 50k + used 50k + initial margin 50k - 5k free buying power percent (5% of 100k)
            Assert.AreEqual(145000, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Sell));
            // Margin remaining 50k - 5k free buying power percent (5% of 100k)
            Assert.AreEqual(45000, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));

            var actual = algo.CalculateOrderQuantity(_symbol, -1m * model.GetLeverage(security));
            // ((100k - 5) * -1 * 0.95 * 0.9975 - (50k holdings)) / 25 - 1 order due to fees
            Assert.AreEqual(-5790m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual * 1.0025m, security, algo));
        }

        [Test]
        public void FreeBuyingPowerPercentMarginAccountWithLongHoldings_Equity()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Equity);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(2, requiredFreeBuyingPowerPercent);
            security.Holdings.SetHoldings(25, 2000);
            security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(algo.Portfolio, security, DateTime.UtcNow.AddDays(-10), new CashAmount(-2000 * 25, Currencies.USD), null));

            // Margin remaining 75k + used 25k + initial margin 25k - 5k free buying power percent (5% of 100k)
            Assert.AreEqual(120000, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Sell));
            // Margin remaining 75k - 5k free buying power percent
            Assert.AreEqual(70000, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));

            var actual = algo.CalculateOrderQuantity(_symbol, -1m * model.GetLeverage(security));
            // ((100k - 5) * -2 * 0.95 * 0.9975 - (50k holdings)) / 25 - 1 order due to fees
            Assert.AreEqual(-9580m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual * 1.0025m, security, algo));
        }

        [Test]
        public void FreeBuyingPowerPercentMarginAccountWithShortHoldings_Equity()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Equity);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(2, requiredFreeBuyingPowerPercent);
            security.Holdings.SetHoldings(25, -2000);
            security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(algo.Portfolio, security, DateTime.UtcNow.AddDays(-10), new CashAmount(2000 * 25, Currencies.USD), null));

            // Margin remaining 75k + used 25k + initial margin 25k - 5k free buying power percent (5% of 100k)
            Assert.AreEqual(120000, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
            // Margin remaining 75k - 5k free buying power percent
            Assert.AreEqual(70000, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Sell));

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // ((100k - 5) * 2 * 0.95 * 0.9975 - (-50k holdings)) / 25 - 1 order due to fees
            Assert.AreEqual(9580m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.IsFalse(HasSufficientBuyingPowerForOrder(actual * 1.0025m, security, algo));
        }

        [Test]
        public void FreeBuyingPowerPercentDefault_Option()
        {
            const decimal price = 25m;
            const decimal underlyingPrice = 25m;

            var tz = TimeZones.NewYork;
            var equity = new QuantConnect.Securities.Equity.Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPutSymbol = Symbol.CreateOption(Symbols.SPY, Market.USA, OptionStyle.American, OptionRight.Put, 207m, new DateTime(2015, 02, 27));
            var security = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), optionPutSymbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties("", Currencies.USD, 100, 0.01m, 1),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick { Value = price });
            security.Underlying = equity;

            var algo = GetAlgorithm();
            security.SetLocalTimeKeeper(algo.TimeKeeper.GetLocalTimeKeeper(tz));
            var actual = security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(
                new GetMaximumOrderQuantityForTargetBuyingPowerParameters(algo.Portfolio, security, 1, 0)).Quantity;

            // (100000 * 1) / (25 * 100 contract multiplier) - 1 order due to fees
            Assert.AreEqual(39m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(algo.Portfolio.Cash, security.BuyingPowerModel.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForCashAccount_Option()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Option);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(1, requiredFreeBuyingPowerPercent);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1 * 0.95) / (25 * 100 contract multiplier) - 1 order due to fees
            Assert.AreEqual(37m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - requiredFreeBuyingPowerPercent);
            Assert.AreEqual(expectedBuyingPower, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForMarginAccount_Option()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Option);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(2, requiredFreeBuyingPowerPercent);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 2 * 0.95) / (25 * 100 contract multiplier) - 1 order due to fees
            Assert.AreEqual(75m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - requiredFreeBuyingPowerPercent);
            Assert.AreEqual(expectedBuyingPower, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentDefault_Future()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5, SecurityType.Future, "ES", time: new DateTime(2020, 1, 27));
            var model = security.BuyingPowerModel;

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // (100000 * 1 * 0.9975 ) / 6600 - 1 order due to fees
            Assert.AreEqual(13m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            Assert.AreEqual(algo.Portfolio.Cash, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForCashAccount_Future()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Future);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(1, requiredFreeBuyingPowerPercent);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // ((100000 - 5) * 1 * 0.95 * 0.9975 / (25 * 50)
            Assert.AreEqual(75m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - requiredFreeBuyingPowerPercent);
            Assert.AreEqual(expectedBuyingPower, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [Test]
        public void FreeBuyingPowerPercentAppliesForMarginAccount_Future()
        {
            var algo = GetAlgorithm();
            algo.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var security = InitAndGetSecurity(algo, 5, SecurityType.Future);
            var requiredFreeBuyingPowerPercent = 0.05m;
            var model = security.BuyingPowerModel = new SecurityMarginModel(2, requiredFreeBuyingPowerPercent);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m * model.GetLeverage(security));
            // ((100000 - 5) * 2 * 0.95 * 0.9975 / (25 * 50)
            Assert.AreEqual(151m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));
            var expectedBuyingPower = algo.Portfolio.Cash * (1 - requiredFreeBuyingPowerPercent);
            Assert.AreEqual(expectedBuyingPower, model.GetBuyingPower(algo.Portfolio, security, OrderDirection.Buy));
        }

        [TestCase(0)]
        [TestCase(10000)]
        public void NonAccountCurrency_GetBuyingPower(decimal nonAccountCurrencyCash)
        {
            var algo = GetAlgorithm();
            algo.Portfolio.CashBook.Clear();
            algo.Portfolio.SetAccountCurrency("EUR");
            algo.Portfolio.SetCash(10000);
            algo.Portfolio.SetCash(Currencies.USD, nonAccountCurrencyCash, 0.88m);
            var security = InitAndGetSecurity(algo, 0);
            Assert.AreEqual(10000m + algo.Portfolio.CashBook[Currencies.USD].ValueInAccountCurrency,
                algo.Portfolio.TotalPortfolioValue);

            var quantity = security.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, security, OrderDirection.Buy)).Value;

            Assert.AreEqual(10000m + algo.Portfolio.CashBook[Currencies.USD].ValueInAccountCurrency,
                quantity);
        }

        [Test]
        public void NonAccountCurrencyFees()
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 0);
            algo.SetCash("EUR", 0, 100);
            security.FeeModel = new NonAccountCurrencyCustomFeeModel();

            // ((100000 - 100 * 100) * 2 * 0.9975 / (25)
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            Assert.AreEqual(7182m, actual);
            // ((100000 - 100 * 100) * 2 / (25)
            var quantity = security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(
                algo.Portfolio, security, 1m, 0).Quantity;
            Assert.AreEqual(7200m, quantity);

            // the maximum order quantity can be executed
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(quantity, security, algo)); ;
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void GetMaximumOrderQuantityForTargetDeltaBuyingPower_NoHoldings(int side)
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5);

            // we use our entire buying power
            var buyingPower = algo.Portfolio.MarginRemaining * side;
            var actual = security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(
                new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(algo.Portfolio,
                    security,
                    buyingPower,
                    0)).Quantity;

            // (100000 * 2 ) / 25 =8k - 1 fees
            Assert.AreEqual(7999 * side, actual);
        }

        [TestCase(100, 510, false)]
        [TestCase(-100, 510, false)]
        [TestCase(-100, 50000, true)]
        [TestCase(100, -510, false)]
        [TestCase(-100, -510, false)]
        [TestCase(100, -50000, true)]
        public void GetMaximumOrderQuantityForTargetDeltaBuyingPower_WithHoldings(decimal quantity, decimal buyingPowerDelta, bool invertsSide)
        {
            // TPV = 100k
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 0);

            // SPY @ $25 * Quantity Shares = Holdings
            // Quantity = 100 -> 2500; TPV = 102500
            // Quantity = -100 -> -2500; TPV = 97500
            security.Holdings.SetHoldings(security.Price, quantity);

            // Used Buying Power = Holdings / Leverage
            // Target BP = Used BP + buyingPowerDelta
            // Target Holdings = Target BP / Unit
            // Max Order For Delta BP = Target Holdings - Current Holdings

            // EX. -100 Quantity, 510 BP Delta.
            // Used BP = -2500 / 2 = -1250
            // Target BP = -1250 + 510 = -740
            // Target Holdings = -740 / 12.5 = -59.2 -> ~-59
            // Max Order = -59 - (-100)  = 41
            var actual = security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(
                new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(algo.Portfolio,
                    security,
                    buyingPowerDelta,
                    0)).Quantity;

            // Calculate expected using logic above
            var targetBuyingPower = ((quantity * (security.Price / security.Leverage)) + buyingPowerDelta);
            var targetHoldings = (targetBuyingPower / (security.Price / security.Leverage));
            targetHoldings -= (targetHoldings % security.SymbolProperties.LotSize);

            var expectedQuantity = targetHoldings - quantity;


            Assert.AreEqual(expectedQuantity, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, security, algo));

            if (invertsSide)
            {
                Assert.AreNotEqual(Math.Sign(quantity), Math.Sign(actual));
            }
        }

        [TestCase(true, 1, 1)]
        [TestCase(true, 1, -1)]
        [TestCase(true, -1, -1)]
        [TestCase(true, -1, 1)]
        // reducing the position to target 0 is valid
        [TestCase(false, 0, -1)]
        [TestCase(false, 0, 1)]
        public void NegativeMarginRemaining(bool isError, int target, int side)
        {
            var algo = GetAlgorithm();
            var security = InitAndGetSecurity(algo, 5);
            security.Holdings.SetHoldings(security.Price, 1000 * side);
            algo.Portfolio.CashBook.Add(algo.AccountCurrency, -100000, 1);
            var fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(fakeOrderProcessor);

            Assert.IsTrue(algo.Portfolio.MarginRemaining < 0);

            var quantity = security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(
                new GetMaximumOrderQuantityForTargetBuyingPowerParameters(algo.Portfolio,
                    security,
                    target * side,
                    0)).Quantity;
            if (!isError)
            {
                Assert.AreEqual(1000 * side * -1, quantity);
            }
            else
            {
                // even if we don't have margin 'GetMaximumOrderQuantityForTargetBuyingPower' doesn't care
                Assert.AreNotEqual(0, quantity);
            }

            var order = new MarketOrder(security.Symbol, quantity, new DateTime(2020, 1, 1));
            fakeOrderProcessor.AddTicket(order.ToOrderTicket(algo.Transactions));
            var actual = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algo.Portfolio,
                    security,
                    order));
            Assert.AreEqual(!isError, actual.IsSufficient);
        }

        private static QCAlgorithm GetAlgorithm()
        {
            SymbolCache.Clear();
            // Initialize algorithm
            var algo = new QCAlgorithm();
            algo.SetFinishedWarmingUp();
            _fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(_fakeOrderProcessor);
            return algo;
        }

        private static Security InitAndGetSecurity(QCAlgorithm algo, decimal fee, SecurityType securityType = SecurityType.Equity, string symbol = "SPY", DateTime? time = null)
        {
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            Security security;
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
                security = algo.AddFuture(symbol == "SPY" ? "ES" : symbol);
                _symbol = security.Symbol;
            }
            else
            {
                throw new Exception("SecurityType not implemented");
            }

            security.FeeModel = new ConstantFeeModel(fee);
            Update(security, 25, time);
            return security;
        }

        private static void Update(Security security, decimal close, DateTime? time = null)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = time ?? DateTime.Now,
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

        internal class NonAccountCurrencyCustomFeeModel : FeeModel
        {
            public string FeeCurrency = "EUR";
            public decimal FeeAmount = 100m;

            public override OrderFee GetOrderFee(OrderFeeParameters parameters)
            {
                return new OrderFee(new CashAmount(FeeAmount, FeeCurrency));
            }
        }
    }
}
