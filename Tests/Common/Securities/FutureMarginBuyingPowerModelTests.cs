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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class FutureMarginBuyingPowerModelTests
    {
        // Test class to enable calling protected methods
        public class TestFutureMarginModel : FutureMarginModel
        {
            public TestFutureMarginModel(Security security = null)
                : base(security: security)
            {
            }

            public new decimal GetMaintenanceMargin(Security security)
            {
                return base.GetMaintenanceMargin(security);
            }

            public new decimal GetInitialMarginRequirement(Security security, decimal quantity)
            {
                return base.GetInitialMarginRequirement(security, quantity);
            }

            public new decimal GetInitialMarginRequiredForOrder(
                InitialMarginRequiredForOrderParameters parameters)
            {
                return base.GetInitialMarginRequiredForOrder(parameters);
            }
        }

        [Test]
        public void TestMarginForSymbolWithOneLinerHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2016, 1, 1);
            var expDate = new DateTime(2017, 1, 1);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Softs.Coffee;
            var symbol = Symbol.CreateFuture(ticker, Market.USA, expDate);

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new TestFutureMarginModel(futureSecurity);
            Assert.AreEqual(buyingPowerModel.MaintenanceOvernightMarginRequirement, buyingPowerModel.GetMaintenanceMargin(futureSecurity));
        }

        [Test]
        public void TestMarginForSymbolWithNoHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2016, 1, 1);
            var expDate = new DateTime(2017, 1, 1);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history at all
            var ticker = "NOT-A-SYMBOL";
            var symbol = Symbol.CreateFuture(ticker, Market.USA, expDate);

            var futureSecurity = new Future(SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new TestFutureMarginModel();
            Assert.AreEqual(0m, buyingPowerModel.GetMaintenanceMargin(futureSecurity));
        }

        [Test]
        public void TestMarginForSymbolWithHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2013, 1, 1);
            var expDate = new DateTime(2017, 1, 1);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have history
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var symbol = Symbol.CreateFuture(ticker, Market.USA, expDate);

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new TestFutureMarginModel(futureSecurity);
            Assert.AreEqual(buyingPowerModel.MaintenanceOvernightMarginRequirement,
                buyingPowerModel.GetMaintenanceMargin(futureSecurity));

            // now we move forward to exact date when margin req changed
            time = new DateTime(2014, 06, 13);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            Assert.AreEqual(buyingPowerModel.MaintenanceOvernightMarginRequirement, buyingPowerModel.GetMaintenanceMargin(futureSecurity));

            // now we fly beyond the last line of the history file (currently) to see how margin model resolves future dates
            time = new DateTime(2016, 06, 04);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            Assert.AreEqual(buyingPowerModel.MaintenanceOvernightMarginRequirement, buyingPowerModel.GetMaintenanceMargin(futureSecurity));
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(-1)]
        [TestCase(-10)]
        public void GetMaintenanceMargin(decimal quantity)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;

            const decimal price = 1.2345m;
            var time = new DateTime(2013, 1, 1);
            var futureSecurity = algorithm.AddFuture(ticker);
            var buyingPowerModel = new TestFutureMarginModel(futureSecurity);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, quantity);

            var res = buyingPowerModel.GetMaintenanceMargin(futureSecurity);
            Assert.AreEqual(buyingPowerModel.MaintenanceOvernightMarginRequirement * futureSecurity.Holdings.AbsoluteQuantity, res);

            // We increase the quantity * 2, maintenance margin should DOUBLE
            futureSecurity.Holdings.SetHoldings(1.5m, quantity * 2);
            res = buyingPowerModel.GetMaintenanceMargin(futureSecurity);
            Assert.AreEqual(buyingPowerModel.MaintenanceOvernightMarginRequirement * futureSecurity.Holdings.AbsoluteQuantity, res);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void GetInitialMarginRequirement(decimal quantity)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;

            const decimal price = 1.2345m;
            var time = new DateTime(2013, 1, 1);
            var futureSecurity = algorithm.AddFuture(ticker);
            var buyingPowerModel = new TestFutureMarginModel();
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, quantity);

            var initialMargin = buyingPowerModel.GetInitialMarginRequirement(futureSecurity, futureSecurity.Holdings.AbsoluteQuantity);
            Assert.IsTrue(initialMargin > 0);
            var overnightMargin = Math.Abs(buyingPowerModel.GetMaintenanceMargin(futureSecurity));

            // initial margin is greater than the maintenance margin
            Assert.Greater(initialMargin, overnightMargin);
        }

        [TestCase(10)]
        [TestCase(-10)]
        public void GetInitialMarginRequiredForOrder(decimal quantity)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;

            const decimal price = 1.2345m;
            var time = new DateTime(2013, 1, 1);
            var futureSecurity = algorithm.AddFuture(ticker);
            var buyingPowerModel = new TestFutureMarginModel(futureSecurity);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var initialMargin = buyingPowerModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algorithm.Portfolio.CashBook,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantity, algorithm.UtcTime)));

            var initialMarginExpected = buyingPowerModel.GetInitialMarginRequirement(futureSecurity, quantity);

            Assert.AreEqual(initialMarginExpected
                            + 18.50m * Math.Sign(quantity), // fees -> 10 quantity * 1.85
                initialMargin);
        }

        [TestCase(100)]
        [TestCase(-100)]
        public void MarginUsedForPositionWhenPriceDrops(decimal quantity)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            futureSecurity.Holdings.SetHoldings(20, quantity);
            Update(futureSecurity, 20, algorithm);

            var marginForPosition = futureSecurity.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(futureSecurity)).AbsoluteUsedBuyingPower;

            // Drop 40% price from $20 to $12
            Update(futureSecurity, 12, algorithm);

            var marginForPositionAfter = futureSecurity.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(futureSecurity)).AbsoluteUsedBuyingPower;

            Assert.AreEqual(marginForPosition, marginForPositionAfter);
        }

        [TestCase(100)]
        [TestCase(-100)]
        public void MarginUsedForPositionWhenPriceIncreases(decimal quantity)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            futureSecurity.Holdings.SetHoldings(20, quantity);
            Update(futureSecurity, 20, algorithm);

            var marginForPosition = futureSecurity.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(futureSecurity)).AbsoluteUsedBuyingPower;

            // Increase from $20 to $40
            Update(futureSecurity, 40, algorithm);

            var marginForPositionAfter = futureSecurity.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(futureSecurity)).AbsoluteUsedBuyingPower;

            Assert.AreEqual(marginForPosition, marginForPositionAfter);
        }

        [Test]
        public void PortfolioStatusForPositionWhenPriceDrops()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            futureSecurity.Holdings.SetHoldings(20, 100);
            Update(futureSecurity, 20, algorithm);

            var marginUsed = algorithm.Portfolio.TotalMarginUsed;
            Assert.IsTrue(marginUsed > 0);
            Assert.IsTrue(algorithm.Portfolio.TotalPortfolioValue > 0);
            Assert.IsTrue(algorithm.Portfolio.MarginRemaining > 0);

            // Drop 40% price from $20 to $12
            Update(futureSecurity, 12, algorithm);

            var expected = (12 - 20) * 100 * futureSecurity.SymbolProperties.ContractMultiplier - 1.85m * 100;
            Assert.AreEqual(futureSecurity.Holdings.UnrealizedProfit, expected);

            // we have a massive loss because of futures leverage
            Assert.IsTrue(algorithm.Portfolio.TotalPortfolioValue < 0);
            Assert.IsTrue(algorithm.Portfolio.MarginRemaining < 0);

            // margin used didn't change because for futures it relies on the maintenance margin
            Assert.AreEqual(marginUsed, algorithm.Portfolio.TotalMarginUsed);
        }

        [Test]
        public void PortfolioStatusPositionWhenPriceIncreases()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            futureSecurity.Holdings.SetHoldings(20, 100);
            Update(futureSecurity, 20, algorithm);

            var marginUsed = algorithm.Portfolio.TotalMarginUsed;
            Assert.IsTrue(marginUsed > 0);
            Assert.IsTrue(algorithm.Portfolio.TotalPortfolioValue > 0);
            Assert.IsTrue(algorithm.Portfolio.MarginRemaining > 0);

            // Increase from $20 to $40
            Update(futureSecurity, 40, algorithm);

            var expected = (40 - 20) * 100 * futureSecurity.SymbolProperties.ContractMultiplier - 1.85m * 100;
            Assert.AreEqual(futureSecurity.Holdings.UnrealizedProfit, expected);

            // we have a massive win because of futures leverage
            Assert.IsTrue(algorithm.Portfolio.TotalPortfolioValue > 0);
            Assert.IsTrue(algorithm.Portfolio.MarginRemaining > 0);

            // margin used didn't change because for futures it relies on the maintenance margin
            Assert.AreEqual(marginUsed, algorithm.Portfolio.TotalMarginUsed);
        }

        [Test]
        public void GetLeverage()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            Update(futureSecurity, 100, algorithm);
            var leverage = futureSecurity.BuyingPowerModel.GetLeverage(futureSecurity);

            Assert.AreEqual(1, leverage);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void SetLeverageThrowsException(int leverage)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);

            Assert.Throws<InvalidOperationException>(() => futureSecurity.BuyingPowerModel.SetLeverage(futureSecurity, leverage));
        }

        [Test]
        public void MarginRequirementsChangeWithDate()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            var model = futureSecurity.BuyingPowerModel as FutureMarginModel;

            Update(futureSecurity, 100, algorithm, new DateTime(2001, 01, 07));
            var initial = model.InitialOvernightMarginRequirement;
            var maintenance = model.MaintenanceOvernightMarginRequirement;
            Assert.AreEqual(810, initial);
            Assert.AreEqual(600, maintenance);

            // date previous to margin change
            Update(futureSecurity, 100, algorithm, new DateTime(2001, 12, 10));
            Assert.AreEqual(810, initial);
            Assert.AreEqual(600, maintenance);

            // new margins!
            Update(futureSecurity, 100, algorithm, new DateTime(2001, 12, 11));
            Assert.AreEqual(945, model.InitialOvernightMarginRequirement);
            Assert.AreEqual(700, model.MaintenanceOvernightMarginRequirement);
        }

        [TestCase(-1.1)]
        [TestCase(1.1)]
        public void GetMaximumOrderQuantityForTargetBuyingPower_ThrowsForInvalidTarget(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);

            Assert.Throws<InvalidOperationException>(() => futureSecurity.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(
                new GetMaximumOrderQuantityForTargetBuyingPowerParameters(algorithm.Portfolio,
                    futureSecurity,
                    target)));
        }

        [TestCase(1)]
        [TestCase(0.5)]
        [TestCase(-1)]
        [TestCase(-0.5)]
        public void GetMaximumOrderQuantityForTargetBuyingPower_NoHoldings(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            Update(futureSecurity, 100, algorithm);
            var model = futureSecurity.BuyingPowerModel as FutureMarginModel;

            // set closed market for simpler math
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));

            var quantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);

            var expected = (algorithm.Portfolio.TotalPortfolioValue * Math.Abs(target)) / model.InitialOvernightMarginRequirement - 1 * Math.Abs(target); // -1 fees
            expected -= expected % futureSecurity.SymbolProperties.LotSize;

            Assert.AreEqual(expected * Math.Sign(target), quantity);

            var request = GetOrderRequest(futureSecurity.Symbol, quantity);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            Assert.IsTrue(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, expected, DateTime.UtcNow))).IsSufficient);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void HasSufficientBuyingPowerForOrderInvalidTargets(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            // set closed market for simpler math
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));
            Update(futureSecurity, 100, algorithm);
            var model = futureSecurity.BuyingPowerModel as FutureMarginModel;

            var quantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);
            var request = GetOrderRequest(futureSecurity.Symbol, quantity);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            var result = model.HasSufficientBuyingPowerForOrder(new HasSufficientBuyingPowerForOrderParameters(
                    algorithm.Portfolio,
                    futureSecurity,
                    // we get the maximum target value 1/-1 and add a lot size it shouldn't be a valid order
                    new MarketOrder(futureSecurity.Symbol, quantity + futureSecurity.SymbolProperties.LotSize * Math.Sign(quantity), DateTime.UtcNow)));

            Assert.IsFalse(result.IsSufficient);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void GetMaximumOrderQuantityForTargetBuyingPower_TwoStep(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            // set closed market for simpler math
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));
            Update(futureSecurity, 100, algorithm);
            var expectedFinalQuantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);

            var quantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target / 2);
            futureSecurity.Holdings.SetHoldings(100, quantity);
            algorithm.Portfolio.InvalidateTotalPortfolioValue();

            var quantity2 = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);

            var request = GetOrderRequest(futureSecurity.Symbol, quantity2);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            Assert.IsTrue(futureSecurity.BuyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantity2, DateTime.UtcNow))).IsSufficient);

            // two step operation is the same as 1 step
            Assert.AreEqual(expectedFinalQuantity, quantity + quantity2);
        }

        [TestCase(1)]
        [TestCase(0.5)]
        [TestCase(-1)]
        [TestCase(-0.5)]
        public void GetMaximumOrderQuantityForTargetBuyingPower_WithHoldingsSameDirection(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            // set closed market for simpler math
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));
            futureSecurity.Holdings.SetHoldings(100, 10 * Math.Sign(target));
            Update(futureSecurity, 100, algorithm);

            var model = new TestFutureMarginModel(futureSecurity);
            futureSecurity.BuyingPowerModel = model;

            var quantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);

            var expected = (algorithm.Portfolio.TotalPortfolioValue * Math.Abs(target) - model.GetInitialMarginRequirement(futureSecurity, futureSecurity.Holdings.AbsoluteQuantity))
                           / model.InitialOvernightMarginRequirement - 1 * Math.Abs(target); // -1 fees
            expected -= expected % futureSecurity.SymbolProperties.LotSize;
            Console.WriteLine($"Expected {expected}");

            Assert.AreEqual(expected * Math.Sign(target), quantity);

            var request = GetOrderRequest(futureSecurity.Symbol, quantity);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            Assert.IsTrue(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, expected * Math.Sign(target), DateTime.UtcNow))).IsSufficient);
        }

        [TestCase(1)]
        [TestCase(0.5)]
        [TestCase(-1)]
        [TestCase(-0.5)]
        public void GetMaximumOrderQuantityForTargetBuyingPower_WithHoldingsInverseDirection(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            // set closed market for simpler math
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));
            futureSecurity.Holdings.SetHoldings(100, 10 * -1 * Math.Sign(target));
            Update(futureSecurity, 100, algorithm);

            var model = new TestFutureMarginModel(futureSecurity);
            futureSecurity.BuyingPowerModel = model;

            var quantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);

            var expected = (algorithm.Portfolio.TotalPortfolioValue * Math.Abs(target) + model.GetInitialMarginRequirement(futureSecurity, futureSecurity.Holdings.AbsoluteQuantity))
                           / model.InitialOvernightMarginRequirement - 1 * Math.Abs(target); // -1 fees
            expected -= expected % futureSecurity.SymbolProperties.LotSize;
            Console.WriteLine($"Expected {expected}");

            Assert.AreEqual(expected * Math.Sign(target), quantity);

            var request = GetOrderRequest(futureSecurity.Symbol, quantity);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            Assert.IsTrue(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, expected * Math.Sign(target), DateTime.UtcNow))).IsSufficient);
        }

        [TestCase(1)]
        [TestCase(0.5)]
        [TestCase(-1)]
        [TestCase(-0.5)]
        public void IntradayVersusOvernightMargins(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            Update(futureSecurity, 100, algorithm);
            var model = futureSecurity.BuyingPowerModel as FutureMarginModel;
            // Close market
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));

            var quantityClosedMarket = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);
            var request = GetOrderRequest(futureSecurity.Symbol, quantityClosedMarket);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            Assert.IsTrue(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantityClosedMarket, DateTime.UtcNow))).IsSufficient);

            var initialOvernight = model.InitialOvernightMarginRequirement;
            var maintenanceOvernight = model.MaintenanceOvernightMarginRequirement;

            // Open market
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 3));

            futureSecurity.Holdings.SetHoldings(100, quantityClosedMarket * 0.4m);

            Assert.IsTrue(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantityClosedMarket / 2, DateTime.UtcNow))).IsSufficient);

            Assert.Greater(initialOvernight, model.InitialIntradayMarginRequirement);
            Assert.Greater(maintenanceOvernight, model.MaintenanceIntradayMarginRequirement);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void ClosingSoonIntradayClosedMarketMargins(decimal target)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var orderProcessor = new FakeOrderProcessor();
            algorithm.Transactions.SetOrderProcessor(orderProcessor);

            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var futureSecurity = algorithm.AddFuture(ticker);
            Update(futureSecurity, 100, algorithm);
            var model = futureSecurity.BuyingPowerModel as FutureMarginModel;
            // this is important
            model.EnableIntradayMargins = true;

            // Open market
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 3));

            var quantity = algorithm.CalculateOrderQuantity(futureSecurity.Symbol, target);
            var request = GetOrderRequest(futureSecurity.Symbol, quantity);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(algorithm.Transactions, request));

            Assert.IsTrue(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantity, DateTime.UtcNow))).IsSufficient);

            // Closing soon market
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 3, 15,50, 0));

            Assert.IsFalse(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantity, DateTime.UtcNow))).IsSufficient);
            Assert.IsTrue(futureSecurity.Exchange.ExchangeOpen);
            Assert.IsTrue(futureSecurity.Exchange.ClosingSoon);

            // Close market
            futureSecurity.Exchange.SetLocalDateTimeFrontier(new DateTime(2020, 2, 1));
            Assert.IsFalse(futureSecurity.Exchange.ExchangeOpen);

            Assert.IsFalse(model.HasSufficientBuyingPowerForOrder(
                new HasSufficientBuyingPowerForOrderParameters(algorithm.Portfolio,
                    futureSecurity,
                    new MarketOrder(futureSecurity.Symbol, quantity, DateTime.UtcNow))).IsSufficient);
        }

        private static void Update(Security security, decimal close, QCAlgorithm algorithm, DateTime? time = null)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = time ?? new DateTime(2019, 1, 1),
                Symbol = security.Symbol,
                Open = close,
                High = close,
                Low = close,
                Close = close
            });
            algorithm.Portfolio.InvalidateTotalPortfolioValue();
        }

        private static SubmitOrderRequest GetOrderRequest(Symbol symbol, decimal quantity)
        {
            return new SubmitOrderRequest(OrderType.Market,
                SecurityType.Future,
                symbol,
                quantity,
                1,
                1,
                DateTime.UtcNow,
                "");
        }
    }
}
