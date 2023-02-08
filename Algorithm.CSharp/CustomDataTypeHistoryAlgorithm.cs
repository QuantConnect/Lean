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
using System.Linq;

using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class CustomDataTypeHistoryAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        public override void Initialize()
        {
            SetStartDate(2010, 1, 1);  //Set Start Date
            SetEndDate(2010, 1, 2);
            SetCash(100000);             //Set Strategy Cash

            _symbol = AddData<MyCustomDataType>("MyCustomDataType", Resolution.Daily).Symbol;
            var history = History<MyCustomDataType>(_symbol, 30, Resolution.Daily);

            Log($"History count: {history.Count()}");

            foreach (var data in history)
            {
                Quit("Got data!");
            }
        }

        public class MyCustomDataType : DynamicData
        {
            public decimal Open;
            public decimal High;
            public decimal Low;
            public decimal Close;

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line,DateTime date, bool isLiveMode)
            {
                if (string.IsNullOrWhiteSpace(line.Trim()))
                {
                    return null;
                }

                var index = new MyCustomDataType();
                index.Symbol = config.Symbol;

                try
                {
                    var data = line.Split(',');
                    index.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                    index.EndTime = index.Time.AddDays(1);
                    index.Open = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                    index.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                    index.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                    index.Close = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                    index.Value = index.Close;
                }
                catch
                {
                    return null;
                }

                return index;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 58;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "9"},
            {"Average Win", "0.86%"},
            {"Average Loss", "-0.27%"},
            {"Compounding Annual Return", "184.364%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "1.781"},
            {"Net Profit", "1.442%"},
            {"Sharpe Ratio", "4.86"},
            {"Probabilistic Sharpe Ratio", "59.497%"},
            {"Loss Rate", "33%"},
            {"Win Rate", "67%"},
            {"Profit-Loss Ratio", "3.17"},
            {"Alpha", "4.181"},
            {"Beta", "-1.322"},
            {"Annual Standard Deviation", "0.321"},
            {"Annual Variance", "0.103"},
            {"Information Ratio", "-0.795"},
            {"Tracking Error", "0.532"},
            {"Treynor Ratio", "-1.18"},
            {"Total Fees", "$14.78"},
            {"Estimated Strategy Capacity", "$47000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Fitness Score", "0.408"},
            {"Kelly Criterion Estimate", "16.559"},
            {"Kelly Criterion Probability Value", "0.316"},
            {"Sortino Ratio", "12.447"},
            {"Return Over Maximum Drawdown", "106.327"},
            {"Portfolio Turnover", "0.411"},
            {"Total Insights Generated", "3"},
            {"Total Insights Closed", "3"},
            {"Total Insights Analysis Completed", "3"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "3"},
            {"Long/Short Ratio", "0%"},
            {"Estimated Monthly Alpha Value", "$20784418.6104"},
            {"Total Accumulated Estimated Alpha Value", "$3579538.7607"},
            {"Mean Population Estimated Insight Value", "$1193179.5869"},
            {"Mean Population Direction", "100%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "100%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "9da9afe1e9137638a55db1676adc2be1"}
        };
    }
}
