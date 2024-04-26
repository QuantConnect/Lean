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

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test asserting behavior of <see cref="Symbol.Canonical"/>
    /// </summary>
    public class OptionSymbolCanonicalRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionContract;
        private Symbol _canonicalOptionContract;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 09);

            var equitySymbol = AddEquity("TWX").Symbol;
            var contracts = OptionChainProvider.GetOptionContractList(equitySymbol, UtcTime).ToList();

            var callOptionSymbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Call)
                .OrderBy(c => c.ID.Date)
                .First();
            _optionContract = AddOptionContract(callOptionSymbol).Symbol;
            _canonicalOptionContract = _optionContract.Canonical;
        }

        public override void OnData(Slice slice)
        {
            if (!ReferenceEquals(_canonicalOptionContract, _optionContract.Canonical))
            {
                throw new Exception("Canonical Symbol reference changed!");
            }

            _canonicalOptionContract = _optionContract.Canonical;
            if (slice.OptionChains.ContainsKey(_optionContract.Canonical))
            {
                var chain = slice.OptionChains[_optionContract.Canonical];
                if (!Portfolio.Invested)
                {
                    MarketOrder(_optionContract, 1);
                }
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
        public long DataPoints => 4713;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-11.148%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99849"},
            {"Net Profit", "-0.151%"},
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
            {"Information Ratio", "-11.639"},
            {"Tracking Error", "0.037"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$5700000.00"},
            {"Lowest Capacity Asset", "AOL VRKS95ENLBYE|AOL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.59%"},
            {"OrderListHash", "cf5752ad13afe5294a9a8aad660d015a"}
        };
    }
}
