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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing the stale fill at market open issue: a market order placed within the first
    /// bar of the session (here one second after the open, while subscribed to minute resolution) must not be filled
    /// on the previous trading date's stale price. The fill should wait for the first bar of the current session to be
    /// available. <see cref="QuantConnect.Orders.Fills.EquityFillModel.MarketFill"/>
    /// </summary>
    public class MarketOrderFillAtMarketOpenRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;

            // Place a market order one second after the open, before the first minute bar of the session is available
            Schedule.On(DateRules.EveryDay(_spy), TimeRules.At(9, 30, 1), Trade);
        }

        private void Trade()
        {
            // Only trade once the security already has data, so the fill would be attempted against the previous
            // trading date's bar (the stale data we want to avoid filling on), not against an empty cache
            if (Securities[_spy].HasData)
            {
                MarketOrder(_spy, 1);
            }
        }

        /// <summary>
        /// Asserts that market orders are never filled on stale data within the first bar after the open
        /// </summary>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var exchangeHours = Securities[orderEvent.Symbol].Exchange.Hours;
            var fillLocalTime = orderEvent.UtcTime.ConvertFromUtc(exchangeHours.TimeZone);
            var marketOpen = exchangeHours.GetPreviousMarketOpen(fillLocalTime, extendedMarketHours: false);

            // The order is submitted one second after the open. Without waiting for the first session bar it would
            // fill immediately on the previous trading date's stale price. The fill must only happen once the first
            // minute bar (the subscription resolution) is available, i.e. at least one minute after the open.
            if (fillLocalTime - marketOpen < QuantConnect.Time.OneMinute)
            {
                throw new RegressionTestException(
                    $"Order {orderEvent.OrderId} for {orderEvent.Symbol} filled at {fillLocalTime} on stale data, " +
                    $"within the first minute after the market open {marketOpen}. It should have waited for the first " +
                    $"bar of the session to be available before filling.");
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
        public long DataPoints => 3943;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.486%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100006.20"},
            {"Net Profit", "0.006%"},
            {"Sharpe Ratio", "-5.157"},
            {"Sortino Ratio", "-19.237"},
            {"Probabilistic Sharpe Ratio", "72.422%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.01"},
            {"Beta", "0.003"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.92"},
            {"Tracking Error", "0.222"},
            {"Treynor Ratio", "-1.182"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$4300000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.12%"},
            {"Drawdown Recovery", "2"},
            {"OrderListHash", "f76c5144075b62eaf924fe29632b92f4"}
        };
    }
}
