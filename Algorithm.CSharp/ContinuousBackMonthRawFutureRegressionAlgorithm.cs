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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Continuous Back Month Raw Futures Regression algorithm. Asserting and showcasing the behavior of adding a continuous future
    /// </summary>
    public class ContinuousBackMonthRawFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<SymbolChangedEvent> _mappings = new();
        private Future _continuousContract;
        private DateTime _lastDateLog;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 7, 1);
            SetEndDate(2014, 1, 1);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.Raw,
                dataMappingMode: DataMappingMode.FirstDayMonth,
                contractDepthOffset: 1
            );
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.Keys.Count != 1)
            {
                throw new Exception($"We are getting data for more than one symbols! {string.Join(",", data.Keys.Select(symbol => symbol))}");
            }

            foreach (var changedEvent in data.SymbolChangedEvents.Values)
            {
                if (changedEvent.Symbol == _continuousContract.Symbol)
                {
                    _mappings.Add(changedEvent);
                    Log($"SymbolChanged event: {changedEvent}");

                    var currentExpiration = changedEvent.Symbol.Underlying.ID.Date;
                    // +4 months cause we are actually using the back month, es is quarterly contract
                    var frontMonthExpiration = FuturesExpiryFunctions.FuturesExpiryFunction(_continuousContract.Symbol)(Time.AddMonths(1 + 4));

                    if (currentExpiration != frontMonthExpiration.Date)
                    {
                        throw new Exception($"Unexpected current mapped contract expiration {currentExpiration}" +
                            $" @ {Time} it should be AT front month expiration {frontMonthExpiration}");
                    }

                    if (_continuousContract.Mapped != changedEvent.Symbol.Underlying)
                    {
                        throw new Exception($"Unexpected mapped continuous contract {_continuousContract.Mapped} expected {changedEvent.Symbol.Underlying}");
                    }
                }
            }

            if (_lastDateLog.Month != Time.Month && _continuousContract.HasData)
            {
                _lastDateLog = Time;

                Log($"{Time}- {Securities[_continuousContract.Symbol].GetLastData()}");
                if (Portfolio.Invested)
                {
                    Liquidate();
                }
                else
                {
                    Buy(_continuousContract.Mapped, 1);
                }

                if(Time.Month == 1 && Time.Year == 2013)
                {
                    var response = History(new[] { _continuousContract.Symbol }, 60 * 24 * 90);
                    if (!response.Any())
                    {
                        throw new Exception("Unexpected empty history response");
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"{orderEvent}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var expectedMappingCounts = 2;
            if (_mappings.Count != expectedMappingCounts)
            {
                throw new Exception($"Unexpected symbol changed events: {_mappings.Count}, was expecting {expectedMappingCounts}");
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
        public long DataPoints => 712043;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "1.48%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "2.968%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101483.2"},
            {"Net Profit", "1.483%"},
            {"Sharpe Ratio", "0.521"},
            {"Sortino Ratio", "0.124"},
            {"Probabilistic Sharpe Ratio", "42.535%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.011"},
            {"Beta", "0.113"},
            {"Annual Standard Deviation", "0.026"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-2.674"},
            {"Tracking Error", "0.076"},
            {"Treynor Ratio", "0.117"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$76000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "0.91%"},
            {"OrderListHash", "7e45786e43b159c7edfcdf0aa0876deb"}
        };
    }
}
