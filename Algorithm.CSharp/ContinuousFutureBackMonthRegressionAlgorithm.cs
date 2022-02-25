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
                throw new Exception("Expected out of rage exception. We don't support that many back months");
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

                    var backMonthExpiration = changedEvent.Symbol.Underlying.ID.Date;
                    var frontMonthExpiration = FuturesExpiryFunctions.FuturesExpiryFunction(_continuousContract.Symbol)(Time.AddMonths(1));

                    if (backMonthExpiration <= frontMonthExpiration.Date)
                    {
                        throw new Exception($"Unexpected current mapped contract expiration {backMonthExpiration}" +
                            $" @ {Time} it should be AFTER front month expiration {frontMonthExpiration}");
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
                    // This works because we set this contract as tradable, even if it's a canonical security
                    Buy(_continuousContract.Symbol, 1);
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "1.16%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "2.229%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.118%"},
            {"Sharpe Ratio", "0.726"},
            {"Probabilistic Sharpe Ratio", "38.511%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.007"},
            {"Beta", "0.099"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.74"},
            {"Tracking Error", "0.076"},
            {"Treynor Ratio", "0.159"},
            {"Total Fees", "$5.55"},
            {"Estimated Strategy Capacity", "$290000.00"},
            {"Lowest Capacity Asset", "ES 1S1"},
            {"Fitness Score", "0.009"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0.498"},
            {"Return Over Maximum Drawdown", "1.803"},
            {"Portfolio Turnover", "0.014"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "e669103cc598f59d85f5e8d5f0b8df30"}
        };
    }
}
