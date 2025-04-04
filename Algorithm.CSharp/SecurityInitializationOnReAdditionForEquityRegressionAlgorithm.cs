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
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class SecurityInitializationOnReAdditionForEquityRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        private Security _equity;
        private Queue<DateTime> _tradableDates;
        private bool _securityWasRemoved;
        private int _securityInitializationCount;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 04);
            SetEndDate(2013, 10, 30);

            var seeder = new FuncSecuritySeeder((security) =>
            {
                if ((_equity != null && ReferenceEquals(security, _equity)) ||
                    (_equity == null && security.Symbol == _symbol))
                {
                    _securityInitializationCount++;
                }

                Debug($"[{Time}] Seeding {security.Symbol}");
                return GetLastKnownPrices(security);
            });
            SetSecurityInitializer(security => seeder.SeedSecurity(security));

            _equity = AddEquity();

            _tradableDates = new(QuantConnect.Time.EachTradeableDay(_equity.Exchange.Hours, StartDate, EndDate));

            Schedule.On(DateRules.EveryDay(_equity.Symbol), TimeRules.Midnight, () =>
            {
                var currentTradableDate = _tradableDates.Dequeue();
                if (currentTradableDate != Time.Date)
                {
                    throw new RegressionTestException($"Expected the current tradable date to be {Time.Date}. Got {currentTradableDate}");
                }

                if (Time == StartDate)
                {
                    return;
                }

                // Before we remove the security let's check that it was not initialized again
                if (_securityInitializationCount != 1)
                {
                    throw new RegressionTestException($"Expected the equity to be initialized once and once only, " +
                        $"but was initialized {_securityInitializationCount} times");
                }

                // Remove the security every day
                Debug($"[{Time}] Removing the equity");
                _securityWasRemoved = RemoveSecurity(_equity.Symbol);

                if (!_securityWasRemoved)
                {
                    throw new RegressionTestException($"Expected the equity to be removed");
                }
            });
        }

        private Equity AddEquity()
        {
            _securityInitializationCount = 0;
            var equity = AddEquity("SPY");

            if (_securityInitializationCount != 1)
            {
                throw new RegressionTestException($"Expected the equity to be initialized once and once only, " +
                    $"but was initialized {_securityInitializationCount} times");
            }

            return equity;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_securityWasRemoved)
            {
                if (changes.AddedSecurities.Count > 0)
                {
                    throw new RegressionTestException($"Expected no securities to be added. Got {changes.AddedSecurities.Count}");
                }

                if (!changes.RemovedSecurities.Contains(_equity))
                {
                    throw new RegressionTestException($"Expected the equity to be removed. Got {changes.RemovedSecurities.Count}");
                }

                _securityWasRemoved = false;

                // Add the security back
                Debug($"[{Time}] Re-adding the equity");
                var reAddedEquity = AddEquity();

                if (!ReferenceEquals(reAddedEquity, _equity))
                {
                    throw new RegressionTestException($"Expected the re-added equity to be the same as the original equity");
                }

                if (!reAddedEquity.IsTradable)
                {
                    throw new RegressionTestException($"Expected the re-added equity to be tradable");
                }
            }
            else if (!changes.AddedSecurities.Contains(_equity))
            {
                throw new RegressionTestException($"Expected the equity to be added back");
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
        public long DataPoints => 4823;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 3838;

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
            {"Information Ratio", "-4.884"},
            {"Tracking Error", "0.108"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
