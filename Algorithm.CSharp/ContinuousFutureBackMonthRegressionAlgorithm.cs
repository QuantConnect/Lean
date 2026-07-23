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
    /// Continuous Futures Back Month #1 Regression algorithm. Asserting and showcasing the behavior of adding a continuous future
    /// </summary>
    public class ContinuousFutureBackMonthRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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

            try
            {
                AddFuture(Futures.Indices.SP500EMini,
                    dataNormalizationMode: DataNormalizationMode.BackwardsPanamaCanal,
                    dataMappingMode: DataMappingMode.OpenInterest,
                    contractDepthOffset: 5
                );
                throw new RegressionTestException("Expected out of rage exception. We don't support that many back months");
            }
            catch (ArgumentOutOfRangeException)
            {
                // expected
            }

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsPanamaCanal,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 1
            );
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (slice.Keys.Count != 1)
            {
                throw new RegressionTestException($"We are getting data for more than one symbols! {string.Join(",", slice.Keys.Select(symbol => symbol))}");
            }

            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                if (changedEvent.Symbol == _continuousContract.Symbol)
                {
                    _mappings.Add(changedEvent);
                    Log($"SymbolChanged event: {changedEvent}");

                    var backMonthExpiration = changedEvent.Symbol.Underlying.ID.Date;
                    var frontMonthExpiration = FuturesExpiryFunctions.FuturesExpiryFunction(_continuousContract.Symbol)(Time.AddMonths(1));

                    if (backMonthExpiration <= frontMonthExpiration.Date)
                    {
                        throw new RegressionTestException($"Unexpected current mapped contract expiration {backMonthExpiration}" +
                            $" @ {Time} it should be AFTER front month expiration {frontMonthExpiration}");
                    }

                    if (_continuousContract.Mapped != changedEvent.Symbol.Underlying)
                    {
                        throw new RegressionTestException($"Unexpected mapped continuous contract {_continuousContract.Mapped} expected {changedEvent.Symbol.Underlying}");
                    }
                }
            }

            if (_lastDateLog.Month != Time.Month && _continuousContract.HasData)
            {
                _lastDateLog = Time;

                Log($"{Time}- {Securities[_continuousContract.Symbol].GetLastData()}");
                if (_continuousContract.Exchange.ExchangeOpen)
                {
                    if (Portfolio.Invested)
                    {
                        Liquidate();
                    }
                    else
                    {
                        Buy(_continuousContract.Mapped, 1);
                }
                }

                if(Time.Month == 1 && Time.Year == 2013)
                {
                    var response = History(new[] { _continuousContract.Symbol }, 60 * 24 * 90);
                    if (!response.Any())
                    {
                        throw new RegressionTestException("Unexpected empty history response");
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
                throw new RegressionTestException($"Unexpected symbol changed events: {_mappings.Count}, was expecting {expectedMappingCounts}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 172698;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "6.43%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "14.898%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "107241.4"},
            {"Net Profit", "7.241%"},
            {"Sharpe Ratio", "1.511"},
            {"Sortino Ratio", "1.208"},
            {"Probabilistic Sharpe Ratio", "74.255%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.075"},
            {"Beta", "0.096"},
            {"Annual Standard Deviation", "0.063"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "-1.256"},
            {"Tracking Error", "0.097"},
            {"Treynor Ratio", "0.996"},
            {"Total Fees", "$6.45"},
            {"Estimated Strategy Capacity", "$240000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "1.37%"},
            {"Drawdown Recovery", "16"},
            {"OrderListHash", "c2eacc46e837feb8bf019c43f5c03f40"}
        };
    }
}
