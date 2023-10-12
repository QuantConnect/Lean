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
using QuantConnect.Interfaces;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Indicators;
using MathNet.Numerics.Statistics;

namespace QuantConnect.Orders.Slippage
{
    /// <summary>
    /// Slippage model that mimic the effect brought by market impact,
    /// i.e. consume the volume listed in the order book
    /// </summary>
    /// <remark>Almgren, R., Thum, C., Hauptmann, E., & Li, H. (2005). 
    /// Direct estimation of equity market impact. Risk, 18(7), 58-62.</remark>
    /// <remark>The default parameters are calibrated around 2 decades ago,
    /// the trading time effect is not accounted (volume near market open/close is larger),
    /// the market regime is not taken into account,
    /// and the market environment does not have many market makers at that time,
    /// so it is recommend to recalibrate with reference to the original paper.</remark>
    public class MarketImpactSlippageModel : ISlippageModel
    {
        private IAlgorithm _algorithm;
        private readonly double _alpha;
        private readonly double _beta;
        private readonly double _gamma;
        private readonly double _eta;
        private readonly double _delta;
        private Dictionary<Symbol, SymbolData> _symbolDataPerSymbol = new();
        private Random _random = new(50);

        /// <summary>
        /// Instantiate a new instance of MarketImpactSlippageModel
        /// </summary>
        /// <param name="algorithm">IAlgorithm instance</param>
        /// <param name="alpha">exponent of the permanent impact function</param>
        /// <param name="beta">exponent of the temporary impact function</param>
        /// <param name="gamma">coefficient of the permanent impact function</param>
        /// <param name="eta">coefficient of the temporary impact functio</param>
        /// <param name="delta">the liquidity scaling factor for permanent impact</param>
        public MarketImpactSlippageModel(IAlgorithm algorithm, double alpha = 0.891d, double beta = 0.600d,
                                         double gamma = 0.314d, double eta = 0.142d, double delta = 0.267d)
        {
            _algorithm = algorithm;
            _alpha = alpha;
            _beta = beta;
            _gamma = gamma;
            _eta = eta;
            _delta = delta;
        }

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            if (!_symbolDataPerSymbol.TryGetValue(asset.Symbol, out var symbolData))
            {
                _symbolDataPerSymbol.Add(asset.Symbol, new SymbolData(_algorithm, asset.Symbol));
                symbolData = _symbolDataPerSymbol[asset.Symbol];
            }

            // time taken for execution, we add 700ms to mimic time slippage of filling (convert to by trading day)
            var time = ((TimeSpan)(_algorithm.UtcTime - order.CreatedTime + TimeSpan.FromMilliseconds(700))).TotalDays * 24d / 6.5d;
            // expected valid time for impact (+ half an hour)
            var timePost = time + 1d / 13d;
            // normalized volume of execution
            var nu = (double)order.AbsoluteQuantity / time / symbolData.AvgVolume;
            // liquidity adjustment for temporary market impact, if any
            var liquidityAdjustment = asset.Fundamentals != null ?
                                      Math.Pow(asset.Fundamentals.CompanyProfile.SharesOutstanding / symbolData.AvgVolume, _delta) :
                                      1d;
            // noise adjustment factor
            var noise = symbolData.Sigma * Math.Sqrt(timePost);

            // temporary market impact
            var permImpact = symbolData.Sigma * time * G(nu) * liquidityAdjustment + SampleGaussian(_random) * noise;
            // permanent market impact
            var tempImpact = symbolData.Sigma * H(nu) + SampleGaussian(_random) * noise + permImpact * 0.5d;
            // realized market impact
            var realizedImpact = tempImpact + permImpact;

            // estimate the slippage by temporary impact
            return SlippageFromImpactEstimation(realizedImpact);
        }

        /// <summary>
        /// The permanent market impact function
        /// </summary>
        /// <param name="absOrderQuantity">The absolute, normalized order quantity</param>
        /// <return>Unadjusted permanent market impact factor</return>
        protected double G(double absOrderQuantity)
        {
            return _gamma * Math.Pow((double)absOrderQuantity, _alpha);
        }

        /// <summary>
        /// The temporary market impact function
        /// </summary>
        /// <param name="absOrderQuantity">The absolute, normalized order quantity</param>
        /// <return>Unadjusted temporary market impact factor</return>
        protected double H(double absOrderQuantity)
        {
            return _eta * Math.Pow((double)absOrderQuantity, _beta);
        }

        /// <summary>
        /// The temporary market impact function
        /// </summary>
        /// <param name="tempImpact">The temporary market impact</param>
        /// <return>Slippage estimation</return>
        protected virtual decimal SlippageFromImpactEstimation(double tempImpact)
        {
            // We assume an exponential distribution on the order book
            // the order with high volume of "no slippage/impact" zone as 0, mid point of max slippage as infinity
            // we use lambda=2.5 as parameter, so the average slippage will be (1 - 1/2.5) of the impact
            return Convert.ToDecimal(tempImpact) * 0.6m;
        }

        private double SampleGaussian(Random random, double mean = 0d, double stddev = 1d)
        {
            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return y1 * stddev + mean;
        }
    }

    internal class SymbolData
    {
        private IAlgorithm _algorithm;
        private Symbol _symbol;
        private TradeBarConsolidator _consolidator;
        private RollingWindow<decimal> _volumes = new(10);
        private RollingWindow<decimal> _prices = new(252);

        public double Sigma { get; internal set; }

        public double AvgVolume { get; internal set; }

        public SymbolData(IAlgorithm algorithm, Symbol symbol)
        {
            _algorithm = algorithm;
            _symbol = symbol;

            _consolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += OnDataConsolidated;
            algorithm.SubscriptionManager.AddConsolidator(symbol, _consolidator);

            var historyRequest = new HistoryRequest(algorithm.UtcTime - TimeSpan.FromDays(370),
                                                    algorithm.UtcTime,
                                                    typeof(TradeBar),
                                                    symbol,
                                                    Resolution.Daily,
                                                    algorithm.Securities[symbol].Exchange.Hours,
                                                    algorithm.TimeZone,
                                                    Resolution.Daily,
                                                    false,
                                                    false,
                                                    DataNormalizationMode.Adjusted,
                                                    TickType.Trade);
            foreach (var bar in algorithm.HistoryProvider.GetHistory(new List<HistoryRequest> { historyRequest }, TimeZones.NewYork))
            {
                _consolidator.Update(bar.Bars[symbol]);
            }
        }

        public void OnDataConsolidated(object _, TradeBar bar)
        {
            _prices.Add(bar.Close);
            _volumes.Add(bar.Volume);

            if (_prices.Samples < 2)
            {
                return;
            }

            var rocp = new double[_prices.Samples - 1];
            for (var i = 0; i < _prices.Samples - 1; i++)
            {
                if (_prices[i + 1] == 0) continue;

                var roc = (_prices[i] - _prices[i + 1]) / _prices[i + 1];
                rocp[i] = (double)roc;
            }

            var variance = rocp.Variance();
            Sigma = Math.Sqrt(variance);
            AvgVolume = (double)_volumes.ToArray().Average();
        }

        public void Dispose()
        {
            _prices.Reset();
            _volumes.Reset();

            _consolidator.DataConsolidated -= OnDataConsolidated;
            _algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
        }
    }
}
