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
 *
*/

using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for the StandardDeviationExecutionModel.
    /// This algorithm shows how the execution model works to split up orders and submit them only when
    /// the price is 2 standard deviations from the 60min mean (default model settings).
    /// </summary>
    public class StandardDeviationExecutionModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(1000000);

            SetUniverseSelection(new ManualUniverseSelectionModel(
                QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            ));

            // using hourly rsi to generate more insights
            SetAlpha(new RsiAlphaModel(14, Resolution.Hour));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new StandardDeviationExecutionModel());
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"{Time}: {orderEvent}");
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
            {"Total Trades", "196"},
            {"Average Win", "0.04%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "1276.717%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "44.681"},
            {"Net Profit", "3.410%"},
            {"Sharpe Ratio", "37.426"},
            {"Probabilistic Sharpe Ratio", "99.760%"},
            {"Loss Rate", "1%"},
            {"Win Rate", "99%"},
            {"Profit-Loss Ratio", "45.22"},
            {"Alpha", "5.805"},
            {"Beta", "0.797"},
            {"Annual Standard Deviation", "0.197"},
            {"Annual Variance", "0.039"},
            {"Information Ratio", "55.32"},
            {"Tracking Error", "0.098"},
            {"Treynor Ratio", "9.268"},
            {"Total Fees", "$245.52"},
            {"Estimated Strategy Capacity", "$400000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Fitness Score", "0.616"},
            {"Kelly Criterion Estimate", "34.534"},
            {"Kelly Criterion Probability Value", "0.444"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "19078.128"},
            {"Portfolio Turnover", "0.616"},
            {"Total Insights Generated", "5"},
            {"Total Insights Closed", "3"},
            {"Total Insights Analysis Completed", "3"},
            {"Long Insight Count", "3"},
            {"Short Insight Count", "2"},
            {"Long/Short Ratio", "150.0%"},
            {"Estimated Monthly Alpha Value", "$801935.3567"},
            {"Total Accumulated Estimated Alpha Value", "$129200.6964"},
            {"Mean Population Estimated Insight Value", "$43066.8988"},
            {"Mean Population Direction", "100%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "100%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "8870b2e5eb44f5fc2b89d6c581c697a1"}
        };
    }
}
