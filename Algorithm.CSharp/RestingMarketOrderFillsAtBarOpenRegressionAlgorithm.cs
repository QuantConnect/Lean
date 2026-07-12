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
    /// Regression algorithm asserting that a hour/daily market order which was resting before the bar it fills on
    /// opened (it predates the bar) fills at that bar's <b>open</b> - the price when trading resumed, like a
    /// <see cref="OrderType.MarketOnOpen"/> - while a market order placed during the bar still fills at the
    /// current/close price.
    ///
    /// It buys a daily future contract on the bar that delivers it (the buy fills at that bar's close), then submits
    /// a liquidation while the market is closed (on the overnight pulse, with no fresh bar). That liquidation rests
    /// and fills on a later bar at the bar open, not its close.
    /// </summary>
    public class RestingMarketOrderFillsAtBarOpenRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _future;
        private Symbol _contract;
        private bool _bought;
        private OrderTicket _liquidate;
        private DateTime _liquidateSubmitUtc;
        private bool _buyAsserted;
        private bool _restingAsserted;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 12, 31);
            SetCash(1000000);

            // Tight stale window so the resting order does not fill on the stale previous bar.
            Settings.StalePriceTimeSpan = TimeSpan.FromMinutes(1);

            _future = AddFuture("ES", Resolution.Daily);
            _future.SetFilter(0, 182);
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

            // Buy on the bar that delivers data: this order is placed during the bar so it fills at the current/close price.
            if (_contract != null && !_bought && slice.Bars.ContainsKey(_contract))
            {
                MarketOrder(_contract, 1);
                _bought = true;
                return;
            }

            // Submit the liquidation while the market is closed (overnight pulse, no fresh bar in the slice). It predates
            // the bar it will eventually fill on, so it must fill at that bar's open, not its close.
            if (_bought && _liquidate == null && Portfolio[_contract].Invested && Time.TimeOfDay == TimeSpan.Zero)
            {
                _liquidate = MarketOrder(_contract, -1, asynchronous: true);
                _liquidateSubmitUtc = UtcTime;

                if (_liquidate.Status.IsFill())
                {
                    throw new RegressionTestException($"The resting liquidation must not fill immediately on the stale bar at {Time}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var open = Securities[_contract].Open;
            var close = Securities[_contract].Close;
            var fill = orderEvent.FillPrice;

            if (open == close)
            {
                throw new RegressionTestException($"Test data is not meaningful: bar open equals close ({open}) at {Time}");
            }

            if (_liquidate != null && orderEvent.OrderId == _liquidate.OrderId)
            {
                // Resting order: it was submitted before this bar opened, so it must fill at the bar open (closer to the
                // open than the close), and only after a later bar arrived.
                if (Math.Abs(fill - open) >= Math.Abs(fill - close))
                {
                    throw new RegressionTestException(
                        $"Expected the resting order to fill at the bar open {open} (not the close {close}) but filled at {fill}");
                }

                if (orderEvent.UtcTime <= _liquidateSubmitUtc)
                {
                    throw new RegressionTestException(
                        $"Expected the resting order to fill on a later bar than its submission {_liquidateSubmitUtc} but filled at {orderEvent.UtcTime}");
                }

                _restingAsserted = true;
            }
            else
            {
                // Buy placed during the bar: it fills at the current/close price, not the open.
                if (Math.Abs(fill - close) >= Math.Abs(fill - open))
                {
                    throw new RegressionTestException(
                        $"Expected the in-bar buy to fill at the bar close {close} (not the open {open}) but filled at {fill}");
                }

                _buyAsserted = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_buyAsserted)
            {
                throw new RegressionTestException("The in-bar buy was never filled/asserted");
            }

            if (!_restingAsserted)
            {
                throw new RegressionTestException("The resting market order was never filled at the bar open/asserted");
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
        public long DataPoints => 1244;

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
            {"Total Orders", "2"},
            {"Average Win", "0.66%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "2.861%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1006570.7"},
            {"Net Profit", "0.657%"},
            {"Sharpe Ratio", "1.931"},
            {"Sortino Ratio", "2.91"},
            {"Probabilistic Sharpe Ratio", "70.540%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.016"},
            {"Beta", "0.068"},
            {"Annual Standard Deviation", "0.006"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-5.117"},
            {"Tracking Error", "0.078"},
            {"Treynor Ratio", "0.182"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$4700000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "0.20%"},
            {"Drawdown Recovery", "12"},
            {"OrderListHash", "6a53aee5b55140888033e93db779c2e9"}
        };
    }
}
