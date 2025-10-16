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
    /// Custom data universe selection regression algorithm asserting it's behavior. See GH issue #6396
    /// </summary>
    public class CustomDataUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private HashSet<Symbol> _currentUnderlyingSymbols = new();
        private readonly Queue<DateTime> _selectionTime = new (new[] {
            new DateTime(2014, 03, 24, 0, 0, 0),
            new DateTime(2014, 03, 25, 0, 0, 0),
            new DateTime(2014, 03, 26, 0, 0, 0),
            new DateTime(2014, 03, 27, 0, 0, 0),
            new DateTime(2014, 03, 28, 0, 0, 0),
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
            AddUniverse<CoarseFundamental>("custom-data-universe", (coarse) =>
            {
                Debug($"Universe selection called: {Time} Count: {coarse.Count()}");

                var expectedTime = _selectionTime.Dequeue();
                if (expectedTime != Time)
                {
                    throw new RegressionTestException($"Unexpected selection time {Time} expected {expectedTime}");
                }
                return coarse.OfType<CoarseFundamental>().OrderByDescending(x => x.DollarVolume)
                    .SelectMany(x => new[] {
                        x.Symbol,
                        QuantConnect.Symbol.CreateBase(typeof(CustomData), x.Symbol)})
                    .Take(20);
            });
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                var customData = slice.Get<CustomData>();
                if (customData.Count > 0)
                {
                    foreach (var symbol in _currentUnderlyingSymbols.OrderBy(x => x.ID.Symbol))
                    {
                        if (!Securities[symbol].HasData)
                        {
                            continue;
                        }
                        SetHoldings(symbol, 1m / _currentUnderlyingSymbols.Count);

                        if (!customData.Any(custom => custom.Key.Underlying == symbol))
                        {
                            throw new RegressionTestException($"Custom data was not found for underlying symbol {symbol}");
                        }
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionTime.Count != 0)
            {
                throw new RegressionTestException($"Unexpected selection times, missing {_selectionTime.Count}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach(var security in changes.AddedSecurities.Where(sec => sec.Symbol.SecurityType != SecurityType.Base))
            {
                _currentUnderlyingSymbols.Add(security.Symbol);
            }
            foreach (var security in changes.RemovedSecurities.Where(sec => sec.Symbol.SecurityType != SecurityType.Base))
            {
                _currentUnderlyingSymbols.Remove(security.Symbol);
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
        public long DataPoints => 42625;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 76;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "7"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-52.130%"},
            {"Drawdown", "2.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98398.34"},
            {"Net Profit", "-1.602%"},
            {"Sharpe Ratio", "-4.839"},
            {"Sortino Ratio", "-4.57"},
            {"Probabilistic Sharpe Ratio", "5.163%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.79"},
            {"Beta", "0.93"},
            {"Annual Standard Deviation", "0.092"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "-15.913"},
            {"Tracking Error", "0.051"},
            {"Treynor Ratio", "-0.48"},
            {"Total Fees", "$11.14"},
            {"Estimated Strategy Capacity", "$700000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "8.76%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "96f806c3a7aafbfef523d98b33962469"}
        };
    }
}
