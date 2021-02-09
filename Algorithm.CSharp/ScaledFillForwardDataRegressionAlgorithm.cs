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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression test algorithm reproduces issue https://github.com/QuantConnect/Lean/issues/4834
    /// fixed in PR https://github.com/QuantConnect/Lean/pull/4836
    /// Adjusted data of fill forward bars should use original scale factor
    /// </summary>
    public class ScaledFillForwardDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private TradeBar _lastRealBar;
        private Symbol _twx;
        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 9);

            _twx = AddEquity("TWX", Resolution.Minute, extendedMarketHours: true).Symbol;
            Schedule.On(DateRules.EveryDay(_twx), TimeRules.Every(TimeSpan.FromHours(1)), PlotPrice);
        }

        private void PlotPrice()
        {
            Plot($"{_twx}", "Ask", Securities[_twx].AskPrice);
            Plot($"{_twx}", "Bid", Securities[_twx].BidPrice);
            Plot($"{_twx}", "Price", Securities[_twx].Price);
            Plot("Portfolio.TPV", "Value", Portfolio.TotalPortfolioValue);
        }

        public override void OnData(Slice data)
        {
            var current = data.Bars.FirstOrDefault().Value;
            if (current != null)
            {
                if (Time == new DateTime(2014, 06, 09, 4, 1, 0) && !Portfolio.Invested)
                {
                    if (!current.IsFillForward)
                    {
                        throw new Exception($"Was expecting a first fill forward bar {Time}");
                    }

                    // trade on the first bar after a factor price scale change. +10 so we fill ASAP. Limit so it fills in extended market hours
                    LimitOrder(_twx, 1000, _lastRealBar.Close + 10);
                }

                if (_lastRealBar == null || !current.IsFillForward)
                {
                    _lastRealBar = current;
                }
                else if (_lastRealBar.Close != current.Close)
                {
                    throw new Exception($"FillForwarded data point at {Time} was scaled. Actual: {current.Close}; Expected: {_lastRealBar.Close}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_lastRealBar == null)
            {
                throw new Exception($"Not all expected data points were received.");
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
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "32.825%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.377%"},
            {"Sharpe Ratio", "8.953"},
            {"Probabilistic Sharpe Ratio", "95.977%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.314"},
            {"Beta", "-0.104"},
            {"Annual Standard Deviation", "0.03"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-3.498"},
            {"Tracking Error", "0.05"},
            {"Treynor Ratio", "-2.573"},
            {"Total Fees", "$5.00"},
            {"Fitness Score", "0.158"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.158"},
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
            {"OrderListHash", "fb4a3d12fdcb4f06fa421f37c7942dd1"}
        };
    }
}
