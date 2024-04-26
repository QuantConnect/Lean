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
            
            SetAlpha(new ConstituentWeightedRsiAlphaModel());
            SetPortfolioConstruction(new InsightWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            var spy = AddEquity("SPY", Resolution.Hour).Symbol;
            
            // We load hourly data for ETF constituents in this algorithm
            UniverseSettings.Resolution = Resolution.Hour;
            Settings.MinimumOrderMarginPortfolioPercentage = 0.01m;

            AddUniverse(Universe.ETF(spy, UniverseSettings, FilterETFConstituents));
        }

        /// <summary>
        /// Filters ETF constituents and adds the resulting Symbols to the ETF constituent universe
        /// </summary>
        /// <param name="constituents">ETF constituents, i.e. the components of the ETF and their weighting</param>
        /// <returns>Symbols to add to universe</returns>
        public IEnumerable<Symbol> FilterETFConstituents(IEnumerable<ETFConstituentUniverse> constituents)
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
            private Dictionary<Symbol, SymbolData> _rsiSymbolData = new Dictionary<Symbol, SymbolData>();

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
                    .Where(x => algorithm.Securities[x].Cache.HasData(typeof(ETFConstituentUniverse)))
                    .Select(x => algorithm.Securities[x].Cache.GetData<ETFConstituentUniverse>())
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
            public ETFConstituentUniverse Constituent { get; }
            
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
            public SymbolData(Symbol symbol, QCAlgorithm algorithm, ETFConstituentUniverse constituent, int period)
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2722;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "55"},
            {"Average Win", "0.09%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "3.321%"},
            {"Drawdown", "0.500%"},
            {"Expectancy", "0.047"},
            {"Start Equity", "100000"},
            {"End Equity", "100535.45"},
            {"Net Profit", "0.535%"},
            {"Sharpe Ratio", "1.377"},
            {"Sortino Ratio", "1.963"},
            {"Probabilistic Sharpe Ratio", "60.081%"},
            {"Loss Rate", "63%"},
            {"Win Rate", "37%"},
            {"Profit-Loss Ratio", "1.83"},
            {"Alpha", "0.022"},
            {"Beta", "-0.024"},
            {"Annual Standard Deviation", "0.015"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.46"},
            {"Tracking Error", "0.109"},
            {"Treynor Ratio", "-0.878"},
            {"Total Fees", "$55.00"},
            {"Estimated Strategy Capacity", "$440000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "11.16%"},
            {"OrderListHash", "d41a5ba07ca662d7bfbeace8a05b34aa"}
        };
    }
}
