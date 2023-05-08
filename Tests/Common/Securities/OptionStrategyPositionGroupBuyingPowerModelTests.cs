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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;
using QuantConnect.Securities.Positions;
using QuantConnect.Logging;
using QuantConnect.Tests.Engine.DataFeeds;
using static QuantConnect.Tests.Engine.PerformanceBenchmarkAlgorithms;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class OptionStrategyPositionGroupBuyingPowerModelTests
    {
        private QCAlgorithm _algorithm;
        private SecurityPortfolioManager _portfolio;
        private QuantConnect.Securities.Equity.Equity _equity;
        private Option _callOption;
        private Option _putOption;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            _algorithm.SetCash(100000);
            _algorithm.SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));
            _portfolio = _algorithm.Portfolio;

            _equity = _algorithm.AddEquity("SPY");

            var strike = 300m;
            var expiry = new DateTime(2016, 1, 15);

            var callOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, strike, expiry);
            _callOption = _algorithm.AddOptionContract(callOptionSymbol);

            var putOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, strike, expiry);
            _putOption = _algorithm.AddOptionContract(putOptionSymbol);

            Log.DebuggingEnabled = true;
        }

        [Test]
        public void HasSufficientBuyingPowerForStrategyOrder([Values] bool withInitialHoldings)
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;

            var initialMargin = _portfolio.MarginRemaining;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            var initialHoldingsQuantity = withInitialHoldings ? -10 : 0;
            _callOption.Holdings.SetHoldings(1.5m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            var optionStrategy = OptionStrategies.Straddle(_callOption.Symbol.Canonical, _callOption.StrikePrice, _callOption.Expiry);

            var sufficientCaseConsidered = false;
            var insufficientCaseConsidered = false;

            // make sure these cases are considered:
            // 1. liquidating part of the position
            var partialLiquidationCaseConsidered = false;
            // 2. liquidating the whole position
            var fullLiquidationCaseConsidered = false;
            // 3. shorting more, but with margin left
            var furtherShortingWithMarginRemainingCaseConsidered = false;
            // 4. shorting even more to the point margin is no longer enough
            var furtherShortingWithNoMarginRemainingCaseConsidered = false;

            for (var strategyQuantity = Math.Abs(initialHoldingsQuantity); strategyQuantity > -30; strategyQuantity--)
            {
                var buyingPowerModel = new OptionStrategyPositionGroupBuyingPowerModel(
                    _callOption.Holdings.Quantity + strategyQuantity == 0
                        // Liquidating
                        ? null
                        : optionStrategy);
                var orders = GetStrategyOrders(strategyQuantity);

                var positionGroup = _portfolio.Positions.CreatePositionGroup(orders);

                var maintenanceMargin = buyingPowerModel.GetMaintenanceMargin(
                    new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

                var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                    new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

                Assert.AreEqual(maintenanceMargin < initialMargin, hasSufficientBuyingPowerResult.IsSufficient);

                if (hasSufficientBuyingPowerResult.IsSufficient)
                {
                    sufficientCaseConsidered = true;
                }
                else
                {
                    Assert.IsTrue(sufficientCaseConsidered, "All 'sufficient buying power' case should have been before the 'insufficient' ones");

                    insufficientCaseConsidered = true;
                }

                var newPositionQuantity = positionGroup.Quantity;
                if (newPositionQuantity == 0)
                {
                    fullLiquidationCaseConsidered = true;
                }
                else if (newPositionQuantity < 0)
                {
                    if (newPositionQuantity > initialHoldingsQuantity)
                    {
                        partialLiquidationCaseConsidered = true;
                    }
                    else if (hasSufficientBuyingPowerResult.IsSufficient)
                    {
                        furtherShortingWithMarginRemainingCaseConsidered = true;
                    }
                    else
                    {
                        furtherShortingWithNoMarginRemainingCaseConsidered = true;
                    }
                }
            }

            Assert.IsTrue(sufficientCaseConsidered, "The 'sufficient buying power' case was not considered");
            Assert.IsTrue(insufficientCaseConsidered, "The 'insufficient buying power' case was not considered");

            if (withInitialHoldings)
            {
                Assert.IsTrue(partialLiquidationCaseConsidered, "The 'partial liquidation' case was not considered");
                Assert.IsTrue(fullLiquidationCaseConsidered, "The 'full liquidation' case was not considered");
            }

            Assert.IsTrue(furtherShortingWithMarginRemainingCaseConsidered, "The 'further shorting with margin remaining' case was not considered");
            Assert.IsTrue(furtherShortingWithNoMarginRemainingCaseConsidered, "The 'further shorting with no margin remaining' case was not considered");
        }

        [Test]
        public void HasSufficientBuyingPowerForReducingStrategyOrder()
        {
            const decimal price = 1m;
            const decimal underlyingPrice = 200m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            var initialHoldingsQuantity = -10;
            _callOption.Holdings.SetHoldings(1.5m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            _algorithm.SetCash(_portfolio.TotalMarginUsed * 0.95m);

            var optionStrategy = OptionStrategies.Straddle(_callOption.Symbol.Canonical, _callOption.StrikePrice, _callOption.Expiry);
            var quantity = -initialHoldingsQuantity / 2;
            var buyingPowerModel = new OptionStrategyPositionGroupBuyingPowerModel(optionStrategy);
            var orders = GetStrategyOrders(quantity);

            var positionGroup = _portfolio.Positions.CreatePositionGroup(orders);

            var parameters = new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders);
            var availableBuyingPower = buyingPowerModel.GetPositionGroupBuyingPower(parameters.Portfolio, parameters.PositionGroup, orders.First().GroupOrderManager.Direction);
            var deltaBuyingPowerArgs = new ReservedBuyingPowerImpactParameters(parameters.Portfolio, parameters.PositionGroup, parameters.Orders);
            var deltaBuyingPower = buyingPowerModel.GetReservedBuyingPowerImpact(deltaBuyingPowerArgs).Delta;

            // Buying power should be sufficient for reducing the position, even if the delta buying power is greater than the available buying power
            Assert.Less(deltaBuyingPower, 0);
            Assert.Greater(deltaBuyingPower, availableBuyingPower);

            var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

            Assert.IsTrue(hasSufficientBuyingPowerResult.IsSufficient);
        }

        [TestCaseSource(nameof(InitialMarginRequirementsTestCases))]
        public void GetsInitialMarginRequirement(OptionStrategyDefinition optionStrategyDefinition, int quantity,
            decimal expectedInitialMarginRequirement)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity);

            var initialMarginRequirement = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual(expectedInitialMarginRequirement, initialMarginRequirement.Value);
        }

        [TestCaseSource(nameof(MaintenanceMarginTestCases))]
        public void GetsMaintenanceMargin(OptionStrategyDefinition optionStrategyDefinition, int quantity, decimal expectedMaintenanceMargin)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity);

            var maintenanceMargin = positionGroup.BuyingPowerModel.GetMaintenanceMargin(
                new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual(expectedMaintenanceMargin, maintenanceMargin.Value);
        }

        private static decimal GetDeltaBuyingPower(decimal initialUsedMargin, int initialPositionQuantity, int expectedFinalQuantity,
            out int expectedQuantity)
        {
            expectedQuantity = 0;
            if (initialUsedMargin != 0)
            {
                expectedQuantity = expectedFinalQuantity - initialPositionQuantity;
            }

            // If reducing the position, we are reducing the buying power used by the strategy, so the delta buying power is negative
            var direction = Math.Abs(expectedFinalQuantity) < Math.Abs(initialPositionQuantity) ? -1 : +1;
            return direction * Math.Abs(expectedQuantity * initialUsedMargin / initialPositionQuantity);
        }

        [TestCaseSource(nameof(GetOrderQuantityTestCases))]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPower(OptionStrategyDefinition optionStrategyDefinition,
            int initialPositionQuantity, int expectedFinalQuantity)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var initialUsedMargin = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, positionGroup));

            var deltaBuyingPower = GetDeltaBuyingPower(initialUsedMargin.Value, initialPositionQuantity, expectedFinalQuantity,
                out var expectedQuantity);

            Log.Trace($"Initial used margin: {initialUsedMargin.Value}");
            Log.Trace($"Delta buying power: {deltaBuyingPower}");

            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                _portfolio, positionGroup, deltaBuyingPower, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Log.Trace($"Expected quantity: {expectedQuantity}");
            Log.Trace($"Computed quantity: {quantity}");

            Assert.AreEqual(expectedQuantity, quantity);
        }

        [TestCaseSource(nameof(GetOrderQuantityTestCases))]
        public void PositionGroupOrderQuantityCalculationForTargetBuyingPower(OptionStrategyDefinition optionStrategyDefinition,
            int initialPositionQuantity, int expectedFinalQuantity)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var initialUsedMargin = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, positionGroup));

            var deltaBuyingPower = GetDeltaBuyingPower(initialUsedMargin.Value, initialPositionQuantity, expectedFinalQuantity,
                out var expectedQuantity);
            var targetBuyingPower = initialUsedMargin + deltaBuyingPower;
            var targetBuyingPowerPercent = _portfolio.TotalPortfolioValue != 0 ? targetBuyingPower / _portfolio.TotalPortfolioValue : 0;

            Log.Trace($"Initial used margin: {initialUsedMargin.Value}");
            Log.Trace($"Delta buying power: {deltaBuyingPower}");
            Log.Trace($"Target buying power: {targetBuyingPower}");
            Log.Trace($"Target buying power percent: {targetBuyingPowerPercent}");

            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForTargetBuyingPower(new GetMaximumLotsForTargetBuyingPowerParameters(
                _portfolio, positionGroup, targetBuyingPowerPercent, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Log.Trace($"Expected quantity: {expectedQuantity}");
            Log.Trace($"Computed quantity: {quantity}");

            Assert.AreEqual(expectedQuantity, quantity);
        }

        private List<Order> GetStrategyOrders(decimal quantity)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, quantity);
            return new List<Order>()
            {
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _callOption.Type,
                    _callOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager)),
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _putOption.Type,
                    _putOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager))
            };
        }

        private IPositionGroup SetUpOptionStrategy(OptionStrategyDefinition optionStrategyDefinition, int initialHoldingsQuantity)
        {
            const decimal callPrice = 110m;
            const decimal putPrice = 0.02m;
            const decimal underlyingPrice = 410m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = callPrice });
            _putOption.SetMarketPrice(new Tick { Value = putPrice });

            var expectedPositionGroupBPMStrategy = optionStrategyDefinition.Name;

            if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CoveredCall.Name)
            {
                _equity.Holdings.SetHoldings(underlyingPrice, initialHoldingsQuantity * _callOption.ContractMultiplier);
                _callOption.Holdings.SetHoldings(callPrice, -initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CoveredPut.Name)
            {
                _equity.Holdings.SetHoldings(underlyingPrice, -initialHoldingsQuantity * _putOption.ContractMultiplier);
                _putOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BearCallSpread.Name)
            {
                var shortCallOption = _callOption;

                var longCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, shortCallOption.StrikePrice + 10, shortCallOption.Expiry);
                var longCallOption = _algorithm.AddOptionContract(longCallOptionSymbol);
                longCallOption.SetMarketPrice(new Tick { Value = callPrice - 10 });

                shortCallOption.Holdings.SetHoldings(callPrice, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BullCallSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BearPutSpread.Name)
            {
                var shortPutOption = _putOption;

                var longPutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, shortPutOption.StrikePrice + 10, shortPutOption.Expiry);
                var longPutOption = _algorithm.AddOptionContract(longPutOptionSymbol);
                longPutOption.SetMarketPrice(new Tick { Value = putPrice });

                longPutOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BullPutSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BullCallSpread.Name)
            {
                var longCallOption = _callOption;

                var shortCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, longCallOption.StrikePrice + 10, longCallOption.Expiry);
                var shortCallOption = _algorithm.AddOptionContract(shortCallOptionSymbol);
                shortCallOption.SetMarketPrice(new Tick { Value = callPrice - 10 });

                longCallOption.Holdings.SetHoldings(callPrice, initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BearCallSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BullPutSpread.Name)
            {
                var longPutOption = _putOption;

                var shortPutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, longPutOption.StrikePrice + 10, longPutOption.Expiry);
                var shortPutOption = _algorithm.AddOptionContract(shortPutOptionSymbol);
                shortPutOption.SetMarketPrice(new Tick { Value = putPrice });

                longPutOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BearPutSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.Straddle.Name)
            {
                _callOption.Holdings.SetHoldings(callPrice, initialHoldingsQuantity);
                _putOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.Strangle.Name)
            {
                var callOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, _putOption.StrikePrice + 10, _putOption.Expiry);
                var callOption = _algorithm.AddOptionContract(callOptionSymbol);
                callOption.SetMarketPrice(new Tick { Value = callPrice - 10 });

                callOption.Holdings.SetHoldings(callOption.Price, initialHoldingsQuantity);
                _putOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ButterflyCall.Name)
            {
                var lowerStrikeCallOption = _callOption;

                var middleStrikeCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, lowerStrikeCallOption.StrikePrice + 10,
                    lowerStrikeCallOption.Expiry);
                var middleStrikeCallOption = _algorithm.AddOptionContract(middleStrikeCallOptionSymbol);

                var upperStrikeCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, middleStrikeCallOption.StrikePrice + 10,
                    middleStrikeCallOption.Expiry);
                var upperStrikeCallOption = _algorithm.AddOptionContract(upperStrikeCallOptionSymbol);

                middleStrikeCallOption.SetMarketPrice(new Tick { Value = callPrice - 10 });
                upperStrikeCallOption.SetMarketPrice(new Tick { Value = callPrice - 20 });

                lowerStrikeCallOption.Holdings.SetHoldings(callPrice, initialHoldingsQuantity);
                middleStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, -2 * initialHoldingsQuantity);
                upperStrikeCallOption.Holdings.SetHoldings(upperStrikeCallOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortButterflyCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortButterflyCall.Name)
            {
                // TODO: this code can be unified with the code in the ButterflyCall case
                var lowerStrikeCallOption = _callOption;

                var middleStrikeCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, lowerStrikeCallOption.StrikePrice + 10,
                    lowerStrikeCallOption.Expiry);
                var middleStrikeCallOption = _algorithm.AddOptionContract(middleStrikeCallOptionSymbol);

                var upperStrikeCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, middleStrikeCallOption.StrikePrice + 10,
                    middleStrikeCallOption.Expiry);
                var upperStrikeCallOption = _algorithm.AddOptionContract(upperStrikeCallOptionSymbol);

                middleStrikeCallOption.SetMarketPrice(new Tick { Value = callPrice + 10 });
                upperStrikeCallOption.SetMarketPrice(new Tick { Value = callPrice + 20 });

                lowerStrikeCallOption.Holdings.SetHoldings(callPrice, -initialHoldingsQuantity);
                middleStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, 2 * initialHoldingsQuantity);
                upperStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ButterflyCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ButterflyPut.Name)
            {
                var lowerStrikePutOption = _putOption;

                var middleStrikePutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, lowerStrikePutOption.StrikePrice + 10,
                    lowerStrikePutOption.Expiry);
                var middleStrikePutOption = _algorithm.AddOptionContract(middleStrikePutOptionSymbol);

                var upperStrikePutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, middleStrikePutOption.StrikePrice + 10,
                    middleStrikePutOption.Expiry);
                var upperStrikePutOption = _algorithm.AddOptionContract(upperStrikePutOptionSymbol);

                middleStrikePutOption.SetMarketPrice(new Tick { Value = putPrice });
                upperStrikePutOption.SetMarketPrice(new Tick { Value = putPrice });

                lowerStrikePutOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);
                middleStrikePutOption.Holdings.SetHoldings(putPrice, -2 * initialHoldingsQuantity);
                upperStrikePutOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortButterflyPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortButterflyPut.Name)
            {
                // TODO: this code can be unified with the code in the ButterflyPut case and probably with the code in the ShortButterflyCall case
                var lowerStrikePutOption = _putOption;

                var middleStrikePutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, lowerStrikePutOption.StrikePrice + 10,
                    lowerStrikePutOption.Expiry);
                var middleStrikePutOption = _algorithm.AddOptionContract(middleStrikePutOptionSymbol);

                var upperStrikePutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, middleStrikePutOption.StrikePrice + 10,
                    middleStrikePutOption.Expiry);
                var upperStrikePutOption = _algorithm.AddOptionContract(upperStrikePutOptionSymbol);

                middleStrikePutOption.SetMarketPrice(new Tick { Value = putPrice });
                upperStrikePutOption.SetMarketPrice(new Tick { Value = putPrice });

                lowerStrikePutOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);
                middleStrikePutOption.Holdings.SetHoldings(putPrice, 2 * initialHoldingsQuantity);
                upperStrikePutOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ButterflyPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CallCalendarSpread.Name)
            {
                var shortCallOption = _callOption;

                var longCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, shortCallOption.StrikePrice,
                    shortCallOption.Expiry.AddDays(30));
                var longCallOption = _algorithm.AddOptionContract(longCallOptionSymbol);

                longCallOption.SetMarketPrice(new Tick { Value = callPrice });

                shortCallOption.Holdings.SetHoldings(callPrice, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(callPrice, initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.PutCalendarSpread.Name)
            {
                var shortPutOption = _putOption;

                var longPutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, shortPutOption.StrikePrice,
                    shortPutOption.Expiry.AddDays(30));
                var longPutOption = _algorithm.AddOptionContract(longPutOptionSymbol);

                longPutOption.SetMarketPrice(new Tick { Value = 1m });

                shortPutOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);
                longPutOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.IronCondor.Name)
            {
                var longPutOption = _putOption;

                var shortPutOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, longPutOption.StrikePrice + 10, longPutOption.Expiry);
                var shortPutOption = _algorithm.AddOptionContract(shortPutOptionSymbol);

                var shortCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, shortPutOption.StrikePrice + 10, shortPutOption.Expiry);
                var shortCallOption = _algorithm.AddOptionContract(shortCallOptionSymbol);

                var longCallOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, shortCallOption.StrikePrice + 10,
                    shortCallOption.Expiry);
                var longCallOption = _algorithm.AddOptionContract(longCallOptionSymbol);

                shortPutOption.SetMarketPrice(new Tick { Value = putPrice });
                shortCallOption.SetMarketPrice(new Tick { Value = callPrice });
                longCallOption.SetMarketPrice(new Tick { Value = callPrice });

                longPutOption.Holdings.SetHoldings(putPrice, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(putPrice, -initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(callPrice, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(callPrice, initialHoldingsQuantity);
            }

            var positionGroup = _portfolio.PositionGroups.Single();
            Assert.AreEqual(expectedPositionGroupBPMStrategy, positionGroup.BuyingPowerModel.ToString());

            return positionGroup;
        }

        /// <summary>
        /// Test cases for the <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetInitialMarginRequirement"/> method.
        ///
        /// TODO: We should come back and revisit these test cases to make sure they are correct.
        /// The approximate values from IB for the prices used in the test are in the comments.
        /// For instance, see the test case for the CoveredCall strategy. The margin values do not match IB's values.
        ///
        /// Test cases marked as explicit will fail if ran, they have an approximate expected value based on IB's margin requirements.
        /// </summary>
        private static TestCaseData[] InitialMarginRequirementsTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedInitialMarginRequirement
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 1, 10000m).Explicit(),                          // IB:  10282.15
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -1, 11000m),                                    // IB:  12338.58
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 1, 12000m).Explicit(),                           // IB:  12331.38
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -1, 10000m).Explicit(),                          // IB:  10276.15
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 1, 0m).Explicit(),                           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -1, 0m),                                     // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 1, 0m),                                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -1, 1000m),                                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 1, 0m),                                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -1, 0m).Explicit(),                          // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 1, 1000m),                                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -1, 0m),                                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 1, 0m).Explicit(),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -1, 3000m).Explicit(),                             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.Strangle, 1, 0m).Explicit(),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -1, 3000m).Explicit(),                             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 1, 0m),                                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -1, 0m).Explicit(),                           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 1, 0m).Explicit(),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -1, 0m),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 1, 0m),                                        // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -1, 1000m),                                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 1, 1000m),                                // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -1, 0m),                                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 1, 0m),                                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -1, 0m),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 1, 0m),                                   // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -1, 3000m).Explicit(),                    // IB:  3001.6
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 1, 1000m),                                       // IB:  1017.62
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -1, 0m),                                         // IB:  0
        };

        /// <summary>
        /// Test cases for the <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetMaintenanceMargin"/> method.
        ///
        /// TODO: We should come back and revisit these test cases to make sure they are correct.
        /// The approximate values from IB for the prices used in the test are in the comments.
        /// For instance, see the test case for the CoveredCall strategy. The margin values do not match IB's values.
        ///
        /// Test cases marked as explicit will fail if ran, they have an approximate expected value based on IB's margin requirements.
        /// </summary>
        private static TestCaseData[] MaintenanceMarginTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedMaintenanceMargin
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 1, 10000m).Explicit(),                          // IB:  10282.15
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -1, 3000m).Explicit(),                          // IB:  3000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 1, 3000m).Explicit(),                            // IB:  3000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -1, 1000m).Explicit(),                           // IB:  10276.15
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 1, 0m).Explicit(),                           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -1, 0m),                                     // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 1, 0m),                                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -1, 1000m),                                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 1, 0m),                                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -1, 0m).Explicit(),                          // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 1, 1000m),                                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -1, 0m),                                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 1, 0m).Explicit(),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -1, 3000m).Explicit(),                             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.Strangle, 1, 0m).Explicit(),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -1, 3000m).Explicit(),                             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 1, 0m),                                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -1, 0m).Explicit(),                           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 1, 0m).Explicit(),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -1, 0m),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 1, 0m),                                        // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -1, 1000m),                                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 1, 1000m),                                // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -1, 0m),                                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 1, 0m),                                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -1, 0m),                                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 1, 0m),                                   // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -1, 3000m).Explicit(),                    // IB:  3001.6
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 1, 1000m),                                       // IB:  1017.62
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -1, 0m),                                         // IB:  0
        };

        private static TestCaseData[] GetOrderQuantityTestCases()
        {
            return new[]
            {
                // option strategy definition, initial position, delta buying power (using the buying power for a unit), expect final position
                new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 0),
                new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 11),
                new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 9),
                new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 11),
                new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 9),
                new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 11),
                new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 9),
                new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 0).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -9).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -11).Explicit(),
                new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 0).Explicit(),
            };
        }
    }
}
