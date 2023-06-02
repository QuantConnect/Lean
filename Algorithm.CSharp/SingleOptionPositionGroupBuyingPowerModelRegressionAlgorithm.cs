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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that for single-asset position groups, the buying power models
    /// (<see cref="PositionGroupBuyingPowerModel"/>, <see cref="SecurityPositionGroupBuyingPowerModel"/>, and <see cref="BuyingPowerModel"/>)
    /// compute the same quantity for a given delta buying power.
    /// </summary>
    public class SingleOptionPositionGroupBuyingPowerModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 30);
            SetCash(1000000);

            var equitySymbol = AddEquity("GOOG").Symbol;

            var option = AddOption(equitySymbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested || !slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                return;
            }

            var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                .GroupBy(x => x.Expiry)
                .OrderBy(grouping => grouping.Key)
                .First()
                .OrderByDescending(x => x.Strike)
                .ToList();
            var contractSymbol = callContracts[0].Symbol;

            // 1. Test starting from a long position
            var quantity = 10;
            MarketOrder(contractSymbol, quantity);

            var security = Securities[contractSymbol];
            var positionGroup = Portfolio.Positions.Groups.Single();

            TestQuantityForDeltaBuyingPowerForPositionGroup(positionGroup, security);

            // 2. Test starting from a short position
            quantity = -10;
            MarketOrder(contractSymbol, quantity - positionGroup.Quantity);

            positionGroup = Portfolio.Positions.Groups.Single();
            if (positionGroup.Positions.Single().Quantity != quantity)
            {
                throw new Exception($@"Expected position group quantity to be {quantity} but was {positionGroup.Quantity}");
            }

            TestQuantityForDeltaBuyingPowerForPositionGroup(positionGroup, security);
        }

        private void TestQuantityForDeltaBuyingPowerForPositionGroup(IPositionGroup positionGroup, Security security)
        {
            var absQuantity = Math.Abs(positionGroup.Quantity);
            var initialMarginPerUnit = ((OptionInitialMargin)positionGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(Portfolio, positionGroup))).TotalValue / absQuantity;
            var maintenanceMarginPerUnit = positionGroup.BuyingPowerModel.GetMaintenanceMargin(Portfolio, positionGroup) / absQuantity;

            for (var expectedQuantity = 1; expectedQuantity <= absQuantity; expectedQuantity++)
            {
                // Test going in the same direction (longer or shorter):
                // positive delta and expected quantity, to increment the position towards the current side
                var deltaBuyingPower = initialMarginPerUnit * expectedQuantity * 1.05m;
                // Adjust the delta buying power:
                // GetMaximumLotsForDeltaBuyingPower will add the delta buying power to the maintenance margin and used that as a target margin,
                // but then GetMaximumLotsForTargetBuyingPower will work with initial margin requirement so we make sure the resulting quantity
                // can be ordered. In order to match this, we need to adjust the delta buying power by the difference between the initial margin
                // requirement  and maintenance margin.
                deltaBuyingPower += (initialMarginPerUnit - maintenanceMarginPerUnit) * absQuantity;
                PerfomQuantityCalculations(positionGroup, security, expectedQuantity, deltaBuyingPower);

                // Test going towards the opposite side until liquidated:
                // negative delta and expected quantity to reduce the position
                deltaBuyingPower = -initialMarginPerUnit * expectedQuantity * 0.95m;
                deltaBuyingPower += (initialMarginPerUnit - maintenanceMarginPerUnit) * absQuantity;
                PerfomQuantityCalculations(positionGroup, security, -expectedQuantity, deltaBuyingPower);
            }
        }

        private void PerfomQuantityCalculations(IPositionGroup positionGroup, Security security, int expectedQuantity,
            decimal deltaBuyingPower)
        {
            // We use the custom TestPositionGroupBuyingPowerModel class here because the default buying power model for position groups is the
            // OptionStrategyPositionGroupBuyingPowerModel, which does not support single-leg positions yet.
            var positionQuantityForDeltaWithPositionGroupBuyingPowerModel = positionGroup.BuyingPowerModel
                .GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(Portfolio, positionGroup, deltaBuyingPower,
                    minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Debug($"Expected quantity: {expectedQuantity}  --  Actual: {positionQuantityForDeltaWithPositionGroupBuyingPowerModel}");

            if (positionQuantityForDeltaWithPositionGroupBuyingPowerModel != expectedQuantity)
            {
                throw new Exception($@"Expected position quantity for delta buying power to be {expectedQuantity} but was {
                    positionQuantityForDeltaWithPositionGroupBuyingPowerModel}");
            }

            var signedDeltaBuyingPower = positionGroup.Positions.Single().Quantity < 0 ? -deltaBuyingPower : deltaBuyingPower;
            var positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel = new SecurityPositionGroupBuyingPowerModel()
                .GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(Portfolio, positionGroup, signedDeltaBuyingPower,
                    minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            var positionQuantityForDeltaWithSecurityBuyingPowerModel = security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(
                new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(Portfolio, security, signedDeltaBuyingPower,
                    minimumOrderMarginPortfolioPercentage: 0)).Quantity;

            var expectedSingleSecurityModelsQuantity = signedDeltaBuyingPower < 0 ? -Math.Abs(expectedQuantity) : Math.Abs(expectedQuantity);

            if (positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel != expectedSingleSecurityModelsQuantity ||
                positionQuantityForDeltaWithSecurityBuyingPowerModel != expectedSingleSecurityModelsQuantity)
            {
                throw new Exception($@"Expected order quantity for delta buying power calls from default buying power models to return {
                    expectedSingleSecurityModelsQuantity}. Results were:" +
                    $"    \nSecurityPositionGroupBuyingPowerModel: {positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel}" +
                    $"    \nBuyingPowerModel: {positionQuantityForDeltaWithSecurityBuyingPowerModel}\n");
            }
        }

        private class TestPositionGroupBuyingPowerModel : PositionGroupBuyingPowerModel
        {
            public override InitialMargin GetInitialMarginRequiredForOrder(PositionGroupInitialMarginForOrderParameters parameters)
            {
                var initialMarginRequirement = 0m;
                foreach (var position in parameters.PositionGroup)
                {
                    var security = parameters.Portfolio.Securities[position.Symbol];
                    initialMarginRequirement += security.BuyingPowerModel.GetInitialMarginRequiredForOrder(
                        new InitialMarginRequiredForOrderParameters(parameters.Portfolio.CashBook, security, parameters.Order)
                    );
                }

                return initialMarginRequirement;
            }

            public override InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters)
            {
                var initialMarginRequirement = 0m;
                foreach (var position in parameters.PositionGroup)
                {
                    var security = parameters.Portfolio.Securities[position.Symbol];
                    initialMarginRequirement += security.BuyingPowerModel.GetInitialMarginRequirement(
                        security, position.Quantity
                    );
                }

                return initialMarginRequirement;
            }

            public override MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters)
            {
                var buyingPower = 0m;
                foreach (var position in parameters.PositionGroup)
                {
                    var security = parameters.Portfolio.Securities[position.Symbol];
                    var result = security.BuyingPowerModel.GetMaintenanceMargin(
                        MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, position.Quantity)
                    );

                    buyingPower += result;
                }

                return buyingPower;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2973376;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.11%"},
            {"Compounding Annual Return", "-2.852%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.061%"},
            {"Sharpe Ratio", "-5.935"},
            {"Probabilistic Sharpe Ratio", "0.982%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.022"},
            {"Beta", "0.007"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.046"},
            {"Tracking Error", "0.084"},
            {"Treynor Ratio", "-3.26"},
            {"Total Fees", "$7.50"},
            {"Estimated Strategy Capacity", "$49000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMML01JA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.45%"},
            {"OrderListHash", "8c49d2f91fd6736f968bc068f2cc188d"}
        };
    }
}
