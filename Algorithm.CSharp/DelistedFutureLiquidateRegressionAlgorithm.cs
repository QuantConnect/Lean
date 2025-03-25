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
        protected virtual Resolution Resolution => Resolution.Minute;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 12, 30);

            var futureSP500 = AddFuture(Futures.Indices.SP500EMini, Resolution);
            futureSP500.SetFilter(0, 182);
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
                        MarketOrder(_contractSymbol, 1);
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{_contractSymbol}: {Securities[_contractSymbol].Invested}");
            if (Securities[_contractSymbol].Invested)
            {
                throw new RegressionTestException($"Position should be closed when {_contractSymbol} got delisted {_contractSymbol.ID.Date}");
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
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 288140;

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
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "7.02%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "34.386%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "107016.6"},
            {"Net Profit", "7.017%"},
            {"Sharpe Ratio", "3.217"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "99.828%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.227"},
            {"Beta", "0.109"},
            {"Annual Standard Deviation", "0.084"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "-1.122"},
            {"Tracking Error", "0.112"},
            {"Treynor Ratio", "2.49"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$1700000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "2.01%"},
            {"OrderListHash", "de82efe4f019a5fa1fb79d111bf15811"}
        };
    }
}
