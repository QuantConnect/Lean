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

using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for the SpreadExecutionModel.
    /// This algorithm shows how the execution model works to 
    /// submit orders only when the price is on desirably tight spread.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class SpreadExecutionModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);

            SetUniverseSelection(new ManualUniverseSelectionModel(
                QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));

            // using hourly rsi to generate more insights
            SetAlpha(new RsiAlphaModel(14, Resolution.Hour));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new SpreadExecutionModel());

            InsightsGenerated += OnInsightsGenerated;
        }

        private void OnInsightsGenerated(IAlgorithm algorithm, GeneratedInsightsCollection eventdata)
        {
            Log($"{Time}: {string.Join(", ", eventdata)}");
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"Purchased Stock: {orderEvent.Symbol}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 15643;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 56;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "19"},
            {"Average Win", "0.62%"},
            {"Average Loss", "-0.19%"},
            {"Compounding Annual Return", "398.867%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "1.395"},
            {"Start Equity", "100000"},
            {"End Equity", "102076.08"},
            {"Net Profit", "2.076%"},
            {"Sharpe Ratio", "10.465"},
            {"Sortino Ratio", "21.499"},
            {"Probabilistic Sharpe Ratio", "70.084%"},
            {"Loss Rate", "44%"},
            {"Win Rate", "56%"},
            {"Profit-Loss Ratio", "3.31"},
            {"Alpha", "0.533"},
            {"Beta", "1.115"},
            {"Annual Standard Deviation", "0.261"},
            {"Annual Variance", "0.068"},
            {"Information Ratio", "8.837"},
            {"Tracking Error", "0.086"},
            {"Treynor Ratio", "2.453"},
            {"Total Fees", "$40.66"},
            {"Estimated Strategy Capacity", "$1900000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "146.73%"},
            {"OrderListHash", "36ab6f7236250f7a064b77af9b4870c4"}
        };
    }
}
