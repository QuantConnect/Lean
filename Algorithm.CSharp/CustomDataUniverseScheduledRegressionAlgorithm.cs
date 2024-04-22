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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Custom data universe selection regression algorithm asserting it's behavior. Similar to CustomDataUniverseRegressionAlgorithm but with a custom schedule
    /// </summary>
    public class CustomDataUniverseScheduledRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Queue<DateTime> _selectionTime = new(new[] {
            new DateTime(2014, 03, 25, 0, 0, 0),
            new DateTime(2014, 03, 27, 0, 0, 0),
            new DateTime(2014, 03, 29, 0, 0, 0)
        });

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 03, 31);

            UniverseSettings.Resolution = Resolution.Daily;
            UniverseSettings.Schedule.On(DateRules.On(_selectionTime.ToArray()));
            AddUniverse<CoarseFundamental>("custom-data-universe", UniverseSettings, (coarse) =>
            {
                Debug($"Universe selection called: {Time} Count: {coarse.Count()}");

                var expectedTime = _selectionTime.Dequeue();
                if (expectedTime != Time)
                {
                    throw new Exception($"Unexpected selection time {Time} expected {expectedTime}");
                }
                return coarse.OfType<CoarseFundamental>().OrderByDescending(x => x.DollarVolume)
                    .SelectMany(x => new[] {
                        x.Symbol,
                        QuantConnect.Symbol.CreateBase(typeof(CustomData), x.Symbol)})
                    .Take(20);
            });

            // This use case is also valid/same because it will use the algorithm settings by default
            // AddUniverse<CoarseFundamental>("custom-data-universe", (coarse) => {});
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                var customData = data.Get<CustomData>();
                var symbols = data.Keys.Where(symbol => symbol.SecurityType != SecurityType.Base).ToList();
                foreach (var symbol in symbols)
                {
                    SetHoldings(symbol, 1m / symbols.Count);

                    if (!customData.Any(custom => custom.Key.Underlying == symbol))
                    {
                        throw new Exception($"Custom data was not found for underlying symbol {symbol}");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionTime.Count != 0)
            {
                throw new Exception($"Unexpected selection times, missing {_selectionTime.Count}");
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
        public long DataPoints => 21382;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "7"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-65.130%"},
            {"Drawdown", "2.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "97717.31"},
            {"Net Profit", "-2.283%"},
            {"Sharpe Ratio", "-4.298"},
            {"Sortino Ratio", "-4.067"},
            {"Probabilistic Sharpe Ratio", "5.388%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-1.062"},
            {"Beta", "1.336"},
            {"Annual Standard Deviation", "0.132"},
            {"Annual Variance", "0.018"},
            {"Information Ratio", "-12.03"},
            {"Tracking Error", "0.078"},
            {"Treynor Ratio", "-0.426"},
            {"Total Fees", "$13.87"},
            {"Estimated Strategy Capacity", "$430000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "12.54%"},
            {"OrderListHash", "fae1a7c34d640dfa020330f24378bcf7"}
        };
    }
}
