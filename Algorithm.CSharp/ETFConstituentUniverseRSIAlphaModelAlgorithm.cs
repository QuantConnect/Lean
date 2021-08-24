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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm demonstrating the usage of the RSI indicator
    /// in combination with ETF constituents data to replicate the weighting
    /// of the ETF's assets in our own account.
    /// </summary>
    public class ETFConstituentUniverseRSIAlphaModelAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Store constituents data when doing universe selection for later use in the alpha model
        /// </summary>
        public List<ETFConstituentData> Constituents = new List<ETFConstituentData>();
        
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
            AddUniverse(Universe.ETF(spy, UniverseSettings, FilterETFConstituents));
        }

        /// <summary>
        /// Filters ETF constituents and adds the resulting Symbols to the ETF constituent universe
        /// </summary>
        /// <param name="constituents">ETF constituents, i.e. the components of the ETF and their weighting</param>
        /// <returns>Symbols to add to universe</returns>
        public IEnumerable<Symbol> FilterETFConstituents(IEnumerable<ETFConstituentData> constituents)
        {
            Constituents = constituents
                .Where(x => x.Weight != null && x.Weight >= 0.001m)
                .ToList();

            return Constituents
                .Select(x => x.Symbol)
                .ToList();
        }

        /// <summary>
        /// no-op
        /// </summary>
        public override void OnData(Slice data)
        {
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
                var algo = (ETFConstituentUniverseRSIAlphaModelAlgorithm) algorithm;
                if (algo.Constituents.Count == 0 || data.Bars.Count == 0)
                {
                    // Don't do anything if we have no data we can work with
                    yield break;
                }
                
                var constituents = algo.Constituents
                    .ToDictionary(x => x.Symbol, x => x);

                var allReady = true;
                foreach (var bar in data.Bars.Values)
                {
                    if (!constituents.ContainsKey(bar.Symbol))
                    {
                        // Dealing with a manually added equity, which in this case is SPY
                        continue;
                    }
                    
                    if (!_rsiSymbolData.TryGetValue(bar.Symbol, out var rsiData))
                    {
                        // First time we're initializing the RSI.
                        // It won't be ready now, but it will be
                        // after 7 data points.
                        var constituent = constituents[bar.Symbol];
                        rsiData = new SymbolData(bar.Symbol, constituent, 7);
                        _rsiSymbolData[bar.Symbol] = rsiData;
                    }

                    // Let's make sure all RSI indicators are ready before we emit any insights.
                    allReady = allReady && rsiData.Rsi.Update(new IndicatorDataPoint(bar.Symbol, bar.EndTime, bar.Close));
                }

                if (!allReady)
                {
                    // We're still warming up the RSI indicators.
                    yield break;
                }

                var emitted = false;
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

                    emitted = true;
                }

                if (emitted)
                {
                    // Prevents us from placing trades before the next
                    // ETF constituents universe selection occurs.
                    algo.Constituents.Clear();
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
            public SymbolData(Symbol symbol, ETFConstituentData constituent, int period)
            {
                Symbol = symbol;
                Constituent = constituent;
                Rsi = new RelativeStrengthIndex(period, MovingAverageType.Exponential);
            }
        }
    }
}
