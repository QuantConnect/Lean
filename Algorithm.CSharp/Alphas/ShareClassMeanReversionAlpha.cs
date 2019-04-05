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
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// A number of companies publicly trade two different classes of shares
    /// in US equity markets. If both assets trade with reasonable volume, then
    /// the underlying driving forces of each should be similar or the same. Given
    /// this, we can create a relatively dollar-netural long/short portfolio using
    /// the dual share classes. Theoretically, any deviation of this portfolio from
    /// its mean-value should be corrected, and so the motivating idea is based on
    /// mean-reversion. Using a Simple Moving Average indicator, we can
    /// compare the value of this portfolio against its SMA and generate insights
    /// to buy the under-valued symbol and sell the over-valued symbol.
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha.
    /// </summary>
    public class ShareClassMeanReversionAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            SetWarmUp(20);

            // Setup Universe settings and tickers to be used
            var symbols = new[] { "VIA", "VIAB" }
                .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA));

            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

            // Use ShareClassMeanReversionAlphaModel to establish insights
            SetAlpha(new ShareClassMeanReversionAlphaModel(symbols));

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        private class ShareClassMeanReversionAlphaModel : AlphaModel
        {
            private const double _insightMagnitude = 0.001;
            private readonly Symbol _longSymbol;
            private readonly Symbol _shortSymbol;
            private readonly TimeSpan _insightPeriod;
            private readonly SimpleMovingAverage _sma;
            private readonly RollingWindow<decimal> _positionWindow;
            private decimal _alpha;
            private decimal _beta;
            private bool _invested;

            public ShareClassMeanReversionAlphaModel(
                IEnumerable<Symbol> symbols,
                Resolution resolution = Resolution.Minute)
            {
                if (symbols.Count() != 2)
                {
                    throw new ArgumentException("ShareClassMeanReversionAlphaModel: symbols parameter must contain 2 elements");
                }
                _longSymbol = symbols.ToArray()[0];
                _shortSymbol = symbols.ToArray()[1];
                _insightPeriod = resolution.ToTimeSpan().Multiply(5);
                _sma = new SimpleMovingAverage(2);
                _positionWindow = new RollingWindow<decimal>(2);
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                // Check to see if either ticker will return a NoneBar, and skip the data slice if so
                if (data.Bars.Count < 2)
                {
                    return Enumerable.Empty<Insight>();
                }

                // If Alpha and Beta haven't been calculated yet, then do so
                if (_alpha == 0 || _beta == 0)
                {
                    CalculateAlphaBeta(algorithm);
                }

                // Update indicator and Rolling Window for each data slice passed into Update() method
                if (!UpdateIndicators(data))
                {
                    return Enumerable.Empty<Insight>();
                }

                // Check to see if the portfolio is invested. If no, then perform value comparisons and emit insights accordingly
                if (!_invested)
                {
                    //Reset invested boolean
                    _invested = true;

                    if (_positionWindow[0] > _sma)
                    {
                        return Insight.Group(new[]
                        {
                            Insight.Price(_longSymbol, _insightPeriod, InsightDirection.Down, _insightMagnitude),
                            Insight.Price(_shortSymbol, _insightPeriod, InsightDirection.Up, _insightMagnitude),
                        });
                    }
                    else
                    {
                        return Insight.Group(new[]
                    {
                            Insight.Price(_longSymbol, _insightPeriod, InsightDirection.Up, _insightMagnitude),
                            Insight.Price(_shortSymbol, _insightPeriod, InsightDirection.Down, _insightMagnitude),
                        });
                    }
                }
                // If the portfolio is invested and crossed back over the SMA, then emit flat insights
                else if (_invested && CrossedMean())
                {
                    _invested = false;
                }

                return Enumerable.Empty<Insight>();
            }

            /// <summary>
            /// Calculate Alpha and Beta, the initial number of shares for each security needed to achieve a 50/50 weighting
            /// </summary>
            /// <param name="algorithm"></param>
            private void CalculateAlphaBeta(QCAlgorithm algorithm)
            {
                _alpha = algorithm.CalculateOrderQuantity(_longSymbol, 0.5);
                _beta = algorithm.CalculateOrderQuantity(_shortSymbol, 0.5);
                algorithm.Log($"{algorithm.Time} :: Alpha: {_alpha} Beta: {_beta}");
            }

            /// <summary>
            /// Calculate position value and update the SMA indicator and Rolling Window
            /// </summary>
            private bool UpdateIndicators(Slice data)
            {
                var positionValue = (_alpha * data[_longSymbol].Close) - (_beta * data[_shortSymbol].Close);
                _sma.Update(data[_longSymbol].EndTime, positionValue);
                _positionWindow.Add(positionValue);
                return _sma.IsReady && _positionWindow.IsReady;
            }

            /// <summary>
            /// Check to see if the position value has crossed the SMA and then return a boolean value
            /// </summary>
            /// <returns></returns>
            private bool CrossedMean()
            {
                return (_positionWindow[0] >= _sma && _positionWindow[1] < _sma)
                    || (_positionWindow[1] >= _sma && _positionWindow[0] < _sma);
            }
        }
    }
}