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

using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using PortfolioTarget = QuantConnect.Algorithm.Framework.Portfolio.PortfolioTarget;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends a list of portfolio targets to vBsase API
    /// </summary>
    public class VBaseSignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// vBase API KEY: This value is provided by vBase in your profile section
        /// See API documentation at https://docs.vbase.com/getting-started/rest-api-user-guide
        /// </summary>
        private const string _vbaseApiKey = "YOUR API KEY";
        private const string _vbaseCollectionName = "YOUR COLLECTION";
        private bool _sentSignal;
        private List<Symbol> _symbols = new()
        {
            QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        };


        /// <summary>
        /// Stamping of predefined portfolio targets
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            foreach (var item in _symbols)
            {
                AddEquity(item);
            }

            // Add vBase signal export provider
            SignalExport.AddSignalExportProvider(new VBaseSignalExport(_vbaseApiKey, _vbaseCollectionName));
        }

        public override void OnData(Slice slice)
        {
            if (_sentSignal)
            {
                return;
            }
            _sentSignal = true;

            var targets = new PortfolioTarget[_symbols.Count];
            for (var index = 0; index < _symbols.Count; index++)
            {
                targets[index] = new PortfolioTarget(_symbols[index], (decimal)0.25);
            }
            SignalExport.SetTargetPortfolio(targets);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
