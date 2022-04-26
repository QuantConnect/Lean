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
using System.IO;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class CustomDataBenchmarkRegressionAlgorithm: QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2017, 8, 18);
            SetEndDate(2017, 8, 21);
            SetCash(100000);

            SetBrokerageModel(Brokerages.BrokerageName.Default, AccountType.Margin);

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

        public bool CanRunLocally = true;

        public Language[] Languages = {Language.CSharp, Language.Python};

        public long DataPoints = 3;

        public int AlgorithmHistoryDataPoints = 3;

        public class ExampleCustomData : BaseData
        {
            public decimal Open;
            public decimal High;
            public decimal Low;
            public decimal Close;

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var source = Path.Combine(Globals.DataFolder, "path_to_my_csv_data.csv");
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
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
