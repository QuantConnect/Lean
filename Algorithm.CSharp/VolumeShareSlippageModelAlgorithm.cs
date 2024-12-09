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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm implementing VolumeShareSlippageModel.
    /// </summary>
    public class VolumeShareSlippageModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _longs = new();
        private List<Symbol> _shorts = new();

        public override void Initialize()
        {
            SetStartDate(2020, 11, 29);
            SetEndDate(2020, 12, 2);
            // To set the slippage model to limit to fill only 30% volume of the historical volume, with 5% slippage impact.
            SetSecurityInitializer((security) => security.SetSlippageModel(new VolumeShareSlippageModel(0.3m, 0.05m)));

            // Create SPY symbol to explore its constituents.
            var spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            UniverseSettings.Resolution = Resolution.Daily;
            // Add universe to trade on the most and least weighted stocks among SPY constituents.
            AddUniverse(Universe.ETF(spy, universeFilterFunc: Selection));
        }

        private IEnumerable<Symbol> Selection(IEnumerable<ETFConstituentUniverse> constituents)
        {
            var sortedByDollarVolume = constituents.OrderBy(x => x.Weight).ToList();
            // Add the 10 most weighted stocks to the universe to long later.
            _longs = sortedByDollarVolume.TakeLast(10)
                .Select(x => x.Symbol)
                .ToList();
            // Add the 10 least weighted stocks to the universe to short later.
            _shorts = sortedByDollarVolume.Take(10)
                .Select(x => x.Symbol)
                .ToList();

            return _longs.Union(_shorts);
        }

        public override void OnData(Slice slice)
        {
            // Equally invest into the selected stocks to evenly dissipate capital risk.
            // Dollar neutral of long and short stocks to eliminate systematic risk, only capitalize the popularity gap.
            var targets = _longs.Select(symbol => new PortfolioTarget(symbol, 0.05m)).ToList();
            targets.AddRange(_shorts.Select(symbol => new PortfolioTarget(symbol, -0.05m)).ToList());

            // Liquidate the ones not being the most and least popularity stocks to release fund for higher expected return trades.
            SetHoldings(targets, liquidateExistingHoldings: true);
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
        public long DataPoints => 1035;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "20.900%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100190.84"},
            {"Net Profit", "0.191%"},
            {"Sharpe Ratio", "9.794"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.297"},
            {"Beta", "-0.064"},
            {"Annual Standard Deviation", "0.017"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-18.213"},
            {"Tracking Error", "0.099"},
            {"Treynor Ratio", "-2.695"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$4400000000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "4.22%"},
            {"OrderListHash", "9d2bd0df7c094c393e77f72b7739bfa0"}
        };
    }
}
