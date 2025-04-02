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
    /// Regression test for consistency of hour data over a reverse split event in US equities.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="regression test" />
    public class HourSplitRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private bool _receivedWarningEvent;
        private bool _receivedOccurredEvent;
        private int _dataCount;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 9);
            SetCash(100000);
            SetBenchmark(x => 0);

            _symbol = AddEquity("AAPL", Resolution.Hour).Symbol;
        }

        public override void OnData(Slice slice)
        {
            _dataCount += slice.Bars.Count;

            TradeBar bar;
            if (!slice.Bars.TryGetValue(_symbol, out bar)) return;

            if (!Portfolio.Invested && Time.Date == EndDate.Date)
            {
                Buy(_symbol, 1);
            }
        }

        public override void OnSplits(Splits splits)
        {
            if (splits.Single().Value.Type == SplitType.Warning)
            {
                _receivedWarningEvent = true;
                Debug($"{splits.Single().Value}");
            }
            else if (splits.Single().Value.Type == SplitType.SplitOccurred)
            {
                _receivedOccurredEvent = true;
                if (splits.Single().Value.Price != 645.5700m || splits.Single().Value.ReferencePrice != 645.5700m)
                {
                    throw new RegressionTestException("Did not receive expected price values");
                }
                Debug($"{splits.Single().Value}");
            }
        }
        public override void OnEndOfAlgorithm()
        {
            if (!_receivedOccurredEvent)
            {
                throw new RegressionTestException("Did not receive expected split event");
            }
            if (!_receivedWarningEvent)
            {
                throw new RegressionTestException("Did not receive expected split warning event");
            }
            if (_dataCount != 14)
            {
                throw new RegressionTestException($"Unexpected data count {_dataCount}. Expected 14");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 17;

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
            {"Compounding Annual Return", "-0.068%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99999.31"},
            {"Net Profit", "-0.001%"},
            {"Sharpe Ratio", "-128.305"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.163"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$160000000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.01%"},
            {"OrderListHash", "fded8f29d111ed771b99bc6b296f776c"}
        };
    }
}
