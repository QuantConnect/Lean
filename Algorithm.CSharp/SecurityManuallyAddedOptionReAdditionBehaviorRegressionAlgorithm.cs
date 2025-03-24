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
    public class SecurityManuallyAddedOptionReAdditionBehaviorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly Symbol _optionContractSymbol = QuantConnect.Symbol.CreateOption(
            QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            Market.USA,
            OptionStyle.American,
            OptionRight.Call,
            342.9m,
            new DateTime(2014, 07, 19));

        private Option _manuallyAddedContract;

        private bool _securityWasRemoved;

        private bool _securityWasInitialized;

        private Queue<DateTime> _tradableDates;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 04);
            SetEndDate(2014, 06, 20);
            SetCash(100000);

            var seeder = new FuncSecuritySeeder((security) =>
            {
                _securityWasInitialized = true;
                Debug($"[{Time}] Seeding {security.Symbol}");
                return GetLastKnownPrices(security);
            });

            SetSecurityInitializer(security => seeder.SeedSecurity(security));

            _manuallyAddedContract = AddOptionContract();

            _tradableDates = new(QuantConnect.Time.EachTradeableDay(_manuallyAddedContract.Exchange.Hours, StartDate, EndDate));
        }

        public Option AddOptionContract()
        {
            _securityWasInitialized = false;
            var option = AddOptionContract(_optionContractSymbol, Resolution.Daily);

            if (!_securityWasInitialized)
            {
                throw new RegressionTestException($"Expected the option contract to be initialized. Symbol: {option.Symbol}");
            }

            return option;
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            if (symbol != _manuallyAddedContract.Symbol)
            {
                return;
            }

            var currentTradableDate = _tradableDates.Dequeue();
            if (currentTradableDate != Time.Date)
            {
                throw new RegressionTestException($"Expected the current tradable date to be {Time.Date}. Got {currentTradableDate}");
            }

            // Remove the security every day
            Debug($"[{Time}] Removing the option contract");
            _securityWasRemoved = RemoveSecurity(symbol);

            if (!_securityWasRemoved)
            {
                throw new RegressionTestException($"Expected the option contract to be removed");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_securityWasRemoved)
            {
                if (changes.AddedSecurities.Count > 0)
                {
                    throw new RegressionTestException($"Expected no securities to be added. Got {changes.AddedSecurities.Count}");
                }

                if (!changes.RemovedSecurities.Contains(_manuallyAddedContract))
                {
                    throw new RegressionTestException($"Expected the option contract to be removed. Got {changes.RemovedSecurities.Count}");
                }

                _securityWasRemoved = false;

                if (Time.Date >= EndDate.Date)
                {
                    return;
                }

                // Add the security back
                Debug($"[{Time}] Re-adding the option contract");
                var reAddedContract = AddOptionContract();

                if (!ReferenceEquals(reAddedContract, _manuallyAddedContract))
                {
                    throw new RegressionTestException($"Expected the re-added option contract to be the same as the original option contract");
                }

                if (!reAddedContract.IsTradable)
                {
                    throw new RegressionTestException($"Expected the re-added option contract to be tradable");
                }
            }
            else if (!changes.AddedSecurities.Contains(_manuallyAddedContract))
            {
                throw new RegressionTestException($"Expected the option contract to be added back");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_tradableDates.Count > 0)
            {
                throw new RegressionTestException($"Expected no more tradable dates. Still have {_tradableDates.Count}");
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
        public long DataPoints => 115;

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
