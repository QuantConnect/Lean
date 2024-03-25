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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Assert that custom data universe selection happens right away after algorithm starts
    /// </summary>
    public class CustomDataUniverseImmediateSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _selected;
        private bool _securitiesChanged;

        private bool _firstOnData = true;

        public override void Initialize()
        {
            SetStartDate(2017, 07, 04);
            SetEndDate(2018, 07, 04);

            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse<StockDataSource>("my-stock-data-source", stockDataSource =>
            {
                _selected = true;
                return stockDataSource.OfType<StockDataSource>().SelectMany(x => x.Symbols);
            });
        }

        public override void OnData(Slice data)
        {
            if (_firstOnData)
            {
                if (!_selected)
                {
                    throw new Exception("Universe selection should have been triggered right away. " +
                        "The first OnData call should have had happened after the universe selection");
                }

                _firstOnData = false;
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (!_selected)
            {
                throw new Exception("Universe selection should have been triggered right away");
            }

            if (!_securitiesChanged)
            {
                // Selection should be happening right on algorithm start
                if (Time != StartDate)
                {
                    throw new Exception("Universe selection should have been triggered right away");
                }

                if (changes.AddedSecurities.Count == 0)
                {
                    throw new Exception($"Expected multiple stocks to be added to the algorithm, " +
                        $"but found {changes.AddedSecurities.Count}");
                }

                _securitiesChanged = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_firstOnData || !_selected || !_securitiesChanged)
            {
                throw new Exception("Expected events didn't happen");
            }
        }

        /// <summary>
        /// Our custom data type that defines where to get and how to read our backtest and live data.
        /// </summary>
        class StockDataSource : BaseData
        {
            private const string Url = @"https://www.dropbox.com/s/ae1couew5ir3z9y/daily-stock-picker-backtest.csv?dl=1";

            public List<string> Symbols { get; set; }

            public StockDataSource()
            {
                Symbols = new List<string>();
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource(Url, SubscriptionTransportMedium.RemoteFile);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                try
                {
                    // create a new StockDataSource and set the symbol using config.Symbol
                    var stocks = new StockDataSource { Symbol = config.Symbol };
                    // break our line into csv pieces
                    var csv = line.ToCsv();
                    if (isLiveMode)
                    {
                        // our live mode format does not have a date in the first column, so use date parameter
                        stocks.Time = date;
                        stocks.Symbols.AddRange(csv);
                    }
                    else
                    {
                        // our backtest mode format has the first column as date, parse it
                        stocks.Time = DateTime.ParseExact(csv[0], "yyyyMMdd", null);
                        // any following comma separated values are symbols, save them off
                        stocks.Symbols.AddRange(csv.Skip(1));
                    }
                    return stocks;
                }
                // return null if we encounter any errors
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
        public long DataPoints => 3287;

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
            {"Information Ratio", "-0.97"},
            {"Tracking Error", "0.104"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
