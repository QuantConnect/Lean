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

using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Show cases how to use the <see cref="CompositeAlphaModel"/> to define
    /// </summary>
    public class CompositeAlphaModelFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            // even though we're using a framework algorithm, we can still add our securities
            // using the AddEquity/Forex/Crypto/ect methods and then pass them into a manual
            // universe selection model using Securities.Keys
            AddEquity("SPY");
            AddEquity("IBM");
            AddEquity("BAC");
            AddEquity("AIG");

            // define a manual universe of all the securities we manually registered
            SetUniverseSelection(new ManualUniverseSelectionModel());

            // define alpha model as a composite of the rsi and ema cross models
            SetAlpha(new CompositeAlphaModel(
                new RsiAlphaModel(),
                new EmaCrossAlphaModel()
            ));

            // default models for the rest
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = {Language.CSharp, Language.Python};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 15643;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 208;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "18"},
            {"Average Win", "0.00%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "88.032%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "-0.679"},
            {"Net Profit", "0.811%"},
            {"Sharpe Ratio", "5.833"},
            {"Probabilistic Sharpe Ratio", "65.782%"},
            {"Loss Rate", "73%"},
            {"Win Rate", "27%"},
            {"Profit-Loss Ratio", "0.18"},
            {"Alpha", "-0.473"},
            {"Beta", "0.725"},
            {"Annual Standard Deviation", "0.165"},
            {"Annual Variance", "0.027"},
            {"Information Ratio", "-14.297"},
            {"Tracking Error", "0.071"},
            {"Treynor Ratio", "1.331"},
            {"Total Fees", "$31.70"},
            {"Estimated Strategy Capacity", "$5900000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Return Over Maximum Drawdown", "75.664"},
            {"Portfolio Turnover", "0.505"},
            {"Total Insights Generated", "6"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "5"},
            {"Short Insight Count", "1"},
            {"Long/Short Ratio", "500%"},
            {"OrderListHash", "f25344c69f9b9476ae5a834616a65c82"}
        };
    }
}
