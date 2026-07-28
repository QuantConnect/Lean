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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that when an option universe is removed and one for the same
    /// underlying is re-added in the same time step, the previously selected contracts that are not
    /// re-selected by the new universe are properly cleaned up: their subscriptions are removed and
    /// removed security changes are emitted for them.
    /// </summary>
    public class OptionUniverseRemovedAndReAddedMemberCleanupRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _canonical;
        private bool _readded;
        private bool _checked;
        private List<Symbol> _oldMembers;
        private HashSet<Symbol> _removedSymbols;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 9);
            SetCash(100000);

            _removedSymbols = new HashSet<Symbol>();

            // Wide filter: several strikes and expirations will be selected
            var option = AddOption("AAPL", Resolution.Minute);
            option.SetFilter(-2, 2, 0, 180);
            _canonical = option.Symbol;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {
                _removedSymbols.Add(security.Symbol);
            }
        }

        public override void OnData(Slice slice)
        {
            if (!_readded)
            {
                if (Time.Hour < 10)
                {
                    return;
                }

                var universe = UniverseManager[_canonical];
                if (universe.Members.Count == 0)
                {
                    return;
                }

                _oldMembers = universe.Members.Keys.Where(symbol => !symbol.IsCanonical()).ToList();

                // Remove the universe and re-add it with a narrower filter in the same time step:
                // most of the previously selected contracts will not be re-selected by the new universe
                RemoveSecurity(_canonical);
                var option = AddOption("AAPL", Resolution.Minute);
                option.SetFilter(universeFilter => universeFilter.Strikes(0, 0).Expiration(0, 30));
                _canonical = option.Symbol;
                _readded = true;
            }
            else if (!_checked && (Time.Hour > 10 || Time.Minute >= 30))
            {
                _checked = true;
                AssertOldMembersCleanedUp();
            }
        }

        private void AssertOldMembersCleanedUp()
        {
            var currentMembers = UniverseManager[_canonical].Members.Keys.ToHashSet();
            var subscribed = SubscriptionManager.Subscriptions.Select(config => config.Symbol).ToHashSet();

            var notReselected = _oldMembers.Where(symbol => !currentMembers.Contains(symbol)).ToList();
            if (notReselected.Count == 0)
            {
                throw new RegressionTestException("Expected some previously selected contracts to not be re-selected");
            }

            var stillSubscribed = notReselected.Where(subscribed.Contains).ToList();
            if (stillSubscribed.Count > 0)
            {
                throw new RegressionTestException(
                    $"Expected the subscriptions of the {notReselected.Count} deselected contracts to be removed, " +
                    $"but {stillSubscribed.Count} are still subscribed, e.g. {string.Join(", ", stillSubscribed.Take(3))}");
            }

            var missingRemovedEvents = notReselected.Where(symbol => !_removedSymbols.Contains(symbol)).ToList();
            if (missingRemovedEvents.Count > 0)
            {
                throw new RegressionTestException(
                    $"Expected removed security changes for the {notReselected.Count} deselected contracts, " +
                    $"but {missingRemovedEvents.Count} were not notified, e.g. {string.Join(", ", missingRemovedEvents.Take(3))}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_readded)
            {
                throw new RegressionTestException("The option universe was never removed and re-added");
            }
            if (!_checked)
            {
                throw new RegressionTestException("The clean up assertions were never performed");
            }
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
        public long DataPoints => 34380;

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
            {"Information Ratio", "-9.486"},
            {"Tracking Error", "0.008"},
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
