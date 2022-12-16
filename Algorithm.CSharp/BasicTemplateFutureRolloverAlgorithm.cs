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
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm for trading continuous future
    /// </summary>
    public class BasicTemplateFutureRolloverAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Future, SymbolData> _symbolData = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2014, 10, 10);
            SetCash(1000000);

            var futures = new List<string> {
                Futures.Indices.SP500EMini
            };

            foreach (var future in futures)
            {
                // Requesting data
                var continuousContract = AddFuture(future,
                    resolution: Resolution.Daily,
                    extendedMarketHours: true,
                    dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                    dataMappingMode: DataMappingMode.OpenInterest,
                    contractDepthOffset: 0
                );

                var symbolData = new SymbolData(this, continuousContract);
                _symbolData.Add(continuousContract, symbolData);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            foreach (var kvp in _symbolData)
            {
                var symbol = kvp.Key.Symbol;
                
                if (slice.SymbolChangedEvents.TryGetValue(symbol, out var changedEvent))
                {
                    var oldSymbol = changedEvent.OldSymbol;
                    var newSymbol = changedEvent.NewSymbol;
                    var tag = $"Rollover - Symbol changed at {Time}: {oldSymbol} -> {newSymbol}";
                    var quantity = Portfolio[oldSymbol].Quantity;

                    // Rolling over: to liquidate any position of the old mapped contract and switch to the newly mapped contract
                    Liquidate(oldSymbol, tag: tag);
                    MarketOrder(newSymbol, quantity, tag: tag);

                    kvp.Value.Reset();
                }

                var mappedSymbol = kvp.Key.Mapped;
                var ema = kvp.Value.EMA;

                if (mappedSymbol != null && slice.Bars.ContainsKey(symbol) && ema.IsReady)
                {
                    if (ema.Current.Value < slice.Bars[symbol].Price && !Portfolio[mappedSymbol].IsLong)
                    {
                        MarketOrder(mappedSymbol, 1);
                    }
                    else if (ema.Current.Value > slice.Bars[symbol].Price && !Portfolio[mappedSymbol].IsShort)
                    {
                        MarketOrder(mappedSymbol, -1);
                    }
                }
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
        public long DataPoints => 5044;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 35;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "6"},
            {"Average Win", "0.23%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.682%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.687%"},
            {"Sharpe Ratio", "1.048"},
            {"Probabilistic Sharpe Ratio", "55.116%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.005"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.307"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "7.068"},
            {"Total Fees", "$12.90"},
            {"Estimated Strategy Capacity", "$580000000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Fitness Score", "0.001"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "7.656"},
            {"Return Over Maximum Drawdown", "53.089"},
            {"Portfolio Turnover", "0.001"},
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
            {"OrderListHash", "c77e88c0f30ae1b07fd635292b4de1c9"}
        };

        public class SymbolData
        {
            private QCAlgorithm _algorithm;
            private Symbol _symbol;
            public ExponentialMovingAverage EMA;

            public SymbolData(QCAlgorithm algorithm, Future future)
            {
                _algorithm = algorithm;
                _symbol = future.Symbol;
                EMA = algorithm.EMA(future.Symbol, 20, Resolution.Daily);
            }

            public void Reset()
            {
                EMA.Reset();
                _algorithm.WarmUpIndicator(_symbol, EMA, Resolution.Daily);
            }
        }
    }  
}
