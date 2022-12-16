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
        private Dictionary<Future, ExponentialMovingAverage> _ema = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetCash(1000000);
            SetStartDate(2020, 2, 1);
            SetEndDate(2020, 4, 1);

            var futures = new List<string> {
                Futures.Indices.SP500EMini,
                Futures.Metals.Gold
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

                var ema = EMA(continuousContract.Symbol, 20, Resolution.Daily);
                _ema.Add(continuousContract, ema);
                Reset(continuousContract);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            foreach (var kvp in _ema)
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

                    Reset(kvp.Key);
                }

                var mappedSymbol = kvp.Key.Mapped;
                var ema = kvp.Value;

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

        public void Reset(Future future)
        {
            _ema[future].Reset();
            WarmUpIndicator(future.Symbol, _ema[future], Resolution.Daily);
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
        public long DataPoints => 2857;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "33"},
            {"Average Win", "2.33%"},
            {"Average Loss", "-0.35%"},
            {"Compounding Annual Return", "-0.644%"},
            {"Drawdown", "4.400%"},
            {"Expectancy", "-0.039"},
            {"Net Profit", "-0.109%"},
            {"Sharpe Ratio", "-0.011"},
            {"Probabilistic Sharpe Ratio", "31.092%"},
            {"Loss Rate", "88%"},
            {"Win Rate", "12%"},
            {"Profit-Loss Ratio", "6.69"},
            {"Alpha", "-0.081"},
            {"Beta", "-0.133"},
            {"Annual Standard Deviation", "0.086"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "0.958"},
            {"Tracking Error", "0.63"},
            {"Treynor Ratio", "0.007"},
            {"Total Fees", "$77.99"},
            {"Estimated Strategy Capacity", "$190000000.00"},
            {"Lowest Capacity Asset", "GC XE1Y0ZJ8NQ8T"},
            {"Fitness Score", "0.039"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.087"},
            {"Return Over Maximum Drawdown", "-0.144"},
            {"Portfolio Turnover", "0.081"},
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
