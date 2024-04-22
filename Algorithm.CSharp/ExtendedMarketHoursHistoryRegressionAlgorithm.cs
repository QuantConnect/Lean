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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing doing some history requests outside market hours, reproducing GH issue #4783
    /// </summary>
    public class ExtendedMarketHoursHistoryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _minuteHistoryCount;
        private int _hourHistoryCount;
        private int _dailyHistoryCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 09);
            SetCash(100000);

            AddEquity("SPY", Resolution.Minute, extendedMarketHours:true, fillForward:false);

            Schedule.On("RunHistoryCall", DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromHours(1)), RunHistoryCall);
        }

        private void RunHistoryCall()
        {
            var spy = Securities["SPY"];
            var regularHours = spy.Exchange.Hours.IsOpen(Time, false);
            var extendedHours = !regularHours && spy.Exchange.Hours.IsOpen(Time, true);

            if (regularHours)
            {
                _minuteHistoryCount++;
                var history = History(spy.Symbol, 5, Resolution.Minute).Count();
                if (history != 5)
                {
                    throw new Exception($"Unexpected Minute data count: {history}");
                }
            }
            else
            {
                if (extendedHours)
                {
                    _hourHistoryCount++;
                    var history = History(spy.Symbol, 5, Resolution.Hour).Count();
                    if (history != 5)
                    {
                        throw new Exception($"Unexpected Hour data count {history}");
                    }
                }
                else
                {
                    _dailyHistoryCount++;
                    var history = History(spy.Symbol, 5, Resolution.Daily).Count();
                    if (history != 5)
                    {
                        throw new Exception($"Unexpected Daily data count {history}");
                    }
                }
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_minuteHistoryCount != 3 * 6)
            {
                throw new Exception($"Unexpected minute history requests count {_minuteHistoryCount}");
            }
            // 6 pre market from 4am to 9am + 4 post market 4pm to 7pm
            if (_hourHistoryCount != 3 * 10)
            {
                throw new Exception($"Unexpected hour history requests count {_hourHistoryCount}");
            }
            // 0am to 3am + 8pm to 11pm, last day ends at 8pm
            if (_dailyHistoryCount != (2 * 8 + 5))
            {
                throw new Exception($"Unexpected Daily history requests count: {_dailyHistoryCount}");
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
        public long DataPoints => 5215;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 435;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "19"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-73.997%"},
            {"Drawdown", "2.500%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "98959.88"},
            {"Net Profit", "-1.040%"},
            {"Sharpe Ratio", "-9.402"},
            {"Sortino Ratio", "-9.402"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.286"},
            {"Beta", "0.55"},
            {"Annual Standard Deviation", "0.075"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "0.914"},
            {"Tracking Error", "0.061"},
            {"Treynor Ratio", "-1.28"},
            {"Total Fees", "$21.45"},
            {"Estimated Strategy Capacity", "$830000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "34.15%"},
            {"OrderListHash", "6ebe462373e2ecc22de8eb2fe114d704"}
        };
    }
}
