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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing issue #5160 where delisting order would be cancelled because it was placed at the market close on the delisting day
    /// </summary>
    public class DelistingFutureOptionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual Resolution Resolution => Resolution.Minute;
        private bool _traded;
        private int _lastMonth;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 3);
            SetEndDate(2020, 3, 23);
            SetCash(10000000);

            var future = AddFuture(Futures.Indices.SP500EMini, Resolution, Market.CME);
            future.SetFilter(1, 120);

            AddFutureOption(future.Symbol, universe => universe.Strikes(-2, 2));
            _lastMonth = -1;

            // This is required to prevent the algorithm from automatically delisting the underlying. Without this, future options will be subscribed
            // with resolution default to Minute insted of this.Resolution. This could be replaced after GH issue #6491 is implemented.
            UniverseSettings.Resolution = Resolution;
        }

        public override void OnData(Slice slice)
        {
            if (Time.Month != _lastMonth)
            {
                _lastMonth = Time.Month;
                var investedSymbols = Securities.Values
                    .Where(security => security.Invested)
                    .Select(security => security.Symbol)
                    .ToList();

                var delistedSecurity = investedSymbols.Where(symbol => symbol.ID.Date.AddDays(1) < Time).ToList();
                if (delistedSecurity.Count > 0)
                {
                    throw new RegressionTestException($"[{UtcTime}] We hold a delisted securities: {string.Join(",", delistedSecurity)}");
                }
                Log($"Holdings({Time}): {string.Join(",", investedSymbols)}");
            }

            if (Portfolio.Invested)
            {
                return;
            }

            foreach (var chain in slice.OptionChains.Values)
            {
                foreach (var contractsValue in chain.Contracts.Values)
                {
                    MarketOrder(contractsValue.Symbol, 1);
                    _traded = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_traded)
            {
                throw new RegressionTestException("We expected some FOP trading to happen");
            }
            if (Portfolio.Invested)
            {
                throw new RegressionTestException("We shouldn't be invested anymore");
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
        public virtual long DataPoints => 462641;

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
            {"Total Orders", "17"},
            {"Average Win", "0.04%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "-1.280%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-0.791"},
            {"Start Equity", "10000000"},
            {"End Equity", "9971576.14"},
            {"Net Profit", "-0.284%"},
            {"Sharpe Ratio", "-5.765"},
            {"Sortino Ratio", "-0.931"},
            {"Probabilistic Sharpe Ratio", "0.062%"},
            {"Loss Rate", "89%"},
            {"Win Rate", "11%"},
            {"Profit-Loss Ratio", "0.88"},
            {"Alpha", "-0.027"},
            {"Beta", "0.002"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.495"},
            {"Tracking Error", "0.429"},
            {"Treynor Ratio", "-15.266"},
            {"Total Fees", "$11.36"},
            {"Estimated Strategy Capacity", "$65000000.00"},
            {"Lowest Capacity Asset", "ES XCZJLDQX2SRO|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "0.16%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "224292a1aace8a0895ea272d84714eac"}
        };
    }
}
