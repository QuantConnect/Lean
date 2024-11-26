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
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Compares two correlation types and asserts they are not equal during the algorithm's execution.
    /// </summary>
    public class CorrelationTypeComparisonRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Correlation _correlationPearson;
        private Correlation _correlationSpearman;

        /// <summary>
        /// Initialise the data and resolution required, as well as the start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);  //Set Start Date
            SetEndDate(2013, 10, 17);    //Set End Date
            var symbol = AddEquity("AAPL", Resolution.Daily).Symbol;
            var spy = AddEquity("SPY", Resolution.Daily).Symbol;
            _correlationPearson = C(symbol, spy, 5, CorrelationType.Pearson);
            _correlationSpearman = C(symbol, spy, 5, CorrelationType.Spearman);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_correlationPearson.IsReady && _correlationSpearman.IsReady)
            {
                var pearsonValue = _correlationPearson.Current.Value;
                var spearmanValue = _correlationSpearman.Current.Value;

                // Check that the correlation values are not the same
                if (pearsonValue == spearmanValue)
                {
                    // Throw an exception if the correlation values are equal
                    throw new RegressionTestException($"Error: Pearson and Spearman correlation values are the same: Pearson = {pearsonValue}, Spearman = {spearmanValue}. This should not happen.");
                }
            }
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation. Intended for closing out logs.
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (!_correlationPearson.IsReady || !_correlationSpearman.IsReady)
            {
                throw new RegressionTestException("Error: Both correlation values should be ready at the end of the algorithm.");
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 80;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-19.184"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
