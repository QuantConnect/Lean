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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for the StandardDeviationExecutionModel.
    /// This algorithm shows how the execution model works to split up orders and submit them only when
    /// the price is 2 standard deviations from the 60min mean (default model settings).
    /// </summary>
    public class StandardDeviationExecutionModelRegressionAlgorithm : QCAlgorithmFramework, IRegressionAlgorithmDefinition
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
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "123"},
            {"Average Win", "0.04%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "2407.069%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0"},
            {"Net Profit", "4.205%"},
            {"Sharpe Ratio", "11.739"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "189.974"},
            {"Annual Standard Deviation", "0.179"},
            {"Annual Variance", "0.032"},
            {"Information Ratio", "11.688"},
            {"Tracking Error", "0.178"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$141.39"},
            {"Total Insights Generated", "5"},
            {"Total Insights Closed", "3"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "3"},
            {"Short Insight Count", "2"},
            {"Long/Short Ratio", "150.0%"},
            {"Estimated Monthly Alpha Value", "$867862.6489"},
            {"Total Accumulated Estimated Alpha Value", "$139822.3157"},
            {"Mean Population Estimated Insight Value", "$46607.4386"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
        };
    }
}