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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Margin model regression algorithm testing <see cref="PatternDayTradingMarginModel"/> and
    /// margin calls NOT being triggered when the market is about to close, GH issue 4064.
    /// Brother too <see cref="MarginCallClosedMarketRegressionAlgorithm"/>
    /// </summary>
    public class NoMarginCallExpectedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _marginCall;
        private Symbol _spy;
        private decimal _closedMarketLeverage;
        private decimal _openMarketLeverage;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var security = AddEquity("SPY", Resolution.Minute);
            _spy = security.Symbol;

            _closedMarketLeverage = 2;
            _openMarketLeverage = 5;
            security.BuyingPowerModel = new PatternDayTradingMarginModel(_closedMarketLeverage, _openMarketLeverage);

            Schedule.On(
                DateRules.EveryDay(_spy),
                // 15 minutes before market close, because PatternDayTradingMarginModel starts using closed
                // market leverage 10 minutes before market closes.
                TimeRules.BeforeMarketClose(_spy, 15),
                () => {
                    // before market close we reduce our position to closed market leverage
                    SetHoldings(_spy, _closedMarketLeverage);
                }
            );

            Schedule.On(
                DateRules.EveryDay(_spy),
                TimeRules.AfterMarketOpen(_spy, 1), // 1 min so that price is set
                () => {
                    // at market open we increase our position to open market leverage
                    SetHoldings(_spy, _openMarketLeverage);
                }
            );
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            _marginCall++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_marginCall != 0)
            {
                throw new Exception($"We expected NO margin call to happen, {_marginCall} occurred");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "10"},
            {"Average Win", "2.45%"},
            {"Average Loss", "-1.97%"},
            {"Compounding Annual Return", "9355.767%"},
            {"Drawdown", "9.800%"},
            {"Expectancy", "0.347"},
            {"Net Profit", "5.989%"},
            {"Sharpe Ratio", "41.596"},
            {"Probabilistic Sharpe Ratio", "63.818%"},
            {"Loss Rate", "40%"},
            {"Win Rate", "60%"},
            {"Profit-Loss Ratio", "1.24"},
            {"Alpha", "27.257"},
            {"Beta", "3.729"},
            {"Annual Standard Deviation", "0.828"},
            {"Annual Variance", "0.686"},
            {"Information Ratio", "53.18"},
            {"Tracking Error", "0.611"},
            {"Treynor Ratio", "9.236"},
            {"Total Fees", "$103.34"},
            {"Fitness Score", "0.999"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "109.629"},
            {"Portfolio Turnover", "7.204"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "be464d03b7ef2440a78c8ad3ba4fbbf4"}
        };
    }
}
