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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5507;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "45.475%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100497.59"},
            {"Net Profit", "0.498%"},
            {"Sharpe Ratio", "9.126"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.977%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.439"},
            {"Beta", "-0.184"},
            {"Annual Standard Deviation", "0.039"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-1.093"},
            {"Tracking Error", "0.059"},
            {"Treynor Ratio", "-1.956"},
            {"Total Fees", "$5.00"},
            {"Estimated Strategy Capacity", "$26000.00"},
            {"Lowest Capacity Asset", "AOL R735QTJ8XC9X"},
            {"Portfolio Turnover", "12.68%"},
            {"OrderListHash", "607f85b69d45d427242a614b9619c502"}
        };
    }
}
