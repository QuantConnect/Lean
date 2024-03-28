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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert we can have custom data subscriptions with different exchanges
    /// </summary>
    public class CustomDataWorksWithDifferentExchangesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _noDataPointsReceived;
        public override void Initialize()
        {
            SetStartDate(2014, 05, 02);
            SetEndDate(2014, 05, 03);

            var market1 = AddForex("EURUSD", Resolution.Hour, Market.FXCM);
            var firstCustomSecurity = AddData<ExampleCustomData>(market1.Symbol, Resolution.Hour, TimeZones.Utc, false);
            if (firstCustomSecurity.Exchange.TimeZone != TimeZones.Utc)
            {
                throw new Exception($"The time zone of security {firstCustomSecurity} should be {TimeZones.Utc}, but it was {firstCustomSecurity.Exchange.TimeZone}");
            }

            var market2 = AddForex("EURUSD", Resolution.Hour, Market.Oanda);
            var secondCustomSecurity = AddData<ExampleCustomData>(market2.Symbol, Resolution.Hour, TimeZones.Utc, false);
            if (secondCustomSecurity.Exchange.TimeZone != TimeZones.Utc)
            {
                throw new Exception($"The time zone of security {secondCustomSecurity} should be {TimeZones.Utc}, but it was {secondCustomSecurity.Exchange.TimeZone}");
            }
            _noDataPointsReceived = true;
        }


        public override void OnData(Slice slice)
        {
            _noDataPointsReceived = false;
            if (slice.Count != ActiveSecurities.Count)
            {
                throw new Exception($"{ActiveSecurities.Count.ToString().ToCamelCase()} data points were expected, but only {slice.Count} were received");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_noDataPointsReceived)
            {
                throw new Exception($"No points were received");
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
        public long DataPoints => 94;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

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
            {"Start Equity", "100000.00"},
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

    public class ExampleCustomData : BaseData
    {
        private int _hours = 0;
        public decimal Open;
        public decimal High;
        public decimal Low;
        public decimal Close;

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
                Time = date.AddHours(_hours),
                Value = csv[4].ToDecimal(),
                Open = csv[1].ToDecimal(),
                High = csv[2].ToDecimal(),
                Low = csv[3].ToDecimal(),
                Close = csv[4].ToDecimal()
            };

            _hours = (_hours + 1) % 22;
            return data;
        }
    }
}
