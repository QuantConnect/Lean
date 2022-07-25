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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which reproduces GH issue 4446
    /// </summary>
    public class DelistedFutureLiquidateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contractSymbol;
        private Symbol _futureSymbol;
        protected virtual Resolution Resolution => Resolution.Minute;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 12, 30);

            var futureSP500 = AddFuture(Futures.Indices.SP500EMini, Resolution, extendedMarketHours: true);
            futureSP500.SetFilter(0, 182);
            _futureSymbol = futureSP500.Symbol;
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (_contractSymbol == null)
            {
                foreach (var chain in slice.FutureChains)
                {
                    var contract = chain.Value.OrderBy(x => x.Expiry).FirstOrDefault();
                    // if found, trade it
                    if (contract != null)
                    {
                        _contractSymbol = contract.Symbol;
                       if (Securities[_contractSymbol].Exchange.ExchangeOpen)
                        {
                            MarketOrder(_contractSymbol, 1);
                        }
                        else
                        {
                            // Very high limit price so the order gets filled with the next bar
                            LimitOrder(_contractSymbol, 1, Securities[_contractSymbol].Price * 2m);
                        }
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{_contractSymbol}: {Securities[_contractSymbol].Invested}");
            if (Securities[_contractSymbol].Invested)
            {
                throw new Exception($"Position should be closed when {_contractSymbol} got delisted {_contractSymbol.ID.Date}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"{orderEvent}. Delisting on: {_contractSymbol.ID.Date}");
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
        public virtual long DataPoints => 1224778;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "1.53%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "6.814%"},
            {"Drawdown", "1.300%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.529%"},
            {"Sharpe Ratio", "2.36"},
            {"Probabilistic Sharpe Ratio", "94.555%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.006"},
            {"Beta", "0.159"},
            {"Annual Standard Deviation", "0.03"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-4.439"},
            {"Tracking Error", "0.075"},
            {"Treynor Ratio", "0.441"},
            {"Total Fees", "$1.85"},
            {"Estimated Strategy Capacity", "$1600000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Fitness Score", "0.018"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "1.031"},
            {"Return Over Maximum Drawdown", "7.886"},
            {"Portfolio Turnover", "0.023"},
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
            {"OrderListHash", "0fda2500fb7139b45026da07aa9866ad"}
        };
    }
}
