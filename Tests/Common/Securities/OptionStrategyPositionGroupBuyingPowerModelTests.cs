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
            _algorithm.SetCash(1000000);
            _algorithm.SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));
            _portfolio = _algorithm.Portfolio;

            _equity = _algorithm.AddEquity("SPY");

            var strike = 200m;
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

            _algorithm.SetCash(100000);
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

        /// <summary>
        /// Test cases for the <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetInitialMarginRequirement"/> method.
        ///
        /// TODO: We should come back and revisit these test cases to make sure they are correct.
        /// The approximate values from IB for the prices used in the test are in the comments.
        /// For instance, see the test case for the CoveredCall strategy. The margin values do not match IB's values.
        ///
        /// Test cases marked as explicit will fail if ran, they have an approximate expected value based on IB's margin requirements.
        /// </summary>
        private static readonly TestCaseData[] InitialMarginRequirementsTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedInitialMarginRequirement
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 1, 10000m).Explicit(),          // IB:  10282.15
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -1, 11200m),                    // IB:  12338.58
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 1, 12000m).Explicit(),           // IB:  12331.38
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -1, 10000m).Explicit(),          // IB:  10276.15
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 1, 0m).Explicit(),           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -1, 0m),                     // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -1, 0m).Explicit(),          // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 1, 0m).Explicit(),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -1, 3000m).Explicit(),             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.Strangle, 1, 0m).Explicit(),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -1, 3000m).Explicit(),             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -1, 0m).Explicit(),           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 1, 0m).Explicit(),       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -1, 0m),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 1, 0m),                        // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 1, 1000m),                // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -1, 0m),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 1, 0m),                   // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -1, 3000m).Explicit(),    // IB:  3001.6
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 1, 1000m),                       // IB:  1017.62
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -1, 0m),                         // IB:  0
        };

        [TestCaseSource(nameof(InitialMarginRequirementsTestCases))]
        public void GetsInitialMarginRequirement(OptionStrategyDefinition optionStrategyDefinition, int quantity,
            decimal expectedInitialMarginRequirement)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity);

            var initialMarginRequirement = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual(expectedInitialMarginRequirement, initialMarginRequirement.Value);
        }

        /// <summary>
        /// Test cases for the <see cref="OptionStrategyPositionGroupBuyingPowerModel.GetMaintenanceMargin"/> method.
        ///
        /// TODO: We should come back and revisit these test cases to make sure they are correct.
        /// The approximate values from IB for the prices used in the test are in the comments.
        /// For instance, see the test case for the CoveredCall strategy. The margin values do not match IB's values.
        ///
        /// Test cases marked as explicit will fail if ran, they have an approximate expected value based on IB's margin requirements.
        /// </summary>
        private static readonly TestCaseData[] MaintenanceMarginTestCases = new[]
        {
            // OptionStrategyDefinition, initialHoldingsQuantity, expectedMaintenanceMargin
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 1, 10000m).Explicit(),          // IB:  10282.15
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -1, 3000m).Explicit(),          // IB:  3000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 1, 3000m).Explicit(),            // IB:  3000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -1, 1000m).Explicit(),           // IB:  10276.15
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 1, 0m).Explicit(),           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -1, 0m),                     // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -1, 1000m),                   // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -1, 0m).Explicit(),          // IB:  0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -1, 0m),                      // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, 1, 0m).Explicit(),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Straddle, -1, 3000m).Explicit(),             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.Strangle, 1, 0m).Explicit(),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.Strangle, -1, 3000m).Explicit(),             // IB:  3001.60
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 1, 0m),                       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -1, 0m).Explicit(),           // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 1, 0m).Explicit(),       // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -1, 0m),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 1, 0m),                        // IB:  0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -1, 1000m),                    // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 1, 1000m),                // IB:  1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 1, 0m),                  // IB:  0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -1, 0m),                 // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 1, 0m),                   // IB:  0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -1, 3000m).Explicit(),    // IB:  3001.6
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 1, 1000m),                       // IB:  1017.62
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -1, 0m),                         // IB:  0
        };

        [TestCaseSource(nameof(MaintenanceMarginTestCases))]
        public void GetsMaintenanceMargin(OptionStrategyDefinition optionStrategyDefinition, int quantity, decimal expectedMaintenanceMargin)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, quantity);

            var maintenanceMargin = positionGroup.BuyingPowerModel.GetMaintenanceMargin(
                new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual(expectedMaintenanceMargin, maintenanceMargin.Value);
        }

        // option strategy definition, initial position quantity, final position quantity
        private static readonly TestCaseData[] OrderQuantityForDeltaBuyingPowerTestCases = new[]
        {
            // Initial margin for CoveredCall with quantity 10 is 205000
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 205000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -205000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -205000m, -10).Explicit(),
            // Initial margin for CoveredCall with quantity -10 is 112000
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 112000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -112000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -112000m, +10).Explicit(),
            // Initial margin for CoveredPut with quantity 10 is 205000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 205000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -205000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -205000m, -10).Explicit(),
            // Initial margin for CoveredPut with quantity -10 is 205000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 205000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -205000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -205000m, +10),
            // Initial margin for BullCallSpread with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 1000m, 1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -1000m, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10000m, 10).Explicit(),
            // Initial margin for BullCallSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 0m, 0).Explicit(),
            // Initial margin for BearPutSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 0m, 0).Explicit(),
            // Initial margin for BearPutSpread with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -10000m, +10).Explicit(),
            // Initial margin for BullCallSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 0m, 0).Explicit(),
            // Initial margin for BullCallSpread with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -10000m, +10).Explicit(),
            // Initial margin for BullPutSpread with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10000m, -10).Explicit(),
            // Initial margin for BullPutSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 0m, 0).Explicit(),
            // Initial margin for Straddle with quantity 10 is 112020
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 112020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -112020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -112020m, -10).Explicit(),
            // Initial margin for Straddle with quantity -10 is 235019
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 235019m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -235019m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -235019m, +10).Explicit(),
            // Initial margin for Strangle with quantity 10 is 102020
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 102020m / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -102020m / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -102020m, -10).Explicit(),
            // Initial margin for Strangle with quantity -10 is 225020
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 225020m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -225020m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -225020m, +10).Explicit(),
            // Initial margin for ButterflyCall with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 0m, 0).Explicit(),
            // Initial margin for ButterflyCall with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -10000m, +10).Explicit(),
            // Initial margin for ShortButterflyCall with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10000m, -10).Explicit(),
            // Initial margin for ShortButterflyCall with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 0m, 0).Explicit(),
            // Initial margin for ButterflyPut with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 0m, 0).Explicit(),
            // Initial margin for ButterflyPut with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -10000m, +10).Explicit(),
            // Initial margin for ShortButterflyPut with quantity 10 is 1000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10000m, -10).Explicit(),
            // Initial margin for ShortButterflyPut with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 0m, 0).Explicit(),
            // Initial margin for CallCalendarSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 0m, 0).Explicit(),
            // Initial margin for CallCalendarSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 0m, 0).Explicit(),
            // Initial margin for PutCalendarSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 0m, 0).Explicit(),
            // Initial margin for PutCalendarSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 0m, 0).Explicit(),
            // Initial margin for IronCondor with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 10000m / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10000m / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10000m, -10).Explicit(),
            // Initial margin for IronCondor with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 0m, 0).Explicit(),
        };

        [TestCaseSource(nameof(OrderQuantityForDeltaBuyingPowerTestCases))]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPower(OptionStrategyDefinition optionStrategyDefinition,
            int initialPositionQuantity, decimal deltaBuyingPower, int expectedQuantity)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                _portfolio, positionGroup, deltaBuyingPower, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Assert.AreEqual(expectedQuantity, quantity);
        }

        // option strategy definition, initial position quantity, target buying power percent, expected quantity
        private static readonly TestCaseData[] OrderQuantityForTargetBuyingPowerTestCases = new[]
        {
            // Initial margin requirement for CoveredCall with quantity 10 is 205000
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 205000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 205000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 0m, -10),
            // Initial margin requirement for CoveredCall with quantity -10 is 112000
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 112000m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 112000m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 0m, +10),
            // Initial margin requirement for CoveredPut with quantity 10 is 205000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 205000m * 11 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 205000m * 9 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 0m, -10),
            // Initial margin requirement for CoveredPut with quantity -10 is 205000
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 205000m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 205000m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 0m, +10),
            // Initial margin requirement for BearCallSpread with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 10000m * 11 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 10000m * 9 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 0m, -10),
            // Initial margin requirement for BearCallSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 0m, 0).Explicit(),
            // Initial margin requirement for BearPutSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 0m, 0).Explicit(),
            // Initial margin requirement for BearPutSpread with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10000m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10000m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 0m, +10).Explicit(),
            // Initial margin requirement for BullCallSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 0m, 0).Explicit(),
            // Initial margin requirement for BullCallSpread with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10000m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10000m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 0m, +10).Explicit(),
            // Initial margin requirement for BullPutSpread with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 10000m * 11 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 10000m * 9 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 0m, -10),
            // Initial margin requirement for BullPutSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 0m, 0).Explicit(),
            // Initial margin requirement for Straddle with quantity 10 is 112020
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 112020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 112020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 0m, -10).Explicit(),
            // Initial margin requirement for Straddle with quantity -10 is 235019
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 235019m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 235019m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 0m, +10).Explicit(),
            // Initial margin requirement for Strangle with quantity 10 is 102020
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 102020m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 102020m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 0m, -10).Explicit(),
            // Initial margin requirement for Strangle with quantity -10 is 225020
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 225020m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 225020m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 0m, +10).Explicit(),
            // Initial margin requirement for ButterflyCall with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 0m, 0).Explicit(),
            // Initial margin requirement for ButterflyCall with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10000m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10000m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 0m, +10).Explicit(),
            // Initial margin requirement for ShortButterflyCall with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 0m, -10),
            // Initial margin requirement for ShortButterflyCall with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 0m, 0).Explicit(),
            // Initial margin requirement for ButterflyPut with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 0m, 0).Explicit(),
            // Initial margin requirement for ButterflyPut with quantity -10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10000m * 11 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10000m * 9 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 0m, +10).Explicit(),
            // Initial margin requirement for ShortButterflyPut with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 10000m * 11 / 10, +1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 10000m * 9 / 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 0m, -10),
            // Initial margin requirement for ShortButterflyPut with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 0m, 0).Explicit(),
            // Initial margin requirement for CallCalendarSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 0m, 0).Explicit(),
            // Initial margin requirement for CallCalendarSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 0m, 0).Explicit(),
            // Initial margin requirement for PutCalendarSpread with quantity 10 is 0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 0m, 0).Explicit(),
            // Initial margin requirement for PutCalendarSpread with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 0m, 0).Explicit(),
            // Initial margin requirement for IronCondor with quantity 10 is 10000
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 10000m * 11 / 10, +1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 10000m * 9 / 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 0m, -10).Explicit(),
            // Initial margin requirement for IronCondor with quantity -10 is 0
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 0m, 0).Explicit(),
        };

        [TestCaseSource(nameof(OrderQuantityForTargetBuyingPowerTestCases))]
        public void PositionGroupOrderQuantityCalculationForTargetBuyingPower(OptionStrategyDefinition optionStrategyDefinition,
            int initialPositionQuantity, decimal targetBuyingPower, int expectedQuantity)
        {
            var positionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var targetBuyingPowerPercent = targetBuyingPower / _portfolio.TotalPortfolioValue;

            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForTargetBuyingPower(new GetMaximumLotsForTargetBuyingPowerParameters(
                _portfolio, positionGroup, targetBuyingPowerPercent, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Assert.AreEqual(expectedQuantity, quantity);
        }

        private static readonly TestCaseData[] PositionGroupBuyingPowerTestCases = new[]
        {
            // option strategy definition, initial position quantity, new position quantity
            // Starting from the "initial position quantity", we want to get the buying power available for an order that would get us to
            // the "new position quantity" (if we don't take into account the initial position).
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 1), // Going from 10 to 11
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -1), // Going from 10 to 9
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -10).Explicit(), // Going from 10 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -20).Explicit(), // Going from 10 to -10
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 1).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -20).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10).Explicit(),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 20),
        };

        [TestCaseSource(nameof(PositionGroupBuyingPowerTestCases))]
        public void BuyingPowerForPositionGroupCalculation(OptionStrategyDefinition optionStrategyDefinition, int initialPositionQuantity,
            int newGroupQuantity)
        {
            var initialMargin = _portfolio.MarginRemaining;
            var initialPositionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var positionGroup = _portfolio.Positions.ResolvePositionGroups(new PositionCollection(
                initialPositionGroup.Positions.Select(position => new Position(position.Symbol,
                    position.Quantity / initialPositionQuantity * newGroupQuantity, position.UnitQuantity)))).Single();

            var finalQuantity = initialPositionQuantity + newGroupQuantity;
            OrderDirection direction;
            if (Math.Abs(finalQuantity) < Math.Abs(initialPositionQuantity))
            {
                direction = initialPositionGroup.GetPositionSide() == PositionSide.Long ? OrderDirection.Sell : OrderDirection.Buy;
            }
            else
            {
                direction = initialPositionGroup.GetPositionSide() == PositionSide.Long ? OrderDirection.Buy : OrderDirection.Sell;
            }
            var buyingPower = positionGroup.BuyingPowerModel.GetPositionGroupBuyingPower(new PositionGroupBuyingPowerParameters(_portfolio,
                positionGroup, direction));

            var initialUsedMargin = _portfolio.TotalMarginUsed;
            var initialPositionInitialMargin = initialPositionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, initialPositionGroup));
            Log.Debug($"Initial used margin: {initialUsedMargin}");
            Log.Debug($"Initial position initial margin requirement: {initialPositionInitialMargin}");
            Log.Debug($"Final quantity: {finalQuantity}");

            // Initial and final positions are in the same side
            if (Math.Sign(finalQuantity) == Math.Sign(initialPositionQuantity))
            {
                // Increasing a position
                if (Math.Abs(finalQuantity) > Math.Abs(initialPositionQuantity))
                {
                    Assert.AreEqual(initialMargin - initialUsedMargin, buyingPower.Value);
                }
                // Reducing or closing a position
                else
                {
                    var positionGroupBuyingPower = positionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                        new ReservedBuyingPowerForPositionGroupParameters(_portfolio, positionGroup));
                    Assert.AreEqual(initialMargin - initialUsedMargin + initialPositionInitialMargin + positionGroupBuyingPower, buyingPower.Value);
                }
            }
            // Switching position side
            else
            {
                Assert.AreEqual(initialMargin, buyingPower.Value);
            }
        }

        private static readonly TestCaseData[] ReservedBuyingPowerImpactTestCases = new[]
        {
            // option strategy definition, initial position quantity, new position quantity
            // Starting from the "initial position quantity", we want to get the buying power available for an order that would get us to
            // the "new position quantity" (if we don't take into account the initial position).
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, 1), // Going from 10 to 11
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -1), // Going from 10 to 9
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -10), // Going from 10 to 0
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, 10, -20), // Going from 10 to -10
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CoveredCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CoveredPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BearCallSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BearPutSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BullCallSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.BullPutSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.Straddle, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.Strangle, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyCall, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ButterflyPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.ShortButterflyPut, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.CallCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.PutCalendarSpread, -10, 20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, 1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, 10, -20),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, -1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 1),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 10),
            new TestCaseData(OptionStrategyDefinitions.IronCondor, -10, 20),
        };

        [TestCaseSource(nameof(ReservedBuyingPowerImpactTestCases))]
        public void ReservedBuyingPowerImpactCalculation(OptionStrategyDefinition optionStrategyDefinition, int initialPositionQuantity,
            int newGroupQuantity)
        {
            var initialMargin = _portfolio.MarginRemaining;
            var initialPositionGroup = SetUpOptionStrategy(optionStrategyDefinition, initialPositionQuantity);

            var positionGroup = _portfolio.Positions.ResolvePositionGroups(new PositionCollection(
                initialPositionGroup.Positions.Select(position => new Position(position.Symbol,
                    position.Quantity / initialPositionQuantity * newGroupQuantity, position.UnitQuantity)))).Single();

            var finalQuantity = initialPositionQuantity + newGroupQuantity;

            var buyingPowerImpact = positionGroup.BuyingPowerModel.GetReservedBuyingPowerImpact(new ReservedBuyingPowerImpactParameters(_portfolio,
                positionGroup, GetPositionGroupOrders(positionGroup, newGroupQuantity)));

            var initialUsedMargin = initialPositionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                new ReservedBuyingPowerForPositionGroupParameters(_portfolio, initialPositionGroup)).AbsoluteUsedBuyingPower;
            Log.Debug($"Initial used margin: {initialUsedMargin}");
            Log.Debug($"Final quantity: {finalQuantity}");

            foreach (var contemplatedChangePosition in buyingPowerImpact.ContemplatedChanges)
            {
                var position = positionGroup.SingleOrDefault(p => contemplatedChangePosition.Symbol == p.Symbol);
                Assert.IsNotNull(position);
                Assert.AreEqual(position.Quantity, contemplatedChangePosition.Quantity);
            }

            Assert.That(buyingPowerImpact.Current, Is.EqualTo(initialUsedMargin).Within(1e-18));

            // Either initial and final positions are in the same side or we are liquidating
            if (Math.Sign(finalQuantity) == Math.Sign(initialPositionQuantity) || finalQuantity == 0)
            {
                var expectedDelta = Math.Abs(newGroupQuantity * initialUsedMargin / initialPositionQuantity)
                    * (Math.Abs(finalQuantity) < Math.Abs(initialPositionQuantity) ? -1 : +1);
                Assert.That(buyingPowerImpact.Delta, Is.EqualTo(expectedDelta).Within(1e-18));
                Assert.That(buyingPowerImpact.Contemplated, Is.EqualTo(initialUsedMargin + expectedDelta).Within(1e-18));
            }
            // Switching position side
            else
            {
                var finalPositionGroup = _portfolio.Positions.ResolvePositionGroups(new PositionCollection(
                    initialPositionGroup.Positions.Select(position =>
                        position.Combine(positionGroup.Positions.Single(x => x.Symbol == position.Symbol))))).Single();
                var finalPositionGroupMargin = finalPositionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                    new ReservedBuyingPowerForPositionGroupParameters(_portfolio, finalPositionGroup)).AbsoluteUsedBuyingPower;
                var expectedDelta = finalPositionGroupMargin - initialUsedMargin;
                Assert.That(buyingPowerImpact.Delta, Is.EqualTo(expectedDelta).Within(1e-18));
                Assert.That(buyingPowerImpact.Contemplated, Is.EqualTo(finalPositionGroupMargin).Within(1e-18));
            }
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

        private List<Order> GetPositionGroupOrders(IPositionGroup positionGroup, decimal quantity)
        {
            var groupOrderManager = new GroupOrderManager(1, positionGroup.Count, quantity);
            return positionGroup.Positions.Select(position => Order.CreateOrder(new SubmitOrderRequest(
                OrderType.ComboMarket,
                position.Symbol.SecurityType,
                position.Symbol,
                position.Quantity,
                0,
                0,
                _algorithm.Time,
                "",
                groupOrderManager: groupOrderManager))).ToList();
        }

        private IPositionGroup SetUpOptionStrategy(OptionStrategyDefinition optionStrategyDefinition, int initialHoldingsQuantity)
        {
            var may172023 = new DateTime(2023, 05, 17);
            var may192023 = new DateTime(2023, 05, 19);

            var spyMay19_300Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 300, may192023));
            spyMay19_300Call.SetMarketPrice(new Tick { Value = 112m });
            var spyMay19_310Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 310, may192023));
            spyMay19_310Call.SetMarketPrice(new Tick { Value = 102m });
            var spyMay19_320Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 320, may192023));
            spyMay19_320Call.SetMarketPrice(new Tick { Value = 92m });
            var spyMay19_330Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 330, may192023));
            spyMay19_330Call.SetMarketPrice(new Tick { Value = 82m });
            var spyMay17_300Call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 300, may172023));
            spyMay17_300Call.SetMarketPrice(new Tick { Value = 112m });

            var spyMay19_300Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 300, may192023));
            spyMay19_300Put.SetMarketPrice(new Tick { Value = 0.02m });
            var spyMay19_310Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 310, may192023));
            spyMay19_310Put.SetMarketPrice(new Tick { Value = 0.02m });
            var spyMay19_320Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 320, may192023));
            spyMay19_320Put.SetMarketPrice(new Tick { Value = 0.03m });
            var spyMay17_300Put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 300, may172023));
            spyMay17_300Put.SetMarketPrice(new Tick { Value = 0.01m });

            _equity.SetMarketPrice(new Tick { Value = 410m });

            var expectedPositionGroupBPMStrategy = optionStrategyDefinition.Name;

            if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CoveredCall.Name)
            {
                _equity.Holdings.SetHoldings(_equity.Price, initialHoldingsQuantity * _callOption.ContractMultiplier);
                spyMay19_300Call.Holdings.SetHoldings(spyMay19_300Call.Price, -initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CoveredPut.Name)
            {
                _equity.Holdings.SetHoldings(_equity.Price, -initialHoldingsQuantity * _putOption.ContractMultiplier);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, -initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BearCallSpread.Name)
            {
                var shortCallOption = spyMay19_300Call;
                var longCallOption = spyMay19_310Call;

                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BullCallSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BearPutSpread.Name)
            {
                var longPutOption = spyMay19_310Put;
                var shortPutOption = spyMay19_300Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BullPutSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BullCallSpread.Name)
            {
                var shortCallOption = spyMay19_310Call;
                var longCallOption = spyMay19_300Call;

                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BearCallSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.BullPutSpread.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay19_310Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.BearPutSpread.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.Straddle.Name)
            {
                spyMay19_300Call.Holdings.SetHoldings(spyMay19_300Call.Price, initialHoldingsQuantity);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.Strangle.Name)
            {
                spyMay19_310Call.Holdings.SetHoldings(spyMay19_310Call.Price, initialHoldingsQuantity);
                spyMay19_300Put.Holdings.SetHoldings(spyMay19_300Put.Price, initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ButterflyCall.Name)
            {
                var lowerStrikeCallOption = spyMay19_300Call;
                var middleStrikeCallOption = spyMay19_310Call;
                var upperStrikeCallOption = spyMay19_320Call;

                lowerStrikeCallOption.Holdings.SetHoldings(lowerStrikeCallOption.Price, initialHoldingsQuantity);
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
                var lowerStrikeCallOption = spyMay19_300Call;
                var middleStrikeCallOption = spyMay19_310Call;
                var upperStrikeCallOption = spyMay19_320Call;

                lowerStrikeCallOption.Holdings.SetHoldings(lowerStrikeCallOption.Price, -initialHoldingsQuantity);
                middleStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, 2 * initialHoldingsQuantity);
                upperStrikeCallOption.Holdings.SetHoldings(middleStrikeCallOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ButterflyCall.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ButterflyPut.Name)
            {
                var lowerStrikePutOption = spyMay19_300Put;
                var middleStrikePutOption = spyMay19_310Put;
                var upperStrikePutOption = spyMay19_320Put;

                lowerStrikePutOption.Holdings.SetHoldings(lowerStrikePutOption.Price, initialHoldingsQuantity);
                middleStrikePutOption.Holdings.SetHoldings(middleStrikePutOption.Price, -2 * initialHoldingsQuantity);
                upperStrikePutOption.Holdings.SetHoldings(upperStrikePutOption.Price, initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ShortButterflyPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.ShortButterflyPut.Name)
            {
                // TODO: this code can be unified with the code in the ButterflyPut case and probably with the code in the ShortButterflyCall case
                var lowerStrikePutOption = spyMay19_300Put;
                var middleStrikePutOption = spyMay19_310Put;
                var upperStrikePutOption = spyMay19_320Put;

                lowerStrikePutOption.Holdings.SetHoldings(lowerStrikePutOption.Price, -initialHoldingsQuantity);
                middleStrikePutOption.Holdings.SetHoldings(middleStrikePutOption.Price, 2 * initialHoldingsQuantity);
                upperStrikePutOption.Holdings.SetHoldings(upperStrikePutOption.Price, -initialHoldingsQuantity);

                if (initialHoldingsQuantity < 0)
                {
                    expectedPositionGroupBPMStrategy = OptionStrategyDefinitions.ButterflyPut.Name;
                }
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.CallCalendarSpread.Name)
            {
                var longCallOption = spyMay19_300Call;
                var shortCallOption = spyMay17_300Call;

                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.PutCalendarSpread.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay17_300Put;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);
            }
            else if (optionStrategyDefinition.Name == OptionStrategyDefinitions.IronCondor.Name)
            {
                var longPutOption = spyMay19_300Put;
                var shortPutOption = spyMay19_310Put;
                var shortCallOption = spyMay19_320Call;
                var longCallOption = spyMay19_330Call;

                longPutOption.Holdings.SetHoldings(longPutOption.Price, initialHoldingsQuantity);
                shortPutOption.Holdings.SetHoldings(shortPutOption.Price, -initialHoldingsQuantity);
                shortCallOption.Holdings.SetHoldings(shortCallOption.Price, -initialHoldingsQuantity);
                longCallOption.Holdings.SetHoldings(longCallOption.Price, initialHoldingsQuantity);
            }

            var positionGroup = _portfolio.PositionGroups.Single();
            Assert.AreEqual(expectedPositionGroupBPMStrategy, positionGroup.BuyingPowerModel.ToString());

            return positionGroup;
        }
    }
}
