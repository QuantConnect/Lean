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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which reproduces GH issue #5079, where option chain universes would sometimes not get removed from the
    /// UniverseManager causing new universes not to get added
    /// </summary>
    public class OptionChainUniverseRemovalRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // initialize our changes to nothing
        private SecurityChanges _changes = SecurityChanges.None;
        private int _optionCount;
        private Symbol _lastEquityAdded;
        private Symbol _aapl;
        private int _onSecuritiesChangedCallCount;

        public override void Initialize()
        {
            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2014, 06, 06);
            SetEndDate(2014, 06, 10);

            var toggle = true;
            var selectionUniverse = AddUniverse(enumerable =>
            {
                if (toggle)
                {
                    toggle = false;
                    return new []{ _aapl };
                }
                toggle = true;
                return Enumerable.Empty<Symbol>();
            });

            AddUniverseOptions(selectionUniverse, universe =>
            {
                if (universe.Underlying == null)
                {
                    throw new RegressionTestException("Underlying data point is null! This shouldn't happen, each OptionChainUniverse handles and should provide this");
                }
                return universe.IncludeWeeklys()
                    .BackMonth() // back month so that they don't get removed because of being delisted
                    .Contracts(contracts => contracts.Take(5));
            });
        }

        public override void OnData(Slice slice)
        {
            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None ||
                _changes.AddedSecurities.Any(security => security.Price == 0))
            {
                return;
            }

            Debug(GetStatusLog());

            foreach (var security in _changes.AddedSecurities)
            {
                if (!security.Symbol.HasUnderlying)
                {
                    _lastEquityAdded = security.Symbol;
                }
                else
                {
                    // options added should all match prev added security
                    if (security.Symbol.Underlying != _lastEquityAdded)
                    {
                        throw new RegressionTestException($"Unexpected symbol added {security.Symbol}");
                    }

                    _optionCount++;
                }
            }
            _changes = SecurityChanges.None;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Debug($"{GetStatusLog()}. CHANGES {changes}");
            _onSecuritiesChangedCallCount++;
            if (Time.Day == 6)
            {
                if (Time.Hour != 0)
                {
                    throw new RegressionTestException($"Unexpected SecurityChanges time: {Time} {changes}");
                }

                if (changes.RemovedSecurities.Count != 0)
                {
                    throw new RegressionTestException($"Unexpected removals: {changes}");
                }

                if (_onSecuritiesChangedCallCount == 1)
                {
                    // first we expect the equity to get Added
                    if (changes.AddedSecurities.Count != 1 || changes.AddedSecurities[0].Symbol != _aapl)
                    {
                        throw new RegressionTestException($"Unexpected SecurityChanges: {changes}");
                    }
                }
                else
                {
                    // later we expect the options to be Added
                    if (changes.AddedSecurities.Count != 5 || changes.AddedSecurities.Any(security => security.Symbol.SecurityType != SecurityType.Option))
                    {
                        throw new RegressionTestException($"Unexpected SecurityChanges: {changes}");
                    }
                }
            }
            // We expect the equity to get Removed
            else if (Time.Day == 7)
            {
                if (Time.Hour != 0)
                {
                    throw new RegressionTestException($"Unexpected SecurityChanges time: {Time} {changes}");
                }

                // Options can be selected/deselected on this day, but the equity should be removed

                if (changes.RemovedSecurities.Count == 0 || !changes.RemovedSecurities.Any(x => x.Symbol == _aapl))
                {
                    throw new RegressionTestException($"Unexpected SecurityChanges: {changes}");
                }
            }
            // We expect the options to get Removed, happens in the next loop after removing the equity
            else if (Time.Day == 9)
            {
                if (Time.Hour != 0)
                {
                    throw new RegressionTestException($"Unexpected SecurityChanges time: {Time} {changes}");
                }

                // later we expect the options to be Removed
                if (changes.RemovedSecurities.Count != 6
                    // the removal of the raw underlying subscription from the option chain universe
                    || changes.RemovedSecurities.Single(security => security.Symbol.SecurityType != SecurityType.Option).Symbol != _aapl
                    // the removal of the 5 option contracts
                    || changes.RemovedSecurities.Count(security => security.Symbol.SecurityType == SecurityType.Option) != 5)
                {
                    throw new RegressionTestException($"Unexpected SecurityChanges: {changes}");
                }
            }

            _changes += changes;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_optionCount == 0)
            {
                throw new RegressionTestException("Option universe chain did not add any option!");
            }
            if (UniverseManager.Any(pair => pair.Value.DisposeRequested))
            {
                throw new RegressionTestException("There shouldn't be any disposed universe, they should be removed and replaced by new universes");
            }
        }

        private string GetStatusLog()
        {
            Plot("Status", "UniverseCount", UniverseManager.Count);
            Plot("Status", "SubscriptionCount", SubscriptionManager.Subscriptions.Count());
            Plot("Status", "ActiveSymbolsCount", UniverseManager.ActiveSecurities.Count);

            return $"{Time} | UniverseCount {UniverseManager.Count}. " +
                $"SubscriptionCount {SubscriptionManager.Subscriptions.Count()}. " +
                $"ActiveSymbols {string.Join(",", UniverseManager.ActiveSecurities.Keys)}";
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 17966;

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
            {"Information Ratio", "-9.522"},
            {"Tracking Error", "0.006"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
