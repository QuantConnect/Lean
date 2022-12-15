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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm for trading continuous future
    /// </summary>
    public class BasicTemplateFutureRolloverAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Symbol _symbol, _oldSymbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetCash(1000000);
            SetStartDate(2019, 2, 1);
            SetEndDate(2021, 6, 1);

            // Requesting data
            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 0
            );
            _symbol = _continuousContract.Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            // Accessing data
            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                if (changedEvent.Symbol == _symbol)
                {
                    _oldSymbol = changedEvent.OldSymbol;
                    Log($"Symbol changed at {Time}: {_oldSymbol} -> {changedEvent.NewSymbol}");
                }
            }

            var mappedSymbol = _continuousContract.Mapped;

            if (!(slice.Bars.ContainsKey(_symbol) && mappedSymbol != null))
            {
                return;
            }

            // Rolling over: to liquidate any position of the old mapped contract and switch to the newly mapped contract
            if (_oldSymbol != null)
            {
                Liquidate(_oldSymbol);
                MarketOrder(mappedSymbol, 1);
                _oldSymbol = null;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 68645;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 340;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "17"},
            {"Average Win", "1.41%"},
            {"Average Loss", "-4.03%"},
            {"Compounding Annual Return", "3.001%"},
            {"Drawdown", "5.600%"},
            {"Expectancy", "0.182"},
            {"Net Profit", "7.144%"},
            {"Sharpe Ratio", "0.737"},
            {"Probabilistic Sharpe Ratio", "31.420%"},
            {"Loss Rate", "12%"},
            {"Win Rate", "88%"},
            {"Profit-Loss Ratio", "0.35"},
            {"Alpha", "-0.003"},
            {"Beta", "0.138"},
            {"Annual Standard Deviation", "0.029"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-0.907"},
            {"Tracking Error", "0.171"},
            {"Treynor Ratio", "0.152"},
            {"Total Fees", "$36.55"},
            {"Estimated Strategy Capacity", "$58000000000.00"},
            {"Lowest Capacity Asset", "ES XPFJZVPGHL35"},
            {"Fitness Score", "0.002"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0.962"},
            {"Return Over Maximum Drawdown", "0.53"},
            {"Portfolio Turnover", "0.003"},
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
            {"OrderListHash", "8f92e1528c6477a156449fd1e86527e7"}
        };
    }
}
