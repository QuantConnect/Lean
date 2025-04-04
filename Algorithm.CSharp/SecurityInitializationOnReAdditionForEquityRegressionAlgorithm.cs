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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing the behavior of the algorithm when a security is removed and re-added.
    /// It asserts that the securities are marked as non-tradable when removed and that they are tradable when re-added.
    /// It also asserts that the algorithm receives the correct security changed events for the added and removed securities.
    ///
    /// This specific algorithm tests this behavior for equities.
    /// </summary>
    public class SecurityInitializationOnReAdditionForEquityRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _security;
        private Queue<DateTime> _tradableDates;
        private bool _securityWasRemoved;

        protected virtual DateTime StartTimeToUse => new DateTime(2013, 10, 05);

        protected virtual DateTime EndTimeToUse => new DateTime(2013, 10, 30);

        public override void Initialize()
        {
            SetStartDate(StartTimeToUse);
            SetEndDate(EndTimeToUse);

            _security = AddSecurity();

            _tradableDates = new(QuantConnect.Time.EachTradeableDay(_security.Exchange.Hours, StartDate, EndDate));

            Schedule.On(DateRules.EveryDay(_security.Symbol), TimeRules.Midnight, () =>
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

                // Remove the security every day
                Debug($"[{Time}] Removing the security");
                _securityWasRemoved = RemoveSecurity(_security.Symbol);

                if (!_securityWasRemoved)
                {
                    throw new RegressionTestException($"Expected the security to be removed");
                }

                if (_security.IsTradable)
                {
                    throw new RegressionTestException($"Expected the security to be not tradable after removing it");
                }
            });
        }

        protected virtual Security AddSecurity()
        {
            return AddEquity("SPY");
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_securityWasRemoved)
            {
                if (changes.AddedSecurities.Count > 0)
                {
                    throw new RegressionTestException($"Expected no securities to be added. Got {changes.AddedSecurities.Count}");
                }

                if (!changes.RemovedSecurities.Contains(_security))
                {
                    throw new RegressionTestException($"Expected the security to be removed. Got {changes.RemovedSecurities.Count}");
                }

                _securityWasRemoved = false;

                // Add the security back
                Debug($"[{Time}] Re-adding the security");
                var reAddedSecurity = AddSecurity();

                if (!ReferenceEquals(reAddedSecurity, _security))
                {
                    throw new RegressionTestException($"Expected the re-added security to be the same as the original security");
                }

                if (!reAddedSecurity.IsTradable)
                {
                    throw new RegressionTestException($"Expected the re-added security to be tradable");
                }
            }
            else if (!changes.AddedSecurities.Contains(_security))
            {
                throw new RegressionTestException($"Expected the security to be added back");
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
        public virtual long DataPoints => 4036;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Information Ratio", "-5.028"},
            {"Tracking Error", "0.11"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
