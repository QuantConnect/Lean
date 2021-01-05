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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// Reversal strategy that goes long when price crosses below SMA and Short when price crosses above SMA.
    /// The trading strategy is implemented only between 10AM - 3PM (NY time). Research suggests this is due to
    /// institutional trades during market hours which need hedging with the USD. Source paper:
    /// LeBaron, Zhao: Intraday Foreign Exchange Reversals
    /// http://people.brandeis.edu/~blebaron/wps/fxnyc.pdf
    /// http://www.fma.org/Reno/Papers/ForeignExchangeReversalsinNewYorkTime.pdf
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
    ///</summary>
    public class IntradayReversalCurrencyMarketsAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2015, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Select resolution
            var resolution = Resolution.Hour;

            // Reversion on the USD.
            var symbols = new[] { QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda) };

            // Set requested data resolution
            UniverseSettings.Resolution = resolution;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

            // Use IntradayReversalAlphaModel to establish insights
            SetAlpha(new IntradayReversalAlphaModel(5, resolution));

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// Alpha model that uses a Price/SMA Crossover to create insights on Hourly Frequency.
        /// Frequency: Hourly data with 5-hour simple moving average.
        /// Strategy:
        /// Reversal strategy that goes Long when price crosses below SMA and Short when price crosses above SMA.
        /// The trading strategy is implemented only between 10AM - 3PM (NY time)
        /// </summary>
        private class IntradayReversalAlphaModel : AlphaModel
        {
            private readonly int _periodSma;
            private readonly Resolution _resolution;
            private readonly Dictionary<Symbol, SymbolData> _cache;

            public IntradayReversalAlphaModel(
                int periodSma = 5,
                Resolution resolution = Resolution.Hour)
            {
                _periodSma = periodSma;
                _resolution = resolution;
                _cache = new Dictionary<Symbol, SymbolData>();
                Name = "IntradayReversalAlphaModel";
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                // Set the time to close all positions at 3PM
                var timeToClose = algorithm.Time.Date.Add(new TimeSpan(0, 15, 1, 0));

                var insights = new List<Insight>();

                foreach (var kvp in algorithm.ActiveSecurities)
                {
                    var symbol = kvp.Key;

                    SymbolData symbolData;

                    if (ShouldEmitInsight(algorithm, symbol) &&
                        _cache.TryGetValue(symbol, out symbolData))
                    {
                        var price = kvp.Value.Price;

                        var direction = symbolData.IsUptrend(price)
                            ? InsightDirection.Up
                            : InsightDirection.Down;

                        // Ignore signal for same direction as previous signal (when no crossover)
                        if (direction == symbolData.PreviousDirection)
                        {
                            continue;
                        }

                        // Save the current Insight Direction to check when the crossover happens
                        symbolData.PreviousDirection = direction;

                        // Generate insight
                        insights.Add(Insight.Price(symbol, timeToClose, direction));
                    }
                }

                return insights;
            }

            private bool ShouldEmitInsight(QCAlgorithm algorithm, Symbol symbol)
            {
                var timeOfDay = algorithm.Time.TimeOfDay;

                return algorithm.Securities[symbol].HasData &&
                    timeOfDay >= TimeSpan.FromHours(10) &&
                    timeOfDay <= TimeSpan.FromHours(15);
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                foreach (var symbol in changes.AddedSecurities.Select(x => x.Symbol))
                {
                    if (_cache.ContainsKey(symbol)) continue;
                    _cache.Add(symbol, new SymbolData(algorithm, symbol, _periodSma, _resolution));
                }
            }

            /// <summary>
            /// Contains data specific to a symbol required by this model
            /// </summary>
            private class SymbolData
            {
                private readonly SimpleMovingAverage _priceSMA;

                public InsightDirection PreviousDirection { get; set; }

                public SymbolData(QCAlgorithm algorithm, Symbol symbol, int periodSma, Resolution resolution)
                {
                    PreviousDirection = InsightDirection.Flat;
                    _priceSMA = algorithm.SMA(symbol, periodSma, resolution);
                }

                public bool IsUptrend(decimal price)
                {
                    return _priceSMA.IsReady && price < Math.Round(_priceSMA * 1.001m, 6);
                }
            }
        }
    }
}