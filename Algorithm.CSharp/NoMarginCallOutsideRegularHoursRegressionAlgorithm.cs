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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm that ensures margin call orders are only triggered during regular market hours.
    /// This test sets up a short position that would cause a margin call near market close.
    /// The algorithm is expected to throw an exception if margin call orders are submitted while the market is closed.
    /// </summary>
    public class NoMarginCallOutsideRegularHoursRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            // Set portfolio to fully allocated for margin call triggering
            Settings.FreePortfolioValuePercentage = 0m;
            var equity = AddEquity("SPY", Resolution.Minute, extendedMarketHours: true);
            equity.BuyingPowerModel = new PatternDayTradingMarginModel(2m, 4m);
            _spy = equity.Symbol;
        }

        /// <summary>
        /// Sets a short position large enough to trigger a margin call.
        /// The position is opened just before market close to simulate after-hours behavior.
        /// </summary>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && Time.Hour == 15 && Time.Minute == 48)
            {
                SetHoldings(_spy, -2.1m);
            }
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            foreach (var request in requests)
            {
                var security = Portfolio.Securities[request.Symbol];

                // Ensure margin call orders only happen when the exchange is open
                if (!security.Exchange.ExchangeOpen)
                {
                    throw new RegressionTestException("Margin calls should not occur outside regular market hours!");
                }
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
        public long DataPoints => 9643;

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
            {"Compounding Annual Return", "-93.216%"},
            {"Drawdown", "7.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "96499.74"},
            {"Net Profit", "-3.500%"},
            {"Sharpe Ratio", "-2.407"},
            {"Sortino Ratio", "-5.131"},
            {"Probabilistic Sharpe Ratio", "18.859%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "2.363"},
            {"Beta", "-1.662"},
            {"Annual Standard Deviation", "0.382"},
            {"Annual Variance", "0.146"},
            {"Information Ratio", "-4.824"},
            {"Tracking Error", "0.6"},
            {"Treynor Ratio", "0.554"},
            {"Total Fees", "$7.24"},
            {"Estimated Strategy Capacity", "$14000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "41.81%"},
            {"OrderListHash", "6cc0fe6a302a15043b93b6c04336771b"}
        };
    }
}
