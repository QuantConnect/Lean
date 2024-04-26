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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Reproduces Lean GH issue 3572: benchmark _would_ use custom data versus equity
    /// when both were present
    /// </summary>
    public class CustomBenchmarkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Algorithm initialization
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            AddEquity("AAPL", Resolution.Hour);
            AddData<DummyCustomData>("AAPL");

            // set benchmark will use equity AAPL as benchmark
            SetBenchmark("AAPL");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("AAPL", 1);
                Debug("Purchased Stock");
            }
        }
        private class DummyCustomData : BaseData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("NonExistingFile", SubscriptionTransportMedium.LocalFile);
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
        public long DataPoints => 1965;

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
            {"Compounding Annual Return", "34.768%"},
            {"Drawdown", "2.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100382.23"},
            {"Net Profit", "0.382%"},
            {"Sharpe Ratio", "5.446"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "60.047%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.001"},
            {"Beta", "0.997"},
            {"Annual Standard Deviation", "0.179"},
            {"Annual Variance", "0.032"},
            {"Information Ratio", "-7.724"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0.978"},
            {"Total Fees", "$32.11"},
            {"Estimated Strategy Capacity", "$66000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "20.08%"},
            {"OrderListHash", "ae85de0e28b44116de68c56ca141c00f"}
        };
    }
}
