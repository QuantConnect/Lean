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
            var initialMarginPerUnit = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(Portfolio, positionGroup) / absQuantity;

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
                PerfomQuantityCalculations(positionGroup, security, expectedQuantity, deltaBuyingPower, increasing: true);

                // Test going towards the opposite side until liquidated:
                // negative delta and expected quantity to reduce the position
                deltaBuyingPower = -initialMarginPerUnit * expectedQuantity * 0.95m;
                PerfomQuantityCalculations(positionGroup, security, -expectedQuantity, deltaBuyingPower, increasing: false);
            }
        }

        private void PerfomQuantityCalculations(IPositionGroup positionGroup, Security security, int expectedQuantity,
            decimal deltaBuyingPower, bool increasing)
        {
            var absQuantity = Math.Abs(positionGroup.Quantity);
            var initialMarginPerUnit = positionGroup.BuyingPowerModel.GetInitialMarginRequirement(Portfolio, positionGroup) / absQuantity;
            var maintenanceMarginPerUnit = positionGroup.BuyingPowerModel.GetMaintenanceMargin(Portfolio, positionGroup) / absQuantity;
            var deltaBuyingPowerAdjustment = (initialMarginPerUnit - maintenanceMarginPerUnit) * absQuantity;

            var positionQuantityForDeltaWithPositionGroupBuyingPowerModel = positionGroup.BuyingPowerModel
                .GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(Portfolio, positionGroup,
                    deltaBuyingPower + deltaBuyingPowerAdjustment, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Debug($"Expected quantity: {expectedQuantity}  --  Actual: {positionQuantityForDeltaWithPositionGroupBuyingPowerModel}");

            if (positionQuantityForDeltaWithPositionGroupBuyingPowerModel != expectedQuantity)
            {
                throw new Exception($@"Expected position quantity for delta buying power to be {expectedQuantity} but was {
                    positionQuantityForDeltaWithPositionGroupBuyingPowerModel}");
            }

            var position = positionGroup.Positions.Single();
            var sign = (increasing ? +1 : -1) * Math.Sign(position.Quantity);
            var signedDeltaBuyingPower = sign * Math.Abs(deltaBuyingPower);
            var positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel = new SecurityPositionGroupBuyingPowerModel()
                .GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(Portfolio, positionGroup,
                    signedDeltaBuyingPower + deltaBuyingPowerAdjustment, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            var positionQuantityForDeltaWithSecurityBuyingPowerModel = security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(
                new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(Portfolio, security, signedDeltaBuyingPower + deltaBuyingPowerAdjustment,
                    minimumOrderMarginPortfolioPercentage: 0)).Quantity;

            var expectedSingleSecurityModelsQuantity = sign * Math.Abs(expectedQuantity);

            if (positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel != expectedSingleSecurityModelsQuantity ||
                positionQuantityForDeltaWithSecurityBuyingPowerModel != expectedSingleSecurityModelsQuantity)
            {
                throw new Exception($@"Expected order quantity for delta buying power calls from default buying power models to return {
                    expectedSingleSecurityModelsQuantity}. Results were:" +
                    $"    \nSecurityPositionGroupBuyingPowerModel: {positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel}" +
                    $"    \nBuyingPowerModel: {positionQuantityForDeltaWithSecurityBuyingPowerModel}\n");
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
        public long DataPoints => 2940643;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.11%"},
            {"Compounding Annual Return", "-2.907%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "999380.5"},
            {"Net Profit", "-0.062%"},
            {"Sharpe Ratio", "-8.624"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.982%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.032"},
            {"Beta", "0.007"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.051"},
            {"Tracking Error", "0.084"},
            {"Treynor Ratio", "-4.737"},
            {"Total Fees", "$19.50"},
            {"Estimated Strategy Capacity", "$49000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMML01JA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.45%"},
            {"OrderListHash", "5d2df7cb88dbc63da13518c0195eea60"}
        };
    }
}
