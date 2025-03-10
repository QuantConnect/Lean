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
using System.IO;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Adds a universe with a custom data type and retrieves historical data 
    /// while preserving the custom data type.
    /// </summary>
    public class PersistentCustomDataUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _universeSymbol;
        private bool _dataReceived;

        public override void Initialize()
        {
            SetStartDate(2018, 6, 1);
            SetEndDate(2018, 6, 19);

            var universe = AddUniverse<StockDataSource>("my-stock-data-source", Resolution.Daily, UniverseSelector);
            _universeSymbol = universe.Symbol;
            RetrieveHistoricalData();
        }

        private IEnumerable<Symbol> UniverseSelector(IEnumerable<BaseData> data)
        {
            foreach (var item in data.OfType<StockDataSource>())
            {
                yield return item.Symbol;
            }
        }

        private void RetrieveHistoricalData()
        {
            var history = History<StockDataSource>(_universeSymbol, new DateTime(2018, 1, 1), new DateTime(2018, 6, 1), Resolution.Daily).ToList();
            if (history.Count == 0)
            {
                throw new RegressionTestException($"No historical data received for the symbol {_universeSymbol}.");
            }

            // Ensure all values are of type StockDataSource
            foreach (var item in history)
            {
                if (item is not StockDataSource)
                {
                    throw new RegressionTestException($"Unexpected data type in history. Expected StockDataSource but received {item.GetType().Name}.");
                }
            }
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_universeSymbol))
            {
                throw new RegressionTestException($"No data received for the universe symbol: {_universeSymbol}.");
            }
            if (!_dataReceived)
            {
                RetrieveHistoricalData();
            }
            _dataReceived = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_dataReceived)
            {
                throw new RegressionTestException("No data was received after the universe selection.");
            }
        }


        /// <summary>
        /// Our custom data type that defines where to get and how to read our backtest and live data.
        /// </summary>
        public class StockDataSource : BaseData
        {
            public List<string> Symbols { get; set; }

            public StockDataSource()
            {
                Symbols = new List<string>();
            }

            public override DateTime EndTime
            {
                get { return Time + Period; }
                set { Time = value - Period; }
            }

            public TimeSpan Period
            {
                get { return QuantConnect.Time.OneDay; }
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var source = Path.Combine("..", "..", "..", "Tests", "TestData", "daily-stock-picker-backtest.csv");
                return new SubscriptionDataSource(source);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                if (string.IsNullOrWhiteSpace(line) || !char.IsDigit(line[0]))
                {
                    return null;
                }

                var stocks = new StockDataSource { Symbol = config.Symbol };

                try
                {
                    var csv = line.ToCsv();
                    stocks.Time = DateTime.ParseExact(csv[0], "yyyyMMdd", null);
                    stocks.Symbols.AddRange(csv[1..]);
                }
                catch (FormatException)
                {
                    return null;
                }

                return stocks;
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
        public long DataPoints => 8767;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 298;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

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
            {"Information Ratio", "-3.9"},
            {"Tracking Error", "0.045"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
