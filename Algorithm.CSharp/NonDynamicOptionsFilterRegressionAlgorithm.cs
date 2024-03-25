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
using System;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that universe selection is not dynamic by default, that is, selection happens only on market open by default.
    /// </summary>
    public class NonDynamicOptionsFilterRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "AAPL";

        private Symbol _optionSymbol;

        private int _securitiesChangedCount;

        private int _previouslyAddedOptionsCount;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 06);
            SetEndDate(2014, 06, 09);
            SetCash(100000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180 * 3));

            SetBenchmark(equity.Symbol);
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (++_securitiesChangedCount == 1)
            {
                // This is the underlying security addition
                if (changes.AddedSecurities.Count != 1 || changes.RemovedSecurities.Count != 0)
                {
                    throw new Exception("Unexpected security changes count: " +
                        "on the first OnSecuritiesChanged callback, we expect only the underlying to be added.");
                }

                if (changes.AddedSecurities[0].Symbol != _optionSymbol.Underlying)
                {
                    throw new Exception("Unexpected security added: " +
                        "on the first OnSecuritiesChanged callback, we expect only the underlying to be added.");
                }
            }
            else if (_securitiesChangedCount < 4)
            {
                // This is the universe selection, which we expect to happen twice, on market open of each day

                // Check the time
                var option = Securities[_optionSymbol];
                var exchangeHours = option.Exchange.Hours;
                var marketOpen = exchangeHours.GetNextMarketOpen(Time.Date, false);
                if (Time.AddMinutes(-1) != marketOpen)
                {
                    throw new Exception($"Unexpected security changes time. Current time {Time}. Expected time: {marketOpen.AddMinutes(1)}");
                }

                // Check the changes
                if (changes.AddedSecurities.Count == 0)
                {
                    throw new Exception("Unexpected security changes count: " +
                        "on second and third OnSecuritiesChanged callbacks we expect options to be added");
                }

                if (changes.AddedSecurities.Any(security => !security.Symbol.HasCanonical() || security.Symbol.Canonical != _optionSymbol))
                {
                    throw new Exception("Unexpected security added: " +
                        $"on second and third OnSecuritiesChanged callbacks we expect only {UnderlyingTicker} options to be added");
                }

                if (_securitiesChangedCount == 3)
                {
                    // The options added the previous day should be removed
                    if (changes.RemovedSecurities.Count != _previouslyAddedOptionsCount)
                    {
                        throw new Exception("Unexpected security changes count: " +
                            "on the third OnSecuritiesChanged callback we expect the previous day selection to be removed.");
                    }
                }

                _previouslyAddedOptionsCount = changes.AddedSecurities.Count;
            }
            else
            {
                throw new Exception($"Unexpected call to OnSecuritiesChanged: we expect only 3 OnSecuritiesChanged callbacks for this algorithm");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_securitiesChangedCount != 3)
            {
                throw new Exception($"Unexpected number of calls to OnSecuritiesChanged: {_securitiesChangedCount}. " +
                    "We expect only 3 OnSecuritiesChanged callbacks for this algorithm");
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
        public long DataPoints => 1814313;

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
            {"Information Ratio", "-19.236"},
            {"Tracking Error", "0.147"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
