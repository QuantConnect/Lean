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
using Castle.Core.Internal;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    // The tests have been verified using the CBOE Margin Calculator
    // http://www.cboe.com/trading-tools/calculators/margin-calculator

    [TestFixture]
    public class OptionMarginBuyingPowerModelTests
    {
        // Test class to enable calling protected methods

        [Test]
        public void OptionMarginBuyingPowerModelInitializationTests()
        {
            var option = CreateOption(Symbols.SPY_P_192_Feb19_2016);
            var buyingPowerModel = new OptionMarginModel();

            // we test that options dont have leverage (100%) and it cannot be changed
            Assert.AreEqual(1m, buyingPowerModel.GetLeverage(option));
            Assert.Throws<InvalidOperationException>(() => buyingPowerModel.SetLeverage(option, 10m));
            Assert.AreEqual(1m, buyingPowerModel.GetLeverage(option));
        }

        [Test]
        public void TestLongCallsPuts()
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = CreateOption(Symbols.SPY_P_192_Feb19_2016);
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Underlying = equity;
            optionPut.Holdings.SetHoldings(1m, 2);

            var optionCall = CreateOption(Symbols.SPY_C_192_Feb19_2016);
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(1.5m, 2);

            var buyingPowerModel = new OptionMarginModel();

            // we expect long positions to be 100% charged.
            Assert.AreEqual(optionPut.Holdings.AbsoluteHoldingsValue, buyingPowerModel.GetInitialMarginRequirement(optionPut, optionPut.Holdings.Quantity));
            Assert.AreEqual(optionCall.Holdings.AbsoluteHoldingsValue, buyingPowerModel.GetInitialMarginRequirement(optionCall, optionCall.Holdings.Quantity));

            // long option position have zero maintenance margin requirement
            Assert.AreEqual(0m, buyingPowerModel.GetMaintenanceMargin(optionPut));
            Assert.AreEqual(0m, buyingPowerModel.GetMaintenanceMargin(optionCall));
        }

        [Test]
        public void TestShortCallsITM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 196m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionCall = CreateOption(Symbols.SPY_C_192_Feb19_2016);
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(price, -2);

            var buyingPowerModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            // they do not include premium since the user gets paid for the premium up front.
            // Margin = quantity * contract multiplier * [option price + MAX(A, B)]
            //      A = 20% * underlying price - OTM amount = 0.2 * 196 - 0 = 39.2
            //      B = 10% * underlying price = 0.1 * 196 = 19.6
            // Margin = 2 * 100 * (14 + MAX(39.2, 19.6)) = 10640
            Assert.AreEqual(10640m, buyingPowerModel.GetMaintenanceMargin(optionCall));
        }

        [Test]
        public void TestShortCallsOTM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 180m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionCall = CreateOption(Symbols.SPY_C_192_Feb19_2016);
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(price, -2);

            var buyingPowerModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (14 + 0.2 * 180 - (192 - 180)) = 7600
            Assert.AreEqual(7600, (double)buyingPowerModel.GetMaintenanceMargin(optionCall), 0.01);
        }

        [Test]
        public void TestShortPutsITM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 182m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = CreateOption(Symbols.SPY_P_192_Feb19_2016);
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Underlying = equity;
            optionPut.Holdings.SetHoldings(price, -2);

            var buyingPowerModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (14 + 0.2 * 182) = 10080
            Assert.AreEqual(10080m, buyingPowerModel.GetMaintenanceMargin(optionPut));
        }

        [Test]
        public void TestShortPutsOTM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 196m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = CreateOption(Symbols.SPY_P_192_Feb19_2016);
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Underlying = equity;
            optionPut.Holdings.SetHoldings(price, -2);

            var buyingPowerModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (14 + 0.2 * 196 - (196 - 192)) = 9840
            Assert.AreEqual(9840, (double)buyingPowerModel.GetMaintenanceMargin(optionPut), 0.01);
        }

        [Test]
        public void TestShortPutFarITM()
        {
            const decimal price = 0.18m;
            const decimal underlyingPrice = 200m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = CreateOption(equity, OptionRight.Put, 207m);
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Holdings.SetHoldings(price, -2);

            var buyingPowerModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (0.18 + 0.2 * 200) = 8036
            Assert.AreEqual(8036, (double)buyingPowerModel.GetMaintenanceMargin(optionPut), 0.01);
        }

        [Test]
        public void TestShortPutMovingFarITM()
        {
            const decimal optionPriceStart = 4.68m;
            const decimal underlyingPriceStart = 192m;
            const decimal optionPriceEnd = 0.18m;
            const decimal underlyingPriceEnd = 200m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPriceStart });

            var optionPut = CreateOption(equity, OptionRight.Put, 207m);
            optionPut.SetMarketPrice(new Tick { Value = optionPriceStart });
            optionPut.Holdings.SetHoldings(optionPriceStart, -2);

            var buyingPowerModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (4.68 + 0.2 * 192) = 8616
            Assert.AreEqual(8616, (double)buyingPowerModel.GetMaintenanceMargin(optionPut), 0.01);

            equity.SetMarketPrice(new Tick { Value = underlyingPriceEnd });
            optionPut.SetMarketPrice(new Tick { Value = optionPriceEnd });

            // short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (4.68 + 0.2 * 200) = 8936
            Assert.AreEqual(8936, (double)buyingPowerModel.GetMaintenanceMargin(optionPut), 0.01);
        }

        // ITM
        [TestCase(OptionRight.Call, 300, 115.75, 415, 19800)] // IB: 19837
        // OTM
        [TestCase(OptionRight.Put, 300, 0.45, 415, 3000)] // IB: 3044
        // ITM
        [TestCase(OptionRight.Call, 390, 27.5, 415, 11000)] // IB: 11022
        // OTM
        [TestCase(OptionRight.Put, 390, 1.85, 415, 6000)] // IB: 6042
        // OTM
        [TestCase(OptionRight.Call, 430, 0.85, 415, 6800)] // IB: 6803
        // ITM
        [TestCase(OptionRight.Put, 430, 16.80, 415, 9900)] // IB: 9929
        public void ShortOptionsMargin(OptionRight optionRight, decimal strikePrice, decimal optionPrice, decimal underlyingPrice,
            double expectedUnitMargin)
        {
            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var option = CreateOption(equity, optionRight, strikePrice);
            option.SetMarketPrice(new Tick { Value = optionPrice });
            option.Holdings.SetHoldings(optionPrice, -1);

            var buyingPowerModel = new OptionMarginModel();

            Assert.AreEqual(expectedUnitMargin, (double)buyingPowerModel.GetMaintenanceMargin(option), delta: 0.05 * expectedUnitMargin);
            Assert.AreEqual(10 * expectedUnitMargin,
                (double)buyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(option, -10)).Value,
                delta: 0.05 * 10 * expectedUnitMargin);
        }

        // ITM
        [TestCase(OptionRight.Call, 3800, 750, 4550, 143000)] // IB: 143275
        [TestCase(OptionRight.Put, 3800, 0.05, 4550, 38000)] // IB: 38000
        // OTM
        [TestCase(OptionRight.Call, 5000, 0.05, 4550, 45500)] // IB: 45537
        [TestCase(OptionRight.Put, 5000, 445, 4550, 112800)] // IB: 112876
        public void ShortIndexOptionsMargin(OptionRight optionRight, decimal strikePrice, decimal optionPrice, decimal underlyingPrice,
            double expectedUnitMargin)
        {
            var index = CreateIndex();
            index.SetMarketPrice(new Tick { Value = underlyingPrice });

            var indexOption = CreateOption(index, optionRight, strikePrice);
            indexOption.SetMarketPrice(new Tick { Value = optionPrice });
            indexOption.Holdings.SetHoldings(optionPrice, -1);

            var buyingPowerModel = new OptionMarginModel();

            Assert.AreEqual(expectedUnitMargin, (double)buyingPowerModel.GetMaintenanceMargin(indexOption), delta: 0.05 * expectedUnitMargin);
            Assert.AreEqual(10 * expectedUnitMargin,
                (double)buyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(indexOption, -10)).Value,
                delta: 0.05 * 10 * expectedUnitMargin);
        }

        [TestCase(0)]
        [TestCase(10000)]
        public void NonAccountCurrency_GetBuyingPower(decimal nonAccountCurrencyCash)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, nonAccountCurrencyCash, 0.88m);

            var option = algorithm.AddOption("SPY");

            var buyingPowerModel = new OptionMarginModel();
            var quantity = buyingPowerModel.GetBuyingPower(new BuyingPowerParameters(
                algorithm.Portfolio, option, OrderDirection.Buy));

            Assert.AreEqual(10000m + algorithm.Portfolio.CashBook[Currencies.USD].ValueInAccountCurrency,
                quantity.Value);
        }

        // For -1.5% target (15k), we can short -2 contracts for 478 margin requirement per unit
        [TestCase(0, -2, -.015)] // Open Short (0 + -2 = -2)
        [TestCase(-1, -1, -.015)] // Short to Shorter (-1 + -1 = -2)
        [TestCase(-2, 0, -.015)] // No action
        [TestCase(2, -4, -.015)] // Long To Short (2 + -4 = -2)

        // -40% Target (~-400k), we can short -58 contracts for 478 margin requirement per unit
        [TestCase(0, -58, -0.40)] // Open Short (0 + -58 = -58)
        [TestCase(-2, -56, -0.40)] // Short to Shorter (-2 + -56 = -58)
        [TestCase(2, -60, -0.40)] // Long To Short (2 + -60 = -58)

        // 40% Target (~400k), we can buy 836 contracts
        [TestCase(0, 836, 0.40)] // Open Long (0 + 836 = 836)
        [TestCase(-2, 838, 0.40)] // Short to Long (-2 + 838 = 836)
        [TestCase(2, 834, 0.40)] // Long To Longer (2 + 834 = 836)

        // ~0.04% Target (~400). This is below the needed margin for one unit. We end up at 0 holdings for all cases.
        [TestCase(0, 0, 0.0004)] // Open Long (0 + 0 = 0)
        [TestCase(-2, 2, 0.0004)] // Short to Long (-2 + 2 = 0)
        [TestCase(2, -2, 0.0004)] // Long To Longer (2 + -2 = 0)
        public void CallOTM_MarginRequirement(int startingHoldings, int expectedOrderSize, decimal targetPercentage)
        {
            // Initialize algorithm
            var algorithm = new QCAlgorithm();
            algorithm.SetFinishedWarmingUp();
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());

            algorithm.SetCash(1000000);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var optionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 411m, DateTime.UtcNow);
            var option = algorithm.AddOptionContract(optionSymbol);

            option.Holdings.SetHoldings(4.74m, startingHoldings);
            Assert.GreaterOrEqual(algorithm.Portfolio.MarginRemaining, 0);
            option.FeeModel = new ConstantFeeModel(0);
            option.SetLeverage(1);

            // Update option data
            UpdatePrice(option, 4.78m);

            // Update the underlying data
            UpdatePrice(option.Underlying, 395.51m);

            var model = new OptionMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetBuyingPower(algorithm.Portfolio, option, targetPercentage, 0);
            Assert.AreEqual(expectedOrderSize, result.Quantity);

            var initialPortfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            var initialMarginUsed = algorithm.Portfolio.TotalMarginUsed;
            option.Holdings.SetHoldings(4.74m, result.Quantity + startingHoldings);

            if (option.Holdings.Invested)
            {
                Assert.LessOrEqual(Math.Abs(initialMarginUsed - algorithm.Portfolio.TotalMarginUsed), initialPortfolioValue * Math.Abs(targetPercentage));
            }
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void GetsMaintenanceMarginForAPotentialShortPositionWithoutInitialHoldings(decimal initialHoldings)
        {
            // Computing the maintenance margin for a potential position is useful because it will be used to check whether there is
            // enough available buying power to open said new position.

            const decimal price = 1.6m;
            const decimal underlyingPrice = 410m;

            var equity = CreateEquity();
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionCall = CreateOption(equity, OptionRight.Call, 408m);
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Holdings.SetHoldings(price, initialHoldings);

            var buyingPowerModel = new OptionMarginModel();

            if (initialHoldings == 0)
            {
                // No holdings for the option, so no maintenance margin expected
                Assert.AreEqual(0m, buyingPowerModel.GetMaintenanceMargin(optionCall));
            }
            else
            {
                // Margin = 10 * 100 * (1.6 + 0.2 * 410) = 83600
                Assert.AreEqual(83600m, buyingPowerModel.GetMaintenanceMargin(optionCall));
            }

            // Short option positions are very expensive in terms of margin.
            // Margin = 2 * 100 * (1.6 + 0.2 * 410) = 16720
            Assert.AreEqual(16720m, buyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(optionCall, -2)).Value);
        }

        // OTM
        [TestCase(1, 3500, 140)] // IB: 0 (GetInitialMarginRequirement() returns the value with the premium)
        [TestCase(-1, 3500, -52340)] // IB: 40781
        // ITM
        [TestCase(1, 3450, 140)] // IB: 0 (GetInitialMarginRequirement() returns the value with the premium)
        [TestCase(-1, 3450, -37340)] // IB: 36081
        public void GetInitialMarginRequiredForOrderWithIndexOption(decimal quantity, decimal strikePrice, decimal expectedInitialMargin)
        {
            var price = 1.40m;
            var underlyingPrice = 17400m;

            var indexSymbol = Symbol.Create("NDX", SecurityType.Index, Market.USA);
            var index = CreateIndex(indexSymbol);
            index.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = CreateOption(index, OptionRight.Put, strikePrice, "NQX");
            optionPut.SetMarketPrice(new Tick { Value = price });
            var buyingPowerModel = new OptionMarginModel();
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var initialMargin = buyingPowerModel.GetInitialMarginRequirement(optionPut, quantity);

            Assert.AreEqual((double)expectedInitialMargin, (double)initialMargin, delta: 0.01);
        }

        private static void UpdatePrice(Security security, decimal close)
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

        private static QuantConnect.Securities.Equity.Equity CreateEquity()
        {
            var tz = TimeZones.NewYork;
            return new QuantConnect.Securities.Equity.Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }

        private static Option CreateOption(Security underlying, OptionRight optionRight, decimal strikePrice, string targetOption = null)
        {
            var tz = TimeZones.NewYork;
            var optionSymbol = targetOption.IsNullOrEmpty() ? Symbol.CreateOption(underlying.Symbol, Market.USA, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2015, 02, 27)) : Symbol.CreateOption(underlying.Symbol, targetOption, Market.USA, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2015, 02, 27));
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), optionSymbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(Market.USA, optionSymbol, optionSymbol.SecurityType, "USD")),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            option.Underlying = underlying;

            return option;
        }

        private static Option CreateOption(Symbol symbol)
        {
            var tz = TimeZones.NewYork;
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties("", Currencies.USD, 100, 0.01m, 1),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            return option;
        }

        private static QuantConnect.Securities.Index.Index CreateIndex(Symbol symbol = null)
        {
            var tz = TimeZones.NewYork;
            return new QuantConnect.Securities.Index.Index(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(typeof(TradeBar), symbol ?? Symbols.SPX, Resolution.Minute, tz, tz, true, false, false),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }
    }
}
