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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using custom buying power model in backtesting.
    /// QuantConnect allows you to model all orders as deeply and accurately as you need.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="transaction fees and slippage" />
    /// <meta name="tag" content="custom buying power models" />
    public class CustomBuyingPowerModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);
            SetEndDate(2013, 10, 31);
            var security = AddEquity("SPY", Resolution.Hour);
            _spy = security.Symbol;

            // set the buying power model
            security.SetBuyingPowerModel(new CustomBuyingPowerModel());
        }

        public void OnData(Slice slice)
        {
            if (Portfolio.Invested)
            {
                return;
            }

            var quantity = CalculateOrderQuantity(_spy, 1m);
            if (quantity % 100 != 0)
            {
                throw new Exception($"CustomBuyingPowerModel only allow quantity that is multiple of 100 and {quantity} was found");
            }

            // We normally get insufficient buying power model, but the
            // CustomBuyingPowerModel always says that there is sufficient buying power for the orders
            MarketOrder(_spy, quantity * 10);
        }

        public class CustomBuyingPowerModel : BuyingPowerModel
        {
            public override GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(
                GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters)
            {
                var quantity = base.GetMaximumOrderQuantityForTargetBuyingPower(parameters).Quantity;
                quantity = Math.Floor(quantity / 100) * 100;
                return new GetMaximumOrderQuantityResult(quantity);
            }

            public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
                HasSufficientBuyingPowerForOrderParameters parameters)
            {
                return new HasSufficientBuyingPowerForOrderResult(true);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "5672.520%"},
            {"Drawdown", "22.500%"},
            {"Expectancy", "0"},
            {"Net Profit", "40.601%"},
            {"Sharpe Ratio", "40.201"},
            {"Probabilistic Sharpe Ratio", "77.339%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "41.848"},
            {"Beta", "9.224"},
            {"Annual Standard Deviation", "1.164"},
            {"Annual Variance", "1.355"},
            {"Information Ratio", "44.459"},
            {"Tracking Error", "1.04"},
            {"Treynor Ratio", "5.073"},
            {"Total Fees", "$30.00"},
            {"Fitness Score", "0.418"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "113.05"},
            {"Return Over Maximum Drawdown", "442.81"},
            {"Portfolio Turnover", "0.418"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "b88362c462e9ab2942cbcb8dfddc6ce0"}
        };
    }
}
