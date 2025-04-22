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
            SetStartDate(2012, 1, 1);
            SetEndDate(2013, 1, 1);
            SetCash(10000000);

            var dc = AddFuture(Futures.Dairy.ClassIIIMilk, Resolution, Market.CME);
            dc.SetFilter(1, 120);

            AddFutureOption(dc.Symbol, universe => universe.Strikes(-2, 2));
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
        public virtual long DataPoints => 761073;

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
            {"Total Orders", "16"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-0.111%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-0.678"},
            {"Start Equity", "10000000"},
            {"End Equity", "9988880.24"},
            {"Net Profit", "-0.111%"},
            {"Sharpe Ratio", "-10.416"},
            {"Sortino Ratio", "-0.959"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "80%"},
            {"Win Rate", "20%"},
            {"Profit-Loss Ratio", "0.61"},
            {"Alpha", "-0.008"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.076"},
            {"Tracking Error", "0.107"},
            {"Treynor Ratio", "14.634"},
            {"Total Fees", "$19.76"},
            {"Estimated Strategy Capacity", "$1400000000.00"},
            {"Lowest Capacity Asset", "DC V5E8PHPRCHJ8|DC V5E8P9SH0U0X"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "6448bae646ab35724a0cd23936d94a48"}
        };
    }
}
