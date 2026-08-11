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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of <see cref="BaseContract.DaysToExpiry()"/> and <see cref="BaseContract.DTE"/>,
    /// the supported alternative to manual expiry math mixing datetime and date values.
    /// The Python version also asserts that the reference argument accepts both datetime and date instances.
    /// </summary>
    public class ContractDaysToExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private int _contractsValidated;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var option = AddOption("GOOG");
            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));
            _optionSymbol = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            OptionChain chain;
            if (!slice.OptionChains.TryGetValue(_optionSymbol, out chain))
            {
                return;
            }

            foreach (var contract in chain)
            {
                var expected = (contract.Expiry.Date - Time.Date).Days;
                if (contract.DaysToExpiry() != expected)
                {
                    throw new RegressionTestException($"Expected DaysToExpiry() to be {expected} but was {contract.DaysToExpiry()}");
                }
                if (contract.DTE != expected)
                {
                    throw new RegressionTestException($"Expected DTE to be {expected} but was {contract.DTE}");
                }
                if (contract.DaysToExpiry(Time.AddDays(-10)) != expected + 10)
                {
                    throw new RegressionTestException($"Expected DaysToExpiry(reference) to be {expected + 10} but was {contract.DaysToExpiry(Time.AddDays(-10))}");
                }
                _contractsValidated++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_contractsValidated == 0)
            {
                throw new RegressionTestException("No contracts were validated");
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
        public long DataPoints => 37131;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
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
