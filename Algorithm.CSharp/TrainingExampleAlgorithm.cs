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
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm showing how to use QCAlgorithm.Train method
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="training" />
    /// </summary>
    public class TrainingExampleAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Queue<DateTime> _trainTimes = new();
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 14);

            AddEquity("SPY", Resolution.Daily);

            // Set TrainingMethod to be executed immediately
            Train(TrainingMethod);

            // Set TrainingMethod to be executed at 8:00 am every Sunday
            Train(DateRules.Every(DayOfWeek.Sunday), TimeRules.At(8, 0), TrainingMethod);
        }

        private void TrainingMethod()
        {
            Log($"Start training at {Time}");
            // Use the historical data to train the machine learning model
            var history = History("SPY", 200, Resolution.Daily);

            // ML code:


            // let's keep this to assert in the end of the algorithm
            _trainTimes.Enqueue(Time);
        }

        /// <summary>
        /// Let's assert the behavior of our traning schedule
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_trainTimes.Count != 2)
            {
                throw new Exception($"Unexpected train count: {_trainTimes.Count}");
            }
            if (_trainTimes.Dequeue() != StartDate
                || _trainTimes.Dequeue() != new DateTime(2013, 10, 13, 8, 0, 0))
            {
                throw new Exception($"Unexpected train times!");
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
        public long DataPoints => 56;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-7.357"},
            {"Tracking Error", "0.161"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
