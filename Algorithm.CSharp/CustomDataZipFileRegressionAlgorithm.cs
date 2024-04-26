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
using System.Globalization;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that custom data can be sourced from a remote CSV zipped file.
    /// </summary>
    public class CustomDataZipFileRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _customDataSymbol;
        private bool _receivedCustomData;

        public override void Initialize()
        {
            SetStartDate(2021, 01, 01);
            SetEndDate(2021, 12, 31);

            CustomData.Url = GetCustomDataUrl();

            _customDataSymbol = AddData<CustomData>("CustomData", Resolution.Daily).Symbol;

            SetBenchmark(x => 0);
        }

        public override void OnData(Slice slice)
        {
            var data = slice.Get<CustomData>(_customDataSymbol);
            if (data != null)
            {
                Log($"{Time}: {data.Symbol} - {data.Time} - {data.Value}");
                _receivedCustomData = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_receivedCustomData)
            {
                throw new Exception("Custom data was not received");
            }
        }

        protected virtual string GetCustomDataUrl()
        {
            return @"https://cdn.quantconnect.com/uploads/multi_csv_zipped_file.zip?some=query&for=testing";
        }

        public class CustomData : BaseData
        {
            public static string Url { get; set; }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource(Url);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.ToCsv(2);

                try
                {
                    return new CustomData()
                    {
                        Symbol = config.Symbol,
                        EndTime = date.Date.AddMilliseconds(Convert.ToInt32(csv[0], CultureInfo.InvariantCulture)).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone),
                        Value = Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture),
                    };
                }
                catch
                {
                    return null;
                }
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
        public virtual long DataPoints => 79;

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
