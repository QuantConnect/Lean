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
using System.Globalization;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Benchmarks;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to demonstrate the use of SetBenchmark() with custom data
    /// </summary>
    public class CustomDataBenchmarkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2017, 8, 18);
            SetEndDate(2017, 8, 21);
            SetCash(100000);

            AddEquity("SPY", Resolution.Hour);
            var customSymbol = AddData<ExampleCustomData>("ExampleCustomData", Resolution.Hour).Symbol;
            SetBenchmark(customSymbol);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var securityBenchmark = (SecurityBenchmark)Benchmark;
            if (securityBenchmark.Security.Price == 0)
            {
                throw new RegressionTestException("Security benchmark price was not expected to be zero");
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
        public long DataPoints => 114;

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
            {"Compounding Annual Return", "29.610%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100281.67"},
            {"Net Profit", "0.282%"},
            {"Sharpe Ratio", "7.023"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.094"},
            {"Beta", "-0.016"},
            {"Annual Standard Deviation", "0.007"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-6.047"},
            {"Tracking Error", "0.439"},
            {"Treynor Ratio", "-3.13"},
            {"Total Fees", "$2.21"},
            {"Estimated Strategy Capacity", "$180000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "24.86%"},
            {"OrderListHash", "8c07dafc84c73401fa0c7709b6baf802"}
        };

        public class ExampleCustomData : BaseData
        {
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var source = "https://www.dl.dropboxusercontent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0";
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile, FileFormat.Csv);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(",");
                var data = new ExampleCustomData()
                {
                    Symbol = config.Symbol,
                    Time = DateTime.ParseExact(csv[0], DateFormat.DB, CultureInfo.InvariantCulture).AddHours(20),
                    Value = csv[4].ToDecimal(),
                    Open = csv[1].ToDecimal(),
                    High = csv[2].ToDecimal(),
                    Low = csv[3].ToDecimal(),
                    Close = csv[4].ToDecimal()
                };

                return data;
            }
        }
    }
}
