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
        private bool _traded;
        private int _lastMonth;

        public override void Initialize()
        {
            SetStartDate(2012, 1, 1);
            SetEndDate(2013, 1, 1);
            SetCash(10000000);

            var dc = AddFuture(Futures.Dairy.ClassIIIMilk, Resolution.Minute, Market.CME);
            dc.SetFilter(1, 120);

            AddFutureOption(dc.Symbol, universe => universe.Strikes(-2, 2));
            _lastMonth = -1;
        }

        public override void OnData(Slice data)
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
                    throw new Exception($"[{UtcTime}] We hold a delisted securities: {string.Join(",", delistedSecurity)}");
                }
                Log($"Holdings({Time}): {string.Join(",", investedSymbols)}");
            }

            if (Portfolio.Invested)
            {
                return;
            }

            foreach (var chain in data.OptionChains.Values)
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
                throw new Exception("We expected some FOP trading to happen");
            }
            if (Portfolio.Invested)
            {
                throw new Exception("We shouldn't be invested anymore");
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
            {"Total Trades", "16"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-0.111%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-0.679"},
            {"Net Profit", "-0.112%"},
            {"Sharpe Ratio", "-1.052"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "80%"},
            {"Win Rate", "20%"},
            {"Profit-Loss Ratio", "0.61"},
            {"Alpha", "-0.001"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.187"},
            {"Tracking Error", "0.115"},
            {"Treynor Ratio", "1.545"},
            {"Total Fees", "$37.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.128"},
            {"Return Over Maximum Drawdown", "-0.995"},
            {"Portfolio Turnover", "0"},
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
            {"OrderListHash", "657651179"}
        };
    }
}
