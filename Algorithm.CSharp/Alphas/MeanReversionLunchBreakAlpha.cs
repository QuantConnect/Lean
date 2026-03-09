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
    /// This alpha aims to capture the mean-reversion effect of ETFs during lunch-break by ranking 20 ETFs
    /// on their return between the close of the previous day to 12:00 the day after and predicting mean-reversion
    /// in price during lunch-break.
    ///
    /// Source:  Lunina, V. (June 2011). The Intraday Dynamics of Stock Returns and Trading Activity: Evidence from OMXS 30 (Master's Essay, Lund University).
    /// Retrieved from http://lup.lub.lu.se/luur/download?func=downloadFile&recordOId=1973850&fileOId=1973852
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
    ///</summary>
    public class MeanReversionLunchBreakAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Use Hourly Data For Simplicity
            UniverseSettings.Resolution = Resolution.Hour;
            SetUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelectionFunction));

            // Use MeanReversionLunchBreakAlphaModel to establish insights
            SetAlpha(new MeanReversionLunchBreakAlphaModel());

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// Sort the data by daily dollar volume and take the top '20' ETFs
        /// </summary>
        private IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            return (from cf in coarse
                    where !cf.HasFundamentalData
                    orderby cf.DollarVolume descending
                    select cf.Symbol).Take(20);
        }

        /// <summary>
        /// Uses the price return between the close of previous day to 12:00 the day after to
        /// predict mean-reversion of stock price during lunch break and creates direction prediction
        /// for insights accordingly.
        /// </summary>
        private class MeanReversionLunchBreakAlphaModel : AlphaModel
        {
            private const Resolution _resolution = Resolution.Hour;
            private readonly TimeSpan _predictionInterval;
            private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

            public MeanReversionLunchBreakAlphaModel(int lookback = 1)
            {
                _predictionInterval = _resolution.ToTimeSpan().Multiply(lookback);
                _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                foreach (var kvp in _symbolDataBySymbol)
                {
                    if (data.Bars.ContainsKey(kvp.Key))
                    {
                        var bar = data.Bars.GetValue(kvp.Key);
                        kvp.Value.Update(bar.EndTime, bar.Close);
                    }
                }

                return algorithm.Time.Hour == 12
                    ? _symbolDataBySymbol.Select(kvp => kvp.Value.Insight)
                    : Enumerable.Empty<Insight>();
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                foreach (var security in changes.RemovedSecurities)
                {
                    if (_symbolDataBySymbol.ContainsKey(security.Symbol))
                    {
                        _symbolDataBySymbol.Remove(security.Symbol);
                    }
                }

                // Retrieve price history for all securities in the security universe
                // and update the indicators in the SymbolData object
                var symbols = changes.AddedSecurities.Select(x => x.Symbol);
                var history = algorithm.History(symbols, 1, _resolution);
                if (symbols.Count() > 0 && history.Count() == 0)
                {
                    algorithm.Debug($"No data on {algorithm.Time}");
                }

                history.PushThrough(bar =>
                {
                    SymbolData symbolData;
                    if (!_symbolDataBySymbol.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData = new SymbolData(bar.Symbol, _predictionInterval);
                    }
                    symbolData.Update(bar.EndTime, bar.Price);
                    _symbolDataBySymbol[bar.Symbol] = symbolData;
                });
            }

            /// <summary>
            /// Contains data specific to a symbol required by this model
            /// </summary>
            private class SymbolData
            {
                // Mean value of returns for magnitude prediction
                private readonly SimpleMovingAverage _meanOfPriceChange = new RateOfChangePercent(1).SMA(3);
                // Price change from close price the previous day
                private readonly RateOfChangePercent _priceChange = new RateOfChangePercent(3);

                private readonly Symbol _symbol;
                private readonly TimeSpan _period;

                public Insight Insight
                {
                    get
                    {
                        // Emit "down" insight for the securities that increased in value and
                        // emit "up" insight for securities that have decreased in value
                        var direction = _priceChange > 0 ? InsightDirection.Down : InsightDirection.Up;
                        var magnitude = Convert.ToDouble(Math.Abs(_meanOfPriceChange));
                        return Insight.Price(_symbol, _period, direction, magnitude);
                    }
                }

                public SymbolData(Symbol symbol, TimeSpan period)
                {
                    _symbol = symbol;
                    _period = period;
                }

                public bool Update(DateTime time, decimal value)
                {
                    return _meanOfPriceChange.Update(time, value) &
                        _priceChange.Update(time, value);
                }
            }
        }
    }
}