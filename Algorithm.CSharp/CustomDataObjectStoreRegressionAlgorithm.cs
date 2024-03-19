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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating the use of custom data sourced from the object store
    /// </summary>
    public class CustomDataObjectStoreRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual string CustomDataKey => "CustomData/ExampleCustomData";

        protected readonly static string CustomData = "2017-08-18 01:00:00,5749.5,5852.95,5749.5,5842.2,214402430,8753.33\n2017-08-18 02:00:00,5834.1,5904.35,5822.2,5898.85,144794030,5405.72\n2017-08-18 03:00:00,5885.5,5898.8,5852.3,5857.55,145721790,5163.09\n2017-08-18 04:00:00,5811.95,5815,5760.4,5770.9,160523863,5219.24\n2017-08-18 05:00:00,5794.75,5848.2,5786.05,5836.95,151929179,5429.87\n2017-08-18 06:00:00,5889.95,5900.45,5858.45,5867.9,123586417,4303.93\n2017-08-18 07:00:00,5833.15,5833.85,5775.55,5811.55,127624733,4823.52\n2017-08-18 08:00:00,5834.6,5864.95,5834.6,5859,110427867,4661.55\n2017-08-18 09:00:00,5869.9,5879.35,5802.85,5816.7,117516350,4820.53\n2017-08-18 10:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-18 11:00:00,6000.5,6019,5951.15,6009,127707078,6591.27\n2017-08-18 12:00:00,5991.2,6038.2,5980.95,6030.8,116275729,4641.97\n2017-08-18 13:00:00,5930.8,5966.05,5910.95,5955.25,151162819,5915.8\n2017-08-18 14:00:00,5972.25,5989.8,5926.75,5973.3,191516153,8349.59\n2017-08-18 15:00:00,5984.7,6051.1,5974.55,6038.05,171728134,7774.83\n2017-08-18 16:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-18 17:00:00,6000.5,6019,5951.15,6009,127707078,6591.27\n2017-08-18 18:00:00,5991.2,6038.2,5980.95,6030.8,116275729,4641.97\n2017-08-18 19:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-18 20:00:00,5895,5956.55,5869.5,5921.4,114174694,4961.54\n2017-08-18 21:00:00,5900.05,5972.7,5871.3,5881,118346364,4888.65\n2017-08-18 22:00:00,5907.9,5931.65,5857.4,5878,100130739,4304.75\n2017-08-18 23:00:00,5848.75,5868.05,5780.35,5788.8,180902123,6695.57\n2017-08-19 01:00:00,5771.75,5792.9,5738.6,5760.2,140394424,5894.04\n2017-08-19 02:00:00,5709.35,5729.85,5683.1,5699.1,142041404,5462.45\n2017-08-19 03:00:00,5748.95,5819.4,5739.4,5808.4,124410018,5121.33\n2017-08-19 04:00:00,5820.4,5854.9,5770.25,5850.05,107160887,4560.84\n2017-08-19 05:00:00,5841.9,5863.4,5804.3,5813.6,117541145,4591.91\n2017-08-19 06:00:00,5805.75,5828.4,5777.9,5822.25,115539008,4643.17\n2017-08-19 07:00:00,5754.15,5755,5645.65,5655.9,198400131,7148\n2017-08-19 08:00:00,5639.9,5686.15,5616.85,5667.65,182410583,6697.18\n2017-08-19 09:00:00,5638.05,5640,5566.25,5590.25,193488581,6308.88\n2017-08-19 10:00:00,5606.95,5666.25,5570.25,5609.1,196571543,6792.49\n2017-08-19 11:00:00,5627.95,5635.25,5579.35,5588.7,160095940,5939.3\n2017-08-19 12:00:00,5647.95,5699.35,5630.95,5682.35,239029425,9184.29\n2017-08-19 13:00:00,5749.5,5852.95,5749.5,5842.2,214402430,8753.33\n2017-08-19 14:00:00,5834.1,5904.35,5822.2,5898.85,144794030,5405.72\n2017-08-19 15:00:00,5885.5,5898.8,5852.3,5857.55,145721790,5163.09\n2017-08-19 16:00:00,5811.95,5815,5760.4,5770.9,160523863,5219.24\n2017-08-19 17:00:00,5794.75,5848.2,5786.05,5836.95,151929179,5429.87\n2017-08-19 18:00:00,5889.95,5900.45,5858.45,5867.9,123586417,4303.93\n2017-08-19 19:00:00,5833.15,5833.85,5775.55,5811.55,127624733,4823.52\n2017-08-19 20:00:00,5834.6,5864.95,5834.6,5859,110427867,4661.55\n2017-08-19 21:00:00,5869.9,5879.35,5802.85,5816.7,117516350,4820.53\n2017-08-19 22:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-19 23:00:00,6000.5,6019,5951.15,6009,127707078,6591.27\n2017-08-21 01:00:00,5749.5,5852.95,5749.5,5842.2,214402430,8753.33\n2017-08-21 02:00:00,5834.1,5904.35,5822.2,5898.85,144794030,5405.72\n2017-08-21 03:00:00,5885.5,5898.8,5852.3,5857.55,145721790,5163.09\n2017-08-21 04:00:00,5811.95,5815,5760.4,5770.9,160523863,5219.24\n2017-08-21 05:00:00,5794.75,5848.2,5786.05,5836.95,151929179,5429.87\n2017-08-21 06:00:00,5889.95,5900.45,5858.45,5867.9,123586417,4303.93\n2017-08-21 07:00:00,5833.15,5833.85,5775.55,5811.55,127624733,4823.52\n2017-08-21 08:00:00,5834.6,5864.95,5834.6,5859,110427867,4661.55\n2017-08-21 09:00:00,5869.9,5879.35,5802.85,5816.7,117516350,4820.53\n2017-08-21 10:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-21 11:00:00,6000.5,6019,5951.15,6009,127707078,6591.27\n2017-08-21 12:00:00,5991.2,6038.2,5980.95,6030.8,116275729,4641.97\n2017-08-21 13:00:00,5930.8,5966.05,5910.95,5955.25,151162819,5915.8\n2017-08-21 14:00:00,5972.25,5989.8,5926.75,5973.3,191516153,8349.59\n2017-08-21 15:00:00,5984.7,6051.1,5974.55,6038.05,171728134,7774.83\n2017-08-21 16:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-21 17:00:00,6000.5,6019,5951.15,6009,127707078,6591.27\n2017-08-21 18:00:00,5991.2,6038.2,5980.95,6030.8,116275729,4641.97\n2017-08-21 19:00:00,5894.5,5948.85,5887.95,5935.1,120195681,4882.29\n2017-08-21 20:00:00,5895,5956.55,5869.5,5921.4,114174694,4961.54\n2017-08-21 21:00:00,5900.05,5972.7,5871.3,5881,118346364,4888.65\n2017-08-21 22:00:00,5907.9,5931.65,5857.4,5878,100130739,4304.75\n2017-08-21 23:00:00,5848.75,5868.05,5780.35,5788.8,180902123,6695.57";

        private Symbol _customSymbol;

        private List<ExampleCustomData> _receivedData = new();

        public override void Initialize()
        {
            SetStartDate(2017, 8, 18);
            SetEndDate(2017, 8, 21);
            SetCash(100000);

            SetBenchmark(x => 0);

            ExampleCustomData.CustomDataKey = CustomDataKey;

            _customSymbol = AddData<ExampleCustomData>("ExampleCustomData", Resolution.Hour).Symbol;

            // Saving data here for demonstration and regression testing purposes.
            // In real scenarios, data has to be saved to the object store before the algorithm starts.
            SaveDataToObjectStore();
        }

        public override void OnData(Slice slice)
        {
            if (slice.ContainsKey(_customSymbol))
            {
                var customData = slice.Get<ExampleCustomData>(_customSymbol);
                if (customData.Price == 0)
                {
                    throw new Exception("Custom data price was not expected to be zero");
                }

                _receivedData.Add(customData);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_receivedData.Count == 0)
            {
                throw new Exception("Custom data was not fetched");
            }

            var customSecurity = Securities[_customSymbol];
            if (customSecurity == null || customSecurity.Price == 0)
            {
                throw new Exception("Expected the custom security to be added to the algorithm securities and to have a price that is not zero");
            }

            // Make sure history requests work as expected
            var history = History<ExampleCustomData>(_customSymbol, StartDate, EndDate, Resolution.Hour).ToList();

            if (history.Count != _receivedData.Count)
            {
                throw new Exception("History request returned different data than expected");
            }

            for (var i = 0; i < history.Count; i++)
            {
                if (!history[i].Equals(_receivedData[i]))
                {
                    throw new Exception("History request returned different data than expected");
                }
            }
        }

        protected virtual void SaveDataToObjectStore()
        {
            ObjectStore.Save(CustomDataKey, CustomData);
        }

        public class ExampleCustomData : BaseData
        {
            public static string CustomDataKey { get; set; }

            public decimal Open;
            public decimal High;
            public decimal Low;
            public decimal Close;

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource(CustomDataKey, SubscriptionTransportMedium.ObjectStore, FileFormat.Csv);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(",");
                var data = new ExampleCustomData()
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

            public bool Equals(ExampleCustomData other)
            {
                return other != null &&
                    Symbol == other.Symbol &&
                    Time == other.Time &&
                    Value == other.Value &&
                    Open == other.Open &&
                    High == other.High &&
                    Low == other.Low &&
                    Close == other.Close;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 70;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 69;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
}
