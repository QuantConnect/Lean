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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a market order placed mid-bar on an hour resolution asset is not filled at
    /// the stale, already past previous hour bar. Instead it waits for fresh data and fills at the next hour bar close.
    /// The order is submitted at minute 55 of the hour, so the previous hour bar (55 minutes old) is too stale to fill
    /// against given the 1 minute stale price window this algorithm opts into via
    /// <see cref="Interfaces.IAlgorithmSettings.StalePriceTimeSpan"/>.
    /// </summary>
    public class HourResolutionMarketOrderStalePriceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private OrderTicket _ticket;
        private bool _scheduledEventFired;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);
            SetCash(100000);

            // Opt into a tight 1 minute stale price window so a market order placed mid hour bar waits for the next
            // bar instead of filling on the stale previous bar (the engine default is one hour).
            Settings.StalePriceTimeSpan = TimeSpan.FromMinutes(1);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            // Submit the order at minute 55 of the hour. The market is open and the asset is hour resolution, so the
            // order stays a regular market order, but it must not fill on the previous (stale) hour bar.
            Schedule.On(DateRules.On(2013, 10, 8), TimeRules.At(10, 55), () =>
            {
                _scheduledEventFired = true;

                if (!Securities[_spy].HasData)
                {
                    throw new RegressionTestException($"Expected SPY to have data on {Time}");
                }

                // Submit asynchronously so we do not block the scheduled event while waiting for the next bar to fill
                _ticket = MarketOrder(_spy, 10, asynchronous: true);

                if (_ticket.OrderType != OrderType.Market)
                {
                    throw new RegressionTestException(
                        $"Expected an hour resolution intraday market order to remain a Market order but was {_ticket.OrderType}. Time: {Time}");
                }

                // It must not fill on the stale previous hour bar at submission time
                if (_ticket.Status.IsFill())
                {
                    throw new RegressionTestException($"Order was not expected to fill on the stale previous hour bar at {Time}");
                }
            });
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            // The order must fill on the next hour bar (10:00 -> 11:00), not on the stale previous bar (10:00)
            var fillLocalTime = orderEvent.UtcTime.ConvertFromUtc(Securities[_spy].Exchange.TimeZone);
            var expectedFill = new DateTime(2013, 10, 8, 11, 0, 0);

            if (fillLocalTime != expectedFill)
            {
                throw new RegressionTestException(
                    $"Expected the order to fill at the next hour bar {expectedFill} but filled at {fillLocalTime}");
            }

            // The fill must use a real, freshly closed hour bar - not a fill-forwarded repeat of an older bar - and
            // that bar must be the next hour bar (ending 11:00).
            var hourBar = Securities[_spy].GetLastData();
            if (hourBar == null || hourBar.IsFillForward)
            {
                throw new RegressionTestException(
                    $"Expected the order to fill on a real (non fill-forwarded) hour bar but got {(hourBar == null ? "no data" : "fill-forwarded data")} at {Time}");
            }

            if (hourBar.EndTime != expectedFill)
            {
                throw new RegressionTestException(
                    $"Expected the fill bar to end at the next hour {expectedFill} but it ended at {hourBar.EndTime}");
            }

            // It must fill at that hour bar's close price, not the stale previous bar's price nor the bar open. The
            // order is placed mid-bar (after the bar opened), so the close - not the open - is used.
            var hourBarClose = Securities[_spy].Close;
            if (orderEvent.FillPrice != hourBarClose)
            {
                throw new RegressionTestException(
                    $"Expected the order to fill at the next hour bar close price {hourBarClose} but filled at {orderEvent.FillPrice}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_scheduledEventFired)
            {
                throw new RegressionTestException("The intraday scheduled event was never fired");
            }

            if (_ticket == null || _ticket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("The market order was expected to be filled on the next hour bar");
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
        public long DataPoints => 36;

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
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99987.93"},
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
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$7200000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.72%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "4fffd216f07ad38c4decde999f018767"}
        };
    }
}
