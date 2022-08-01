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
using Accord.Math;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Implementation of On-Line Moving Average Reversion (OLMAR)
    /// </summary>
    /// <remarks>Li, B., Hoi, S. C. (2012). On-line portfolio selection with moving average reversion. arXiv preprint arXiv:1206.4626.
    /// Available at https://arxiv.org/ftp/arxiv/papers/1206/1206.4626.pdf</remarks>
    /// <remarks>Using windowSize = 1 => Passive Aggressive Mean Reversion (PAMR) Portfolio</remarks>
    public class MeanReversionPortfolioConstructionModel : PortfolioConstructionModel
    {
        private int _m = 0;
        private double[] _b_t;
        private double _eps;
        private int _windowSize;
        private Resolution _resolution;
        private Dictionary<Symbol, SymbolData> _symbolData = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="eps">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(double eps = 1, int windowSize = 20, Resolution resolution = Resolution.Daily)
            : base()
        {
            _eps = eps;
            _resolution = resolution;
            _windowSize = windowSize;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">list of active insights</param>
        /// <return>dictionary of insight and respective target weight</return>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            var targets = new Dictionary<Insight, double>();

            // If we have no insights or non-ready just return an empty target list
            if (activeInsights.IsNullOrEmpty() || 
                !activeInsights.All(x => _symbolData[x.Symbol].IsReady()))
            {
                return targets;
            }

            var m = activeInsights.Count();
            if (_m != m)
            {
                _m = m;
                // Initialize price vector and portfolio weightings vector
                _b_t = Enumerable.Repeat((double) 1/_m, _m).ToArray();
            }

            // Get price relatives vs expected price (SMA)
            var xTilde = GetPriceRelatives(activeInsights);

            // Get step size of next portfolio
            // \bar{x}_{t+1} = 1^T * \tilde{x}_{t+1} / m
            // \lambda_{t+1} = max( 0, ( b_t * \tilde{x}_{t+1} - \epsilon ) / ||\tilde{x}_{t+1}  - \bar{x}_{t+1} * 1|| ^ 2 )
            var xBar = xTilde.Average();
            var assetsMeanDev = xTilde.Select(x => x - xBar).ToArray();
            var secondNorm = Math.Pow(assetsMeanDev.Euclidean(), 2);
            double stepSize;
            
            if (secondNorm == 0d)
            {
                stepSize = 0d;
            }
            else
            {
                stepSize = (_b_t.InnerProduct(xTilde) - _eps) / secondNorm;
                stepSize = Math.Max(0d, stepSize);
            }

            // Get next portfolio weightings
            // b_{t+1} = b_t - step_size * ( \tilde{x}_{t+1}  - \bar{x}_{t+1} * 1 )
            var b = _b_t.Select((x, i) => x - assetsMeanDev[i] * stepSize);
            // Normalize
            var bNorm = SimplexProjection(b);
            // Save normalized result for the next portfolio step
            _b_t = bNorm;

            // update portfolio state
            for (int i = 0; i < activeInsights.Count(); i++)
            {
                targets.Add(activeInsights[i], bNorm[i]);
            }

            return targets;
        }

        /// <summary>
        /// Get price relatives with reference level of SMA
        /// </summary>
        /// <param name="activeInsights">list of active insights</param>
        /// <return>array of price relatives vector</return>
        protected virtual double[] GetPriceRelatives(List<Insight> activeInsights)
        {
            // Initialize a price vector of the next prices relatives' projection
            var xTilde = new double[_m];

            for (int i = 0; i < _m; i++)
            {
                var insight = activeInsights[i];
                var symbolData = _symbolData[insight.Symbol];

                xTilde[i] = insight.Magnitude != null ?
                            1 + (double) insight.Magnitude :
                            (double)symbolData._identity.Current.Value / (double)symbolData._sma.Current.Value;
            }

            return xTilde;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                _symbolData.Remove(removed.Symbol, out var symbolData);
                symbolData.Reset();
            }

            // initialize data for added securities
            var symbols = changes.AddedSecurities.Select(x => x.Symbol);

            foreach(var symbol in symbols)
            {
                if (!_symbolData.ContainsKey(symbol))
                {
                    _symbolData.Add(symbol, new SymbolData(algorithm, symbol, _windowSize, _resolution));
                }
            }
        }

        /// <summary>
        /// Cumulative Sum of a given sequence
        /// </summary>
        /// <param name="sequence">sequence to obtain cumulative sum</param>
        /// <return>cumulative sum</return>
        private IEnumerable<double> CumulativeSum(IEnumerable<double> sequence)
        {
            double sum = 0;
            foreach(var item in sequence)
            {
                sum += item;
                yield return sum;
            }        
        }

        /// <summary>
        /// Normalize the updated portfolio into weight vector:
        /// v_{t+1} = arg min || v - v_{t+1} || ^ 2
        /// </summary>
        /// <remark>Duchi, J., Shalev-Shwartz, S., Singer, Y., and Chandra, T. (2008, July). 
        /// Efficient projections onto the l1-ball for learning in high dimensions.
        /// In Proceedings of the 25th international conference on Machine learning (pp. 272-279).</remark>
        /// <param name="v">unnormalized weight vector</param>
        /// <param name="b">total weight</param>
        /// <return>normalized weight vector</return>
        private double[] SimplexProjection(IEnumerable<double> v, double b = 1)
        {
            // Sort v into u in descending order
            var u = v.OrderByDescending(x => x).ToArray();
            var sv = CumulativeSum(u).ToArray();

            var rho = Enumerable.Range(0, v.Count()).Where(i => u[i] > (sv[i] - b) / (i+1)).Last();
            var theta = (sv[rho] - b) / (rho + 1);
            var w = v.Select(x => Math.Max(x - theta, 0d)).ToArray();
            return w;
        }

        private class SymbolData
        {
            public Identity _identity;
            public SimpleMovingAverage _sma;

            public SymbolData(QCAlgorithm algo, Symbol symbol, int windowSize, Resolution resolution)
            {
                // Indicator of price
                _identity = algo.Identity(symbol, resolution);
                // Moving average indicator for mean reversion level
                _sma = algo.SMA(symbol, windowSize);

                // Warmup indicator
                algo.WarmUpIndicator(symbol, _identity, resolution);
                algo.WarmUpIndicator(symbol, _sma, resolution);
            }

            public void Reset()
            {
                _identity.Reset();
                _sma.Reset();
            }
            
            public bool IsReady()
            {
                return (_identity.IsReady & _sma.IsReady);
            }
        }
    }
}