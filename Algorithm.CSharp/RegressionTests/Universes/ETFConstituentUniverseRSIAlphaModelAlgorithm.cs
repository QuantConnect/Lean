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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm demonstrating the usage of the RSI indicator
    /// in combination with ETF constituents data to replicate the weighting
    /// of the ETF's assets in our own account.
    /// </summary>
    public class ETFConstituentUniverseRSIAlphaModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 12, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);
            
            SetAlpha(new ConstituentWeightedRsiAlphaModel(1));
            SetPortfolioConstruction(new InsightWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            var spy = AddEquity("SPY", Resolution.Hour).Symbol;
            
            // We load hourly data for ETF constituents in this algorithm
            UniverseSettings.Resolution = Resolution.Hour;
            AddUniverse(Universe.ETF(spy, UniverseSettings, FilterETFConstituents));
        }

        /// <summary>
        /// Filters ETF constituents and adds the resulting Symbols to the ETF constituent universe
        /// </summary>
        /// <param name="constituents">ETF constituents, i.e. the components of the ETF and their weighting</param>
        /// <returns>Symbols to add to universe</returns>
        public IEnumerable<Symbol> FilterETFConstituents(IEnumerable<ETFConstituentData> constituents)
        {
            return constituents
                .Where(x => x.Weight != null && x.Weight >= 0.001m)
                .Select(x => x.Symbol);
        }

        /// <summary>
        /// Alpha model making use of the RSI indicator and ETF constituent weighting to determine
        /// which assets we should invest in and the direction of investment
        /// </summary>
        private class ConstituentWeightedRsiAlphaModel : AlphaModel
        {
            private int? _maxTrades;
            private int _trades;
            
            private Dictionary<Symbol, SymbolData> _rsiSymbolData = new Dictionary<Symbol, SymbolData>();

            public ConstituentWeightedRsiAlphaModel(int? maxTrades = null)
            {
                _maxTrades = maxTrades;
            }
            
            /// <summary>
            /// Receives new data and emits new <see cref="Insight"/> instances
            /// </summary>
            /// <param name="algorithm">Algorithm</param>
            /// <param name="data">Current data</param>
            /// <returns>Enumerable of insights for assets to invest with a specific weight</returns>
            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                // Cast first, and then access the constituents collection defined in our algorithm.
                var algoConstituents = data.Bars.Keys
                    .Where(x => algorithm.Securities[x].Cache.HasData(typeof(ETFConstituentData)))
                    .Select(x => algorithm.Securities[x].Cache.GetData<ETFConstituentData>())
                    .ToList();
                
                if (algoConstituents.Count == 0 || data.Bars.Count == 0)
                {
                    // Don't do anything if we have no data we can work with
                    yield break;
                }
                
                var constituents = algoConstituents
                    .ToDictionary(x => x.Symbol, x => x);

                foreach (var bar in data.Bars.Values)
                {
                    if (!constituents.ContainsKey(bar.Symbol))
                    {
                        // Dealing with a manually added equity, which in this case is SPY
                        continue;
                    }
                    
                    if (!_rsiSymbolData.ContainsKey(bar.Symbol))
                    {
                        // First time we're initializing the RSI.
                        // It won't be ready now, but it will be
                        // after 7 data points.
                        var constituent = constituents[bar.Symbol];
                        _rsiSymbolData[bar.Symbol] = new SymbolData(bar.Symbol, algorithm, constituent, 7);
                    }
                }

                // Let's make sure all RSI indicators are ready before we emit any insights.
                var allReady = _rsiSymbolData.All(kvp => kvp.Value.Rsi.IsReady);
                if (!allReady)
                {
                    // We're still warming up the RSI indicators.
                    yield break;
                }

                if (_rsiSymbolData.Count != 0 && 
                    _maxTrades != null && 
                    _trades++ >= _maxTrades.Value)
                {
                    // We've exceeded the maximum amount of times we could trade according to the maximum
                    // set by us when we created this alpha model
                    yield break;
                }
                
                foreach (var kvp in _rsiSymbolData)
                {
                    var symbol = kvp.Key;
                    var symbolData = kvp.Value;
                    
                    var averageLoss = symbolData.Rsi.AverageLoss.Current.Value;
                    var averageGain = symbolData.Rsi.AverageGain.Current.Value;
                    
                    // If we've lost more than gained, then we think it's going to go down more
                    var direction = averageLoss > averageGain
                        ? InsightDirection.Down
                        : InsightDirection.Up;

                    // Set the weight of the insight as the weight of the ETF's
                    // holding. The InsightWeightingPortfolioConstructionModel
                    // will rebalance our portfolio to have the same percentage
                    // of holdings in our algorithm that the ETF has.
                    yield return Insight.Price(
                        symbol,
                        TimeSpan.FromDays(1),
                        direction,
                        (double)(direction == InsightDirection.Down
                            ? averageLoss
                            : averageGain),
                        weight: (double?) symbolData.Constituent.Weight);
                }
            }
        }

        /// <summary>
        /// Helper class to access ETF constituent data and RSI indicators
        /// for a single Symbol
        /// </summary>
        private class SymbolData
        {
            /// <summary>
            /// Symbol this data belongs to
            /// </summary>
            public Symbol Symbol { get; }
            
            /// <summary>
            /// Symbol's constituent data for the ETF it belongs to
            /// </summary>
            public ETFConstituentData Constituent { get; }
            
            /// <summary>
            /// RSI indicator for the Symbol's price data
            /// </summary>
            public RelativeStrengthIndex Rsi { get; }
            
            /// <summary>
            /// Creates a new instance of SymbolData
            /// </summary>
            /// <param name="symbol">The symbol to add data for</param>
            /// <param name="constituent">ETF constituent data</param>
            /// <param name="period">RSI period</param>
            public SymbolData(Symbol symbol, QCAlgorithm algorithm, ETFConstituentData constituent, int period)
            {
                Symbol = symbol;
                Constituent = constituent;
                Rsi = algorithm.RSI(symbol, period, MovingAverageType.Exponential, Resolution.Hour);
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "9"},
            {"Average Win", "0.00%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.064%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-0.331"},
            {"Net Profit", "-0.010%"},
            {"Sharpe Ratio", "-1.204"},
            {"Probabilistic Sharpe Ratio", "15.336%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.34"},
            {"Alpha", "-0.001"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.86"},
            {"Tracking Error", "0.129"},
            {"Treynor Ratio", "1.008"},
            {"Total Fees", "$9.00"},
            {"Estimated Strategy Capacity", "$120000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Fitness Score", "0.002"},
            {"Kelly Criterion Estimate", "54.21"},
            {"Kelly Criterion Probability Value", "0.251"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-2.425"},
            {"Portfolio Turnover", "0.003"},
            {"Total Insights Generated", "4"},
            {"Total Insights Closed", "4"},
            {"Total Insights Analysis Completed", "4"},
            {"Long Insight Count", "4"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$-37463.63"},
            {"Total Accumulated Estimated Alpha Value", "$-74771.15"},
            {"Mean Population Estimated Insight Value", "$-18692.79"},
            {"Mean Population Direction", "75%"},
            {"Mean Population Magnitude", "75%"},
            {"Rolling Averaged Population Direction", "75%"},
            {"Rolling Averaged Population Magnitude", "75%"},
            {"OrderListHash", "719522d3df1987e19b715af1fe65e1f6"}
        };
    }
}
