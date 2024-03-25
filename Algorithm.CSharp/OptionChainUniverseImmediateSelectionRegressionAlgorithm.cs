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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Asserts that Option Chain universe selection happens right away after algorithm starts and a bar of the underlying is received
    /// </summary>
    public class OptionChainUniverseImmediateSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        private bool _firstOnDataCallDone;
        private int _securityChangesCallCount;

        private DateTime _selectionTimeUtc;

        private int _selectedOptionsCount;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(10000);

            var option = AddOption("GOOG", Resolution.Minute);

            _optionSymbol = option.Symbol;

            option.SetFilter(universe =>
            {
                if (_selectionTimeUtc == DateTime.MinValue)
                {
                    _selectionTimeUtc = universe.LocalTime.ConvertToUtc(option.Exchange.TimeZone);

                    if (_firstOnDataCallDone)
                    {
                        throw new Exception("Option chain universe selection time was set after OnData was called");
                    }
                }

                var selection = universe
                    .IncludeWeeklys()
                    .Strikes(-2, +2)
                    .Expiration(TimeSpan.Zero, TimeSpan.FromDays(10));

                _selectedOptionsCount = selection.Count();

                return selection;
            });

            SetBenchmark(x => 0);
        }

        public override void OnData(Slice slice)
        {
            if (!_firstOnDataCallDone)
            {
                _firstOnDataCallDone = true;

                if (!slice.ContainsKey(_optionSymbol.Underlying))
                {
                    throw new Exception($"Expected to find {_optionSymbol.Underlying} in first slice");
                }

                if (!slice.OptionChains.ContainsKey(_optionSymbol))
                {
                    throw new Exception($"Expected to find {_optionSymbol} in first slice's Option Chain");
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Log($"{Time} :: {changes}");
            _securityChangesCallCount++;

            if (_securityChangesCallCount <= 2 && _firstOnDataCallDone)
            {
                throw new Exception("Expected 2 OnSecuritiesChanged calls (Underlying addition + Options additions) " +
                    "before the first data is sent to the algorithm");
            }

            if (_securityChangesCallCount == 1)
            {
                // The first time, only the underlying should have been added
                if (changes.AddedSecurities.Count != 1 || changes.RemovedSecurities.Count != 0)
                {
                    throw new Exception($"Unexpected securities changes on first OnSecuritiesChanged event. " +
                        $"Expected one security added and none removed but got {changes.AddedSecurities.Count} securities added " +
                        $"and {changes.RemovedSecurities.Count} removed.");
                }

                var addedSecuritySymbol = changes.AddedSecurities.Single().Symbol;
                if (addedSecuritySymbol != _optionSymbol.Underlying)
                {
                    throw new Exception($"Expected to find {_optionSymbol.Underlying} in first OnSecuritiesChanged event, " +
                        $"but found {addedSecuritySymbol}");
                }
            }
            else if (_securityChangesCallCount == 2)
            {
                var expectedSelectionTime = StartDate.Add(Securities[_optionSymbol].Resolution.ToTimeSpan());

                if (_selectionTimeUtc == DateTime.MinValue)
                {
                    throw new Exception("Option chain universe selection time was not set");
                }

                if (changes.AddedSecurities.Count != _selectedOptionsCount || changes.RemovedSecurities.Count != 0)
                {
                    throw new Exception($"Unexpected securities changes on second OnSecuritiesChanged event. " +
                        $"Expected {_selectedOptionsCount} options added and none removed but got {changes.AddedSecurities.Count} " +
                        $"securities added and {changes.RemovedSecurities.Count} removed.");
                }

                if (!changes.AddedSecurities.All(x => x.Type.IsOption() && !x.Symbol.IsCanonical() && x.Symbol.Canonical == _optionSymbol))
                {
                    throw new Exception($"Expected to find a multiple option contracts");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_firstOnDataCallDone)
            {
                throw new Exception("OnData was never called");
            }

            if (_securityChangesCallCount < 2)
            {
                throw new Exception("OnSecuritiesChanged was not called at least twice");
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
        public long DataPoints => 470437;

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
            {"Start Equity", "10000"},
            {"End Equity", "10000"},
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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
