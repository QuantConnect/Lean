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
            SetStartDate(2013, 10, 8);
            SetEndDate(2014, 10, 10);

            // Requesting data
            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                resolution: Resolution.Daily,
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
        public long DataPoints => 4542;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0.06%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.057%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.057%"},
            {"Sharpe Ratio", "0.665"},
            {"Probabilistic Sharpe Ratio", "25.882%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.357"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "1.135"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$600000000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0.417"},
            {"Return Over Maximum Drawdown", "4.379"},
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
            {"OrderListHash", "960f2a002af87a34d20257aeb36ffbb5"}
        };
    }
}
