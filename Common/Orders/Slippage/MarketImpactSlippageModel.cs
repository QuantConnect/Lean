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
    /// Direct estimation of equity market impact. Risk, 18(7), 58-62.
    /// Available from: https://www.ram-ai.com/sites/default/files/2022-06/costestim.pdf</remark>
    /// <remark>The default parameters are calibrated around 2 decades ago,
    /// the trading time effect is not accounted (volume near market open/close is larger),
    /// the market regime is not taken into account,
    /// and the market environment does not have many market makers at that time,
    /// so it is recommend to recalibrate with reference to the original paper.</remark>
    public class MarketImpactSlippageModel : ISlippageModel
    {
        private readonly IAlgorithm _algorithm;
        private readonly double _alpha;
        private readonly double _beta;
        private readonly double _gamma;
        private readonly double _eta;
        private readonly double _delta;
        private readonly bool _nonNegOnly;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataPerSymbol = new();
        private readonly Random _random;

        /// <summary>
        /// Instantiate a new instance of MarketImpactSlippageModel
        /// </summary>
        /// <param name="algorithm">IAlgorithm instance</param>
        /// <param name="alpha">Exponent of the permanent impact function</param>
        /// <param name="beta">Exponent of the temporary impact function</param>
        /// <param name="gamma">Coefficient of the permanent impact function</param>
        /// <param name="eta">Coefficient of the temporary impact function</param>
        /// <param name="delta">Liquidity scaling factor for permanent impact</param>
        /// <param name="nonNegOnly">Indicator whether only non-negative slippage allowed</param>
        /// <param name="randomSeed">Random seed for generating gaussian noise</param>
        public MarketImpactSlippageModel(IAlgorithm algorithm, double alpha = 0.891d, double beta = 0.600d,
                                         double gamma = 0.314d, double eta = 0.142d, double delta = 0.267d,
                                         bool nonNegOnly = true, int randomSeed = 50)
        {
            _algorithm = algorithm;
            _alpha = alpha;
            _beta = beta;
            _gamma = gamma;
            _eta = eta;
            _delta = delta;
            _nonNegOnly = nonNegOnly;
            _random = new(randomSeed);
        }

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            if (!_symbolDataPerSymbol.TryGetValue(asset.Symbol, out var symbolData))
            {
                symbolData = new SymbolData(_algorithm, asset.Symbol);
                _symbolDataPerSymbol.Add(asset.Symbol, symbolData);
            }

            if (symbolData.AvgVolume == 0d)
            {
                return 0m;
            }

            // time taken for execution, we add 700ms to mimic time slippage of filling (convert to by trading day)
            var time = (_algorithm.UtcTime - order.CreatedTime.AddMilliseconds(700)).TotalDays * 24d / 6.5d;
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
            var permImpact = symbolData.Sigma * time * G(nu) * liquidityAdjustment + SampleGaussian() * noise;
            // permanent market impact
            var tempImpact = symbolData.Sigma * H(nu) + SampleGaussian() * noise;
            // realized market impact
            var realizedImpact = tempImpact + permImpact * 0.5d;

            // estimate the slippage by temporary impact
            return SlippageFromImpactEstimation(realizedImpact);
        }

        /// <summary>
        /// The permanent market impact function
        /// </summary>
        /// <param name="absOrderQuantity">The absolute, normalized order quantity</param>
        /// <return>Unadjusted permanent market impact factor</return>
        private double G(double absOrderQuantity)
        {
            return _gamma * Math.Pow(absOrderQuantity, _alpha);
        }

        /// <summary>
        /// The temporary market impact function
        /// </summary>
        /// <param name="absOrderQuantity">The absolute, normalized order quantity</param>
        /// <return>Unadjusted temporary market impact factor</return>
        private double H(double absOrderQuantity)
        {
            return _eta * Math.Pow(absOrderQuantity, _beta);
        }

        /// <summary>
        /// Estimate the slippage size from impact
        /// </summary>
        /// <param name="impact">The market impact of the order</param>
        /// <return>Slippage estimation</return>
        private decimal SlippageFromImpactEstimation(double impact)
        {
            // The percentage of impact that an order is averagely being affected is random from 0.0 to 1.0
            var ultimateSlippage = (impact * _random.NextDouble()).SafeDecimalCast();

            if (_nonNegOnly)
            {
                return Math.Max(0m, ultimateSlippage);
            }

            return ultimateSlippage;
        }

        private double SampleGaussian(double mean = 0d, double stddev = 1d)
        {
            var x1 = 1 - _random.NextDouble();
            var x2 = 1 - _random.NextDouble();

            var y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return y1 * stddev + mean;
        }
    }

    internal class SymbolData
    {
        private readonly IAlgorithm _algorithm;
        private readonly Symbol _symbol;
        private readonly TradeBarConsolidator _consolidator;
        private readonly RollingWindow<decimal> _volumes = new(10);
        private readonly RollingWindow<decimal> _prices = new(252);

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
