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
 *
*/

using System;
using System.Linq;
using QuantConnect.Data;
using System.Globalization;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    public class DescendingCustomDataObjectStoreRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual string CustomDataKey => "CustomData/SortCustomData";

        protected readonly static string[] descendingCustomData = new string[]
        {
            "2024-10-03 19:00:00,173.5,176.0,172.0,175.2,120195681,4882.29",
            "2024-10-02 18:00:00,174.0,177.0,173.0,175.8,116275729,4641.97",
            "2024-10-01 17:00:00,175.0,178.0,172.5,174.5,127707078,6591.27",
            "2024-09-30 11:00:00,174.8,176.5,172.8,175.0,127707078,6591.27",
            "2024-09-27 10:00:00,172.5,175.0,171.5,173.5,120195681,4882.29",
            "2024-09-26 09:00:00,171.0,172.5,170.0,171.8,117516350,4820.53",
            "2024-09-25 08:00:00,169.5,172.0,169.0,171.0,110427867,4661.55",
            "2024-09-24 07:00:00,170.0,171.0,168.0,169.5,127624733,4823.52",
            "2024-09-23 06:00:00,172.0,173.5,169.5,171.5,123586417,4303.93",
            "2024-09-20 05:00:00,168.0,171.0,167.5,170.5,151929179,5429.87",
            "2024-09-19 04:00:00,170.5,171.5,166.0,167.0,160523863,5219.24",
            "2024-09-18 03:00:00,173.0,174.0,169.0,172.0,145721790,5163.09",
            "2024-09-17 02:00:00,171.0,173.5,170.0,172.5,144794030,5405.72",
            "2024-09-16 01:00:00,168.0,171.0,167.0,170.0,214402430,8753.33",
            "2024-09-13 16:00:00,173.5,176.0,172.0,175.2,120195681,4882.29",
            "2024-09-12 15:00:00,174.5,177.5,173.5,176.5,171728134,7774.83",
            "2024-09-11 14:00:00,175.0,178.0,174.0,175.5,191516153,8349.59",
            "2024-09-10 13:00:00,174.5,176.0,173.0,174.0,151162819,5915.8",
            "2024-09-09 12:00:00,176.0,178.0,175.0,177.0,116275729,4641.97"
        };

        private Symbol _customSymbol;

        private List<SortCustomData> _receivedData = new();

        public override void Initialize()
        {
            SetStartDate(2024, 09, 09);
            SetEndDate(2024, 10, 03);
            SetCash(100000);

            SetBenchmark(x => 0);

            SortCustomData.CustomDataKey = CustomDataKey;

            _customSymbol = AddData<SortCustomData>("SortCustomData", Resolution.Daily).Symbol;

            // Saving data here for demonstration and regression testing purposes.
            // In real scenarios, data has to be saved to the object store before the algorithm starts.
            ObjectStore.Save(CustomDataKey, string.Join("\n", descendingCustomData));
        }

        public override void OnData(Slice slice)
        {
            if (slice.ContainsKey(_customSymbol))
            {
                var sortCustomData = slice.Get<SortCustomData>(_customSymbol);
                if (sortCustomData.Open == 0 || sortCustomData.High == 0 || sortCustomData.Low == 0 || sortCustomData.Close == 0 || sortCustomData.Price == 0)
                {
                    throw new RegressionTestException("One or more custom data fields (Open, High, Low, Close, Price) are zero.");
                }

                _receivedData.Add(sortCustomData);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_receivedData.Count == 0)
            {
                throw new RegressionTestException("Custom data was not fetched");
            }

            var history = History<SortCustomData>(_customSymbol, StartDate, EndDate, Resolution.Hour).ToList();

            if (history.Count != _receivedData.Count)
            {
                throw new RegressionTestException("History request returned different data than expected");
            }

            // Iterate through the history collection, checking if the EndTime is in ascending order.
            for (int i = 0; i < history.Count - 1; i++)
            {
                if (history[i].EndTime > history[i + 1].EndTime)
                {
                    throw new RegressionTestException($"Order failure: {history[i].EndTime} > {history[i + 1].EndTime} at index {i}.");
                }
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages => new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all TimeSlices of algorithm
        /// </summary>
        public long DataPoints => 20;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 19;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new ()
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }

    public class SortCustomData : BaseData
    {
        public static string CustomDataKey { get; set; }

        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(CustomDataKey, SubscriptionTransportMedium.ObjectStore, FileFormat.Csv)
            {
                // Indicate that the data from the subscription will be returned in descending order.
                Sort = true
            };
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var csv = line.Split(",");
            var data = new SortCustomData()
            {
                Symbol = config.Symbol,
                Time = DateTime.ParseExact(csv[0], DateFormat.DB, CultureInfo.InvariantCulture),
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
