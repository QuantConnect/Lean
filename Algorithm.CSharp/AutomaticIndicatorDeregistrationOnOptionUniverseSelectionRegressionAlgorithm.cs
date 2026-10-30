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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that when the mirror contract of an option indicator leaves the option universe,
    /// only the mirror's consolidator is removed: the indicators of the contract that stays keep their consolidators
    /// and keep updating. See <see cref="IAlgorithmSettings.AutomaticIndicatorDeregistration"/>
    /// </summary>
    public class AutomaticIndicatorDeregistrationOnOptionUniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly DateTime _putRemovalDate = new DateTime(2015, 12, 25);

        private Symbol _call;
        private Symbol _put;
        private Delta _mirroredDelta;
        private Delta _delta;
        private bool _putRemoved;
        private long _deltaSamplesAtPutRemoval;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            AddEquity("GOOG");
            var option = AddOption("GOOG");
            option.SetFilter(universe =>
            {
                universe = universe.Strikes(new[] { 750m }).Expiration(new[] { new DateTime(2016, 6, 17) });
                // the put leaves the universe on the removal date, the call stays and keeps getting data on the 28th
                return universe.LocalTime.Date >= _putRemovalDate ? universe.CallsOnly() : universe;
            });
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.AddedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Option))
            {
                if (security.Symbol.ID.OptionRight == OptionRight.Call)
                {
                    _call = security.Symbol;
                }
                else
                {
                    _put = security.Symbol;
                }
            }

            if (_delta == null && _call != null && _put != null)
            {
                _mirroredDelta = D(_call, _put);
                _delta = D(_call);
            }

            foreach (var security in changes.RemovedSecurities)
            {
                if (security.Symbol == _call)
                {
                    throw new RegressionTestException("The call should stay in the universe");
                }
                if (security.Symbol == _put)
                {
                    OnPutRemoved();
                }
            }
        }

        private void OnPutRemoved()
        {
            _putRemoved = true;
            _deltaSamplesAtPutRemoval = _delta.Samples;

            if (!_mirroredDelta.IsReady)
            {
                throw new RegressionTestException("The mirrored delta should have been updated while the put was in the universe");
            }
            if (ConsolidatorCount(_put) != 0)
            {
                throw new RegressionTestException("The put consolidators should have been removed with the put");
            }
            if (ConsolidatorCount(_call) != 2 || ConsolidatorCount(_call.Underlying) != 2)
            {
                throw new RegressionTestException("The call and underlying consolidators of both deltas should be kept");
            }
            if (_mirroredDelta.Consolidators.Count != 2 || _delta.Consolidators.Count != 2)
            {
                throw new RegressionTestException("The deltas should only lose the put consolidator");
            }
        }

        private int ConsolidatorCount(Symbol symbol)
        {
            return SubscriptionManager.Subscriptions.Where(x => x.Symbol == symbol).Sum(x => x.Consolidators.Count);
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_putRemoved)
            {
                throw new RegressionTestException("The put should have been removed from the universe");
            }
            if (_delta.Samples <= _deltaSamplesAtPutRemoval || _delta.Current.EndTime.Date <= _putRemovalDate)
            {
                throw new RegressionTestException("The delta of the call should keep updating after the put is removed");
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
        public long DataPoints => 9216;

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
            {"Information Ratio", "7.438"},
            {"Tracking Error", "0.017"},
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
