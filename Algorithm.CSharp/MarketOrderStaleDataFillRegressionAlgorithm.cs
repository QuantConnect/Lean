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
    /// Regression algorithm reproducing the stale fill issue: a market order must not be filled on data that is older
    /// than the subscribed resolution (the latest available bar is more than one resolution behind the fill time),
    /// regardless of the time of day. The fill should wait until fresh data is available.
    ///
    /// Two orders are submitted to cover both cases:
    ///  - right after the market open (9:30:01), when the only data available is the previous trading date's bar
    ///    (the original reported case), and
    ///  - mid-session (noon), where data is continuous and the order fills normally on fresh data.
    /// <see cref="QuantConnect.Orders.Fills.EquityFillModel.MarketFill"/>
    /// </summary>
    public class MarketOrderStaleDataFillRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private System.TimeSpan _resolutionSpan;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            var resolution = Resolution.Minute;
            _resolutionSpan = resolution.ToTimeSpan();
            _spy = AddEquity("SPY", resolution).Symbol;

            // Market open: order one second after the open, before the first minute bar of the session is available
            Schedule.On(DateRules.EveryDay(_spy), TimeRules.At(9, 30, 1), Trade);
            // Mid-session: data is continuous here, so the order fills normally on fresh data
            Schedule.On(DateRules.EveryDay(_spy), TimeRules.At(12, 0, 0), Trade);
        }

        private void Trade()
        {
            // Only trade once the security already has data, so a stale fill would be attempted against an older bar
            // (the data we want to avoid filling on) rather than against an empty cache
            if (Securities[_spy].HasData)
            {
                MarketOrder(_spy, 1);
            }
        }

        /// <summary>
        /// Asserts that market orders are never filled on data staler than the subscribed resolution, at any time of day
        /// </summary>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var security = Securities[orderEvent.Symbol];
            var lastData = security.GetLastData();
            if (lastData == null)
            {
                return;
            }

            // The latest available data must be within one resolution bar of the fill time. Without the fix, a market
            // order placed when only stale data is available (e.g. right after the open, before the first session bar)
            // would fill on that stale price instead of waiting for fresh data.
            var dataGap = orderEvent.UtcTime - lastData.EndTime.ConvertToUtc(security.Exchange.TimeZone);
            if (dataGap > _resolutionSpan)
            {
                throw new RegressionTestException(
                    $"Order {orderEvent.OrderId} for {orderEvent.Symbol} filled at {orderEvent.UtcTime} UTC on stale " +
                    $"data ending {lastData.EndTime} ({dataGap} behind, more than the {_resolutionSpan} resolution). " +
                    $"The fill should have waited for fresh data.");
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
            {"Total Orders", "9"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1.007%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100012.81"},
            {"Net Profit", "0.013%"},
            {"Sharpe Ratio", "1.079"},
            {"Sortino Ratio", "3.268"},
            {"Probabilistic Sharpe Ratio", "50.232%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.012"},
            {"Beta", "0.007"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.931"},
            {"Tracking Error", "0.221"},
            {"Treynor Ratio", "0.244"},
            {"Total Fees", "$9.00"},
            {"Estimated Strategy Capacity", "$290000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.26%"},
            {"Drawdown Recovery", "2"},
            {"OrderListHash", "c849aa80f00dde3d93bf4cc6d65c4d5e"}
        };
    }
}
