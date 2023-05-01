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
            SetCash(200000);

            var equitySymbol = AddEquity("GOOG").Symbol;

            var option = AddOption(equitySymbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                .Expiration(0, 180));
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

            var quantity = -10;
            MarketOrder(contractSymbol, quantity);

            var security = Securities[contractSymbol];
            var positionGroup = Portfolio.PositionGroups.Single();

            var usedMargin = Portfolio.TotalMarginUsed;
            var absQuantity = Math.Abs(quantity);
            var marginPerNakedShortUnit = usedMargin / absQuantity;

            for (var expectedQuantity = 1; expectedQuantity <= absQuantity; expectedQuantity++)
            {
                var deltaBuyingPower = marginPerNakedShortUnit * expectedQuantity * 0.95m;
                PerfomQuantityCalculations(positionGroup, security, expectedQuantity, deltaBuyingPower);
            }

            // Now test that for buying power deltas greater than the total margin used,
            // the calculated order quantity starts increasing by the margin required for
            // a group unit without underlyings (complete liquidation + going long)
            var longUnitGroup = positionGroup.Key.CreateUnitGroup();
            var marginPerLongUnit = longUnitGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(Portfolio, longUnitGroup)).Value;

            for (var i = 1; i < 10; i++)
            {
                var expectedQuantity = absQuantity + i;
                var deltaBuyingPower = usedMargin + marginPerLongUnit * i;
                PerfomQuantityCalculations(positionGroup, security, expectedQuantity, deltaBuyingPower);
            }
        }

        private void PerfomQuantityCalculations(IPositionGroup positionGroup, Security security, int expectedQuantity,
            decimal deltaBuyingPower)
        {
            var positionQuantityForDeltaWithPositionGroupBuyingPowerModel = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(
                    new GetMaximumLotsForDeltaBuyingPowerParameters(Portfolio, positionGroup, deltaBuyingPower,
                        minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            var positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel = new SecurityPositionGroupBuyingPowerModel()
                .GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(Portfolio, positionGroup, deltaBuyingPower,
                    minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            var positionQuantityForDeltaWithSecurityBuyingPowerModel = security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(
                new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(Portfolio, security, deltaBuyingPower,
                    minimumOrderMarginPortfolioPercentage: 0)).Quantity;

            if (positionQuantityForDeltaWithPositionGroupBuyingPowerModel != positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel ||
                positionQuantityForDeltaWithPositionGroupBuyingPowerModel != positionQuantityForDeltaWithSecurityBuyingPowerModel)
            {
                throw new Exception($"Expected all order quantity for delta buying power calls to return the same. Results were: " +
                    $"PositionGroupBuyingPowerModel: {positionQuantityForDeltaWithPositionGroupBuyingPowerModel}\n" +
                    $"SecurityPositionGroupBuyingPowerModel: {positionQuantityForDeltaWithSecurityPositionGroupBuyingPowerModel}\n" +
                    $"BuyingPowerModel: {positionQuantityForDeltaWithSecurityBuyingPowerModel}\n");
            }

            if (positionQuantityForDeltaWithPositionGroupBuyingPowerModel != expectedQuantity)
            {
                throw new Exception($@"Expected position quantity for delta buying power to be {expectedQuantity} but was {
                    positionQuantityForDeltaWithPositionGroupBuyingPowerModel}");
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
        public long DataPoints => 3179296;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "12.556%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.249%"},
            {"Sharpe Ratio", "6.276"},
            {"Probabilistic Sharpe Ratio", "95.221%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.093"},
            {"Beta", "-0.027"},
            {"Annual Standard Deviation", "0.015"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.261"},
            {"Tracking Error", "0.088"},
            {"Treynor Ratio", "-3.447"},
            {"Total Fees", "$2.50"},
            {"Estimated Strategy Capacity", "$140000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMML01JA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.73%"},
            {"OrderListHash", "4ad64d6b8116ed9025bb02673469ee88"}
        };
    }
}
