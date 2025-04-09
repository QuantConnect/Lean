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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing the behavior of the algorithm when a security is removed and re-added.
    /// It asserts that the securities are marked as non-tradable when removed and that they are tradable when re-added.
    /// It also asserts that the algorithm receives the correct security changed events for the added and removed securities.
    ///
    /// Additionally, it tests that the security is initialized after every addition, and no more.
    ///
    /// This specific algorithm tests this behavior for option contracts that are selected, deselected and re-selected.
    /// </summary>
    public class SecurityInitializationOnReAdditionForSelectedOptionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _canonicalOption;
        private List<Symbol> _contractsToSelect;
        private HashSet<Option> _selectedContracts = new();
        private bool _selectSingle;
        private int _selectionsCount;

        private Dictionary<Security, int> _securityInializationCounts = new();

        public override void Initialize()
        {
            SetStartDate(2014, 06, 04);
            SetEndDate(2014, 06, 20);
            SetCash(100000);

            var seeder = new FuncSecuritySeeder((security) =>
            {
                if (security is Option option)
                {
                    if (!_securityInializationCounts.TryGetValue(security, out var count))
                    {
                        count = 0;
                    }
                    _securityInializationCounts[security] = count + 1;
                }

                Debug($"[{Time}] Seeding {security.Symbol}");
                return GetLastKnownPrices(security);
            });

            SetSecurityInitializer(security => seeder.SeedSecurity(security));

            var equitySymbol = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            _contractsToSelect = new List<Symbol>()
            {
                QuantConnect.Symbol.CreateOption(equitySymbol, Market.USA, OptionStyle.American, OptionRight.Call, 335.7m, new DateTime(2014, 07, 19)),
                QuantConnect.Symbol.CreateOption(equitySymbol, Market.USA, OptionStyle.American, OptionRight.Call, 335.7m, new DateTime(2015, 01, 17))
            };

            var option = AddOption(equitySymbol, Resolution.Daily);
            option.SetFilter(u => u.Contracts(contracts =>
            {
                _selectionsCount++;
                _securityInializationCounts.Clear();

                List<Symbol> selected;
                if (_selectSingle)
                {
                    _selectSingle = false;
                    selected = _contractsToSelect.Take(1).ToList();
                }
                else
                {
                    _selectSingle = true;
                    selected = _contractsToSelect;
                }

                Log($"[{Time}] [{UtcTime}] Selecting {string.Join(", ", selected.Select(x => x.Value))}");
                return selected;
            }));

            _canonicalOption = option.Symbol;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.AddedSecurities)
            {
                if (!security.IsTradable)
                {
                    throw new RegressionTestException($"Expected the security to be tradable. Symbol: {security.Symbol}");
                }
            }

            foreach (var security in changes.RemovedSecurities)
            {
                if (security.IsTradable)
                {
                    throw new RegressionTestException($"Expected the security to be not tradable. Symbol: {security.Symbol}");
                }
            }

            var underlyingEquity = changes.AddedSecurities.FirstOrDefault(x => x.Symbol == _canonicalOption.Underlying);
            if (Time == StartDate)
            {
                if (underlyingEquity == null)
                {
                    throw new RegressionTestException($"Expected the underlying equity to be added. " +
                        $"Added: {string.Join(", ", changes.AddedSecurities.Select(x => x.Symbol.Value))}");
                }
            }
            else if (underlyingEquity != null)
            {
                throw new RegressionTestException($"Expected the underlying equity to not be added. " +
                    $"Added: {string.Join(", ", changes.AddedSecurities.Select(x => x.Symbol.Value))}");
            }

            var addedContracts = changes.AddedSecurities.OfType<Option>().ToList();
            if (addedContracts.Any(x => !_securityInializationCounts.TryGetValue(x, out var count) || count != 1))
            {
                throw new RegressionTestException($"Expected all contracts to be initialized. Added: {string.Join(", ", addedContracts.Select(x => x.Symbol.Value))}, Initialized: {string.Join(", ", _securityInializationCounts.Select(x => $"{x.Key.Symbol.Value} - {x.Value}"))}");
            }

            // The first contract will be selected always, so we expect it to be added only once
            var firstAddedContract = changes.AddedSecurities.FirstOrDefault(x => x.Symbol == _contractsToSelect[0]) as Option;
            if (firstAddedContract == null)
            {
                if (_selectedContracts.Contains(firstAddedContract))
                {
                    throw new RegressionTestException($"Expected the first contract to be added only once");
                }
            }

            // _selectSingle flag was set to true, so we expect both contracts to be selected
            if (_selectSingle)
            {
                if (!changes.AddedSecurities.Any(x => x.Symbol == _contractsToSelect[1]))
                {
                    throw new RegressionTestException($"Expected the second contract to be added");
                }
            }
            else
            {
                if (changes.AddedSecurities.Any(x => x.Symbol == _contractsToSelect[1]))
                {
                    throw new RegressionTestException($"Expected the second contract to not be added");
                }

                var removedContract = changes.RemovedSecurities.FirstOrDefault(x => x.Symbol == _contractsToSelect[1]);

                if (removedContract == null)
                {
                    throw new RegressionTestException($"Expected the second contract to be removed");
                }

                if (removedContract.IsTradable)
                {
                    throw new RegressionTestException($"Expected the second contract to be not tradable since it was removed");
                }
            }

            foreach (var security in changes.AddedSecurities.OfType<Option>())
            {
                _selectedContracts.Add(security);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionsCount == 0)
            {
                throw new RegressionTestException("Expected at least one selection");
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
        public long DataPoints => 39254;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 5;

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
            {"Information Ratio", "-6.27"},
            {"Tracking Error", "0.056"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
