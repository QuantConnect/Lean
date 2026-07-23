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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that, while the market is open, an hour resolution market order placed mid-bar
    /// (between bars, via an intraday scheduled event) fills immediately at the latest available bar's close - not
    /// waiting and not at the bar open. With the default one hour stale window the latest bar (the previous close) is
    /// recent enough to fill against right away, so the resting-order open-fill behavior does not apply.
    /// </summary>
    public class HourMarketOrderFillsAtBarCloseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contract;
        private bool _ordered;
        private bool _asserted;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 10, 10);
            SetCash(1000000);

            // Default one hour StalePriceTimeSpan (do not tighten it): the latest hour bar stays within the window.
            var future = AddFuture("ES", Resolution.Hour);
            future.SetFilter(0, 182);

            // Submit mid-bar, half past the hour, while the regular session is open.
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromMinutes(30)), PlaceMidBarOrder);
        }

        public override void OnData(Slice slice)
        {
            if (_contract == null)
            {
                foreach (var chain in slice.FutureChains)
                {
                    var contract = chain.Value.OrderBy(x => x.Expiry).FirstOrDefault(x => x.Expiry > Time.Date.AddDays(90));
                    if (contract != null)
                    {
                        _contract = contract.Symbol;
                    }
                }
            }
        }

        private void PlaceMidBarOrder()
        {
            if (_contract == null || _ordered || Time.Minute != 30)
            {
                return;
            }

            var security = Securities[_contract];

            // Require the market open, fresh-enough data, and a latest bar with a meaningful open/close range.
            if (!security.Exchange.ExchangeOpen || !security.HasData || Math.Abs(security.Open - security.Close) < 1m)
            {
                return;
            }

            var open = security.Open;
            var close = security.Close;

            var ticket = MarketOrder(_contract, 1);
            _ordered = true;

            // Placed mid-bar while the market is open: with the default stale window the latest bar is recent, so the
            // order must fill immediately rather than wait for the next bar.
            if (ticket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Expected an hour market order placed mid-bar while the market is open to fill immediately, but status was {ticket.Status} at {Time}");
            }

            var fill = ticket.AverageFillPrice;

            // It must fill at the latest available bar's close, not its open.
            if (Math.Abs(fill - close) >= Math.Abs(fill - open))
            {
                throw new RegressionTestException(
                    $"Expected the mid-bar order to fill at the latest bar close {close} (not the open {open}) but filled at {fill} at {Time}");
            }

            _asserted = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_asserted)
            {
                throw new RegressionTestException("An hour market order was never placed/asserted while the market was open");
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
        public long DataPoints => 343;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "12.701%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1000983.2"},
            {"Net Profit", "0.098%"},
            {"Sharpe Ratio", "13.749"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.945"},
            {"Beta", "0.073"},
            {"Annual Standard Deviation", "0.018"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-70.946"},
            {"Tracking Error", "0.225"},
            {"Treynor Ratio", "3.337"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$77000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "2.76%"},
            {"Drawdown Recovery", "1"},
            {"OrderListHash", "cae25534b6806e7c98e3d33636f91fe5"}
        };
    }
}
