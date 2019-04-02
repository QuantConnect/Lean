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

using Accord.Math;
using Accord.Statistics;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// Energy prices, especially Oil and Natural Gas, are in general fairly correlated,
    /// meaning they typically move in the same direction as an overall trend.This Alpha
    /// uses this idea and implements an Alpha Model that takes Natural Gas ETF price
    /// movements as a leading indicator for Crude Oil ETF price movements.We take the
    /// Natural Gas/Crude Oil ETF pair with the highest historical price correlation and
    /// then create insights for Crude Oil depending on whether or not the Natural Gas ETF price change
    /// is above/below a certain threshold that we set (arbitrarily).
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    /// sourced so the community and client funds can see an example of an alpha.
    ///</summary>
    public class GasAndCrudeOilEnergyCorrelationAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            Func<string, Symbol> ToSymbol = x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA);
            var naturalGas = new[] { "UNG", "BOIL", "FCG" }.Select(ToSymbol).ToArray();
            var crudeOil = new[] { "USO", "UCO", "DBO" }.Select(ToSymbol).ToArray();

            // Manually curated universe
            UniverseSettings.Resolution = Resolution.Minute;
            SetUniverseSelection(new ManualUniverseSelectionModel(naturalGas.Concat(crudeOil)));

            // Use PairsAlphaModel to establish insights
            SetAlpha(new PairsAlphaModel(naturalGas, crudeOil, 90, Resolution.Minute));

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Custom Execution Model
            SetExecution(new CustomExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// This Alpha model assumes that the ETF for natural gas is a good leading-indicator
        /// of the price of the crude oil ETF.The model will take in arguments for a threshold
        /// at which the model triggers an insight, the length of the look-back period for evaluating
        /// rate-of-change of UNG prices, and the duration of the insight
        /// </summary>
        private class PairsAlphaModel : AlphaModel
        {
            private readonly Symbol[] _leading;
            private readonly Symbol[] _following;
            private readonly int _historyDays;
            private readonly int _lookback;
            private readonly decimal _differenceTrigger = 0.75m;
            private readonly Resolution _resolution;
            private readonly TimeSpan _predictionInterval;
            private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;
            private Tuple<SymbolData, SymbolData> _pair;

            private DateTime _nextUpdate;

            public PairsAlphaModel(
                Symbol[] naturalGas,
                Symbol[] crudeOil,
                int historyDays = 90,
                Resolution resolution = Resolution.Hour,
                int lookback = 5,
                decimal differenceTrigger = 0.75m)
            {
                _leading = naturalGas;
                _following = crudeOil;
                _historyDays = historyDays;
                _resolution = resolution;
                _lookback = lookback;
                _differenceTrigger = differenceTrigger;
                _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
                _predictionInterval = resolution.ToTimeSpan().Multiply(lookback);
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                if (_nextUpdate == DateTime.MinValue || algorithm.Time > _nextUpdate)
                {
                    CorrelationPairsSelection();
                    _nextUpdate = algorithm.Time.AddDays(30);
                }

                var magnitude = (double)Math.Round(_pair.Item1.Return / 100, 6);

                if (_pair.Item1.Return > _differenceTrigger)
                {
                    yield return Insight.Price(_pair.Item2.Symbol, _predictionInterval, InsightDirection.Up, magnitude);
                }
                if (_pair.Item1.Return < -_differenceTrigger)
                {
                    yield return Insight.Price(_pair.Item2.Symbol, _predictionInterval, InsightDirection.Down, magnitude);
                }
            }

            public void CorrelationPairsSelection()
            {
                var maxCorrelation = -1.0;
                var matrix = new double[_historyDays, _following.Length + 1];

                // Get returns for each oil ETF
                for (var j = 0; j < _following.Length; j++)
                {
                    SymbolData symbolData2;
                    if (_symbolDataBySymbol.TryGetValue(_following[j], out symbolData2))
                    {
                        var dailyReturn2 = symbolData2.DailyReturnArray;
                        for (var i = 0; i < _historyDays; i++)
                        {
                            matrix[i, j + 1] = symbolData2.DailyReturnArray[i];
                        }
                    }
                }

                // Get returns for each natural gas ETF
                for (var j = 0; j < _leading.Length; j++)
                {
                    SymbolData symbolData1;
                    if (_symbolDataBySymbol.TryGetValue(_leading[j], out symbolData1))
                    {
                        for (var i = 0; i < _historyDays; i++)
                        {
                            matrix[i, 0] = symbolData1.DailyReturnArray[i];
                        }

                        var column = matrix.Correlation().GetColumn(0);
                        var correlation = column.RemoveAt(0).Max();

                        // Calculate the pair with highest historical correlation
                        if (correlation > maxCorrelation)
                        {
                            var maxIndex = column.IndexOf(correlation) - 1;
                            if (maxIndex < 0) continue;
                            var symbolData2 = _symbolDataBySymbol[_following[maxIndex]];
                            _pair = Tuple.Create(symbolData1, symbolData2);
                            maxCorrelation = correlation;
                        }
                    }
                }
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                foreach (var removed in changes.RemovedSecurities)
                {
                    if (_symbolDataBySymbol.ContainsKey(removed.Symbol))
                    {
                        _symbolDataBySymbol[removed.Symbol].RemoveConsolidators(algorithm);
                        _symbolDataBySymbol.Remove(removed.Symbol);
                    }
                }

                // Initialize data for added securities
                var symbols = changes.AddedSecurities.Select(x => x.Symbol);
                var dailyHistory = algorithm.History(symbols, _historyDays + 1, Resolution.Daily);
                if (symbols.Count() > 0 && dailyHistory.Count() == 0)
                {
                    algorithm.Debug($"{algorithm.Time} :: No daily data");
                }

                dailyHistory.PushThrough(bar =>
                {
                    SymbolData symbolData;
                    if (!_symbolDataBySymbol.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData = new SymbolData(algorithm, bar.Symbol, _historyDays, _lookback, _resolution);
                        _symbolDataBySymbol.Add(bar.Symbol, symbolData);
                    }
                    // Update daily rate of change indicator
                    symbolData.UpdateDailyRateOfChange(bar);
                });

                algorithm.History(symbols, _lookback, _resolution).PushThrough(bar =>
                {
                    // Update rate of change indicator with given resolution
                    if (_symbolDataBySymbol.ContainsKey(bar.Symbol))
                    {
                        _symbolDataBySymbol[bar.Symbol].UpdateRateOfChange(bar);
                    }
                });
            }

            /// <summary>
            /// Contains data specific to a symbol required by this model
            /// </summary>
            private class SymbolData
            {
                private readonly RateOfChangePercent _dailyReturn;
                private readonly IDataConsolidator _dailyConsolidator;
                private readonly RollingWindow<IndicatorDataPoint> _dailyReturnHistory;
                private readonly IDataConsolidator _consolidator;

                public Symbol Symbol { get; }

                public RateOfChangePercent Return { get; }

                public double[] DailyReturnArray => _dailyReturnHistory
                    .OrderBy(x => x.EndTime)
                    .Select(x => (double)x.Value).ToArray();

                public SymbolData(QCAlgorithm algorithm, Symbol symbol, int dailyLookback, int lookback, Resolution resolution)
                {
                    Symbol = symbol;

                    _dailyReturn = new RateOfChangePercent($"{symbol}.DailyROCP(1)", 1);
                    _dailyConsolidator = algorithm.ResolveConsolidator(symbol, Resolution.Daily);
                    _dailyReturnHistory = new RollingWindow<IndicatorDataPoint>(dailyLookback);
                    _dailyReturn.Updated += (s, e) => _dailyReturnHistory.Add(e);
                    algorithm.RegisterIndicator(symbol, _dailyReturn, _dailyConsolidator);

                    Return = new RateOfChangePercent($"{symbol}.ROCP({lookback})", lookback);
                    _consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                    algorithm.RegisterIndicator(symbol, Return, _consolidator);
                }

                public void RemoveConsolidators(QCAlgorithm algorithm)
                {
                    algorithm.SubscriptionManager.RemoveConsolidator(Symbol, _consolidator);
                    algorithm.SubscriptionManager.RemoveConsolidator(Symbol, _dailyConsolidator);
                }

                public void UpdateRateOfChange(BaseData data)
                {
                    Return.Update(data.EndTime, data.Value);
                }

                internal void UpdateDailyRateOfChange(BaseData data)
                {
                    _dailyReturn.Update(data.EndTime, data.Value);
                }

                public override string ToString() => Return.ToDetailedString();
            }
        }

        /// <summary>
        /// Provides an implementation of IExecutionModel that immediately submits market orders to achieve the desired portfolio targets
        /// </summary>
        private class CustomExecutionModel : ExecutionModel
        {
            private readonly PortfolioTargetCollection _targetsCollection = new PortfolioTargetCollection();
            private Symbol _previousSymbol;

            /// <summary>
            /// Immediately submits orders for the specified portfolio targets.
            /// </summary>
            /// <param name="algorithm">The algorithm instance</param>
            /// <param name="targets">The portfolio targets to be ordered</param>
            public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                _targetsCollection.AddRange(targets);

                foreach (var target in _targetsCollection.OrderByMarginImpact(algorithm))
                {
                    var openQuantity = algorithm.Transactions.GetOpenOrders(target.Symbol)
                        .Sum(x => x.Quantity);
                    var existing = algorithm.Securities[target.Symbol].Holdings.Quantity + openQuantity;
                    var quantity = target.Quantity - existing;

                    // Liquidate positions in Crude Oil ETF that is no longer part of the highest-correlation pair
                    if (_previousSymbol != null && target.Symbol != _previousSymbol)
                    {
                        algorithm.Liquidate(_previousSymbol);
                    }
                    if (quantity != 0)
                    {
                        algorithm.MarketOrder(target.Symbol, quantity);
                        _previousSymbol = target.Symbol;
                    }
                }
                _targetsCollection.ClearFulfilled(algorithm);
            }
        }
    }
}