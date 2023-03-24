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

using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends current portfolio targets from its Portfolio to different 3rd party API's
    /// every time the ema indiicators crosses between themselves.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class PortfolioSignalExportDemonstrationAlgorithm : SignalExportDemonstrationAlgorithm , IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Set Holdings to SPY and sends signals to the different 3rd party API's already defined
        /// </summary>
        /// <param name="quantity">Holding quantity to set to SPY</param>
        public override void SetHoldingsToSpyAndSendSignals(decimal quantity)
        {
            SetHoldings("SPY", quantity);
            SignalExport.SetTargetPortfolio(this);
        }

        /// <summary>
        /// Set initial holding quantity for each symbol in Symbols list
        /// </summary>
        public override void SetInitialSignalValueForTargets()
        {
            foreach (var symbol in Symbols)
            {
                SetHoldings(symbol, 0.05);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 11743;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "8"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "31.500%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.351%"},
            {"Sharpe Ratio", "6.444"},
            {"Probabilistic Sharpe Ratio", "68.992%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.107"},
            {"Beta", "0.2"},
            {"Annual Standard Deviation", "0.045"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-9.509"},
            {"Tracking Error", "0.178"},
            {"Treynor Ratio", "1.446"},
            {"Total Fees", "$8.00"},
            {"Estimated Strategy Capacity", "$6700000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Fitness Score", "0.048"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "9.197"},
            {"Return Over Maximum Drawdown", "69.229"},
            {"Portfolio Turnover", "0.049"},
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
            {"OrderListHash", "bd935d199d8b92a3a9eb1fa64f57b930"}
        };
    }
}
