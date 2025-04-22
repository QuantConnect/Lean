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

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested)
            {
                return;
            }

            var quantity = CalculateOrderQuantity(_spy, 1m);
            if (quantity % 100 != 0)
            {
                throw new RegressionTestException($"CustomBuyingPowerModel only allow quantity that is multiple of 100 and {quantity} was found");
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
                // if portfolio doesn't have enough buying power:
                //     parameters.Insufficient()

                // this model never allows a lack of funds get in the way of buying securities
                return parameters.Sufficient();
            }

            // Let's always return 0 as the maintenance margin so we avoid margin call orders
            public override MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
            {
                return new MaintenanceMargin(0);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 330;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "4775.196%"},
            {"Drawdown", "21.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "138618.81"},
            {"Net Profit", "38.619%"},
            {"Sharpe Ratio", "14.322"},
            {"Sortino Ratio", "26.701"},
            {"Probabilistic Sharpe Ratio", "75.756%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "10.447"},
            {"Beta", "8.754"},
            {"Annual Standard Deviation", "0.95"},
            {"Annual Variance", "0.903"},
            {"Information Ratio", "15.703"},
            {"Tracking Error", "0.844"},
            {"Treynor Ratio", "1.554"},
            {"Total Fees", "$30.00"},
            {"Estimated Strategy Capacity", "$150000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "26.62%"},
            {"OrderListHash", "dae7e349316dce7621bc1f8be86ccd0d"}
        };
    }
}
