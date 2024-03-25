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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test illustrating how history from custom data sources can be requested.
    /// </summary>
    public class HistoryWithCustomDataSourceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 6);

            _aapl = AddData<CustomData>("AAPL", Resolution.Minute).Symbol;
            _spy = AddData<CustomData>("SPY", Resolution.Minute).Symbol;
        }

        public override void OnEndOfAlgorithm()
        {
            // We remove the symbol from history data in order to compare values only
            Func<CustomData, Object> getRawCustomData = data => new {
                Time = data.Time,
                Value = data.Value,
                Close = data.Close,
                Open = data.Open,
                High = data.High,
                Low = data.Low,
                Volume = data.Volume,
            };

            var aaplHistory = History<CustomData>("AAPL", StartDate, EndDate, Resolution.Minute).Select(getRawCustomData).ToList();
            var spyHistory = History<CustomData>("SPY", StartDate, EndDate, Resolution.Minute).Select(getRawCustomData).ToList();

            if (aaplHistory.Count == 0 || spyHistory.Count == 0)
            {
                throw new Exception("At least one of the history results is empty");
            }

            // Check that both results contain the same data, since CustomData fetches APPL data regardless of the symbol
            if (!aaplHistory.SequenceEqual(spyHistory))
            {
                throw new Exception("Histories are not equal");
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
        public long DataPoints => 2960;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 2938;

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

        /// <summary>
        /// Custom data source for the regression test algorithm, which returns AAPL equity data regardless of the symbol requested.
        /// </summary>
        public class CustomData : BaseData
        {
            public decimal Open;
            public decimal High;
            public decimal Low;
            public decimal Close;
            public decimal Volume;

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new TradeBar().GetSource(
                    new SubscriptionDataConfig(
                        config,
                        typeof(CustomData),
                        // Create a new symbol as equity so we find the existing data files
                        // Symbol.Create(config.MappedSymbol, SecurityType.Equity, config.Market)),
                        Symbol.Create("AAPL", SecurityType.Equity, config.Market)),
                    date,
                    isLiveMode);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var tradeBar = TradeBar.ParseEquity(config, line, date);

                return new CustomData {
                    Symbol = config.Symbol,
                    Time = tradeBar.Time,
                    Value = tradeBar.Value,
                    Close = tradeBar.Close,
                    Open = tradeBar.Open,
                    High = tradeBar.High,
                    Low = tradeBar.Low,
                    Volume = tradeBar.Volume,
                };
            }
        }
    }
}
