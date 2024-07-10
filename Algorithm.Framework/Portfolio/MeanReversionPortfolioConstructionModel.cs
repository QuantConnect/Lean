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
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Scheduling;
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
        private int _numOfAssets;
        private double[] _weightVector;
        private decimal _reversionThreshold;
        private int _windowSize;
        private Resolution _resolution;
        private Dictionary<Symbol, MeanReversionSymbolData> _symbolData = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time
        /// in UTC</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="reversionThreshold">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(IDateRule rebalancingDateRules,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            decimal reversionThreshold = 1, 
            int windowSize = 20, 
            Resolution resolution = Resolution.Daily)
            : this(rebalancingDateRules.ToFunc(), portfolioBias, reversionThreshold, windowSize, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalanceResolution">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="reversionThreshold">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(Resolution rebalanceResolution = Resolution.Daily,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            decimal reversionThreshold = 1, 
            int windowSize = 20, 
            Resolution resolution = Resolution.Daily)
            : this(rebalanceResolution.ToTimeSpan(), portfolioBias, reversionThreshold, windowSize, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="reversionThreshold">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(TimeSpan timeSpan,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            decimal reversionThreshold = 1, 
            int windowSize = 20, 
            Resolution resolution = Resolution.Daily)
            : this(dt => dt.Add(timeSpan), portfolioBias, reversionThreshold, windowSize, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalance">Rebalancing func or if a date rule, timedelta will be converted into func.
        /// For a given algorithm UTC DateTime the func returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="reversionThreshold">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(PyObject rebalance,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            decimal reversionThreshold = 1, 
            int windowSize = 20, 
            Resolution resolution = Resolution.Daily)
            : this((Func<DateTime, DateTime?>)null, portfolioBias, reversionThreshold, windowSize, resolution)
        {
            SetRebalancingFunc(rebalance);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored.</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="reversionThreshold">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            decimal reversionThreshold = 1, 
            int windowSize = 20, 
            Resolution resolution = Resolution.Daily)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null,
                   portfolioBias, reversionThreshold, windowSize, resolution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanReversionPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance.</param>
        /// <param name="portfolioBias">Specifies the bias of the portfolio (Short, Long/Short, Long)</param>
        /// <param name="reversionThreshold">Reversion threshold</param>
        /// <param name="windowSize">Window size of mean price</param>
        /// <param name="resolution">The resolution of the history price and rebalancing</param>
        public MeanReversionPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc,
            PortfolioBias portfolioBias = PortfolioBias.LongShort,
            decimal reversionThreshold = 1, 
            int windowSize = 20, 
            Resolution resolution = Resolution.Daily)
            : base(rebalancingFunc)
        {
            if (portfolioBias == PortfolioBias.Short)
            {
                throw new ArgumentException("Long position must be allowed in MeanReversionPortfolioConstructionModel.");
            }
            
            _reversionThreshold = reversionThreshold;
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

            var numOfAssets = activeInsights.Count;
            if (_numOfAssets != numOfAssets)
            {
                _numOfAssets = numOfAssets;
                // Initialize price vector and portfolio weightings vector
                _weightVector = Enumerable.Repeat((double) 1/_numOfAssets, _numOfAssets).ToArray();
            }

            // Get price relatives vs expected price (SMA)
            var priceRelatives = GetPriceRelatives(activeInsights);     // \tilde{x}_{t+1}

            // Get step size of next portfolio
            // \bar{x}_{t+1} = 1^T * \tilde{x}_{t+1} / m
            // \lambda_{t+1} = max( 0, ( b_t * \tilde{x}_{t+1} - \epsilon ) / ||\tilde{x}_{t+1}  - \bar{x}_{t+1} * 1|| ^ 2 )
            var nextPrediction = priceRelatives.Average();      // \bar{x}_{t+1}
            var assetsMeanDev = priceRelatives.Select(x => x - nextPrediction).ToArray();
            var secondNorm = Math.Pow(assetsMeanDev.Euclidean(), 2);
            double stepSize;        // \lambda_{t+1}
            
            if (secondNorm == 0d)
            {
                stepSize = 0d;
            }
            else
            {
                stepSize = (_weightVector.InnerProduct(priceRelatives) - (double)_reversionThreshold) / secondNorm;
                stepSize = Math.Max(0d, stepSize);
            }

            // Get next portfolio weightings
            // b_{t+1} = b_t - step_size * ( \tilde{x}_{t+1}  - \bar{x}_{t+1} * 1 )
            var nextPortfolio = _weightVector.Select((x, i) => x - assetsMeanDev[i] * stepSize);
            // Normalize
            var normalizedPortfolioWeightVector = SimplexProjection(nextPortfolio);
            // Save normalized result for the next portfolio step
            _weightVector = normalizedPortfolioWeightVector;

            // Update portfolio state
            for (int i = 0; i < _numOfAssets; i++)
            {
                targets.Add(activeInsights[i], normalizedPortfolioWeightVector[i]);
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
            var numOfInsights = activeInsights.Count;

            // Initialize a price vector of the next prices relatives' projection
            var nextPriceRelatives = new double[numOfInsights];

            for (int i = 0; i < numOfInsights; i++)
            {
                var insight = activeInsights[i];
                var symbolData = _symbolData[insight.Symbol];

                nextPriceRelatives[i] = insight.Magnitude != null ?
                            1 + (double)insight.Magnitude * (int)insight.Direction:
                            (double)symbolData.Identity.Current.Value / (double)symbolData.Sma.Current.Value;
            }

            return nextPriceRelatives;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            base.OnSecuritiesChanged(algorithm, changes);

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
                    _symbolData.Add(symbol, new MeanReversionSymbolData(algorithm, symbol, _windowSize, _resolution));
                }
            }
        }

        /// <summary>
        /// Cumulative Sum of a given sequence
        /// </summary>
        /// <param name="sequence">sequence to obtain cumulative sum</param>
        /// <return>cumulative sum</return>
        public static IEnumerable<double> CumulativeSum(IEnumerable<double> sequence)
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
        /// <param name="vector">unnormalized weight vector</param>
        /// <param name="total">regulator, default to be 1, making it a probabilistic simplex</param>
        /// <return>normalized weight vector</return>
        public static double[] SimplexProjection(IEnumerable<double> vector, double total = 1)
        {
            if (total <= 0)
            {
                throw new ArgumentException("Total must be > 0 for Euclidean Projection onto the Simplex.");
            }

            // Sort v into u in descending order
            var mu = vector.OrderByDescending(x => x).ToArray();
            var sv = CumulativeSum(mu).ToArray();

            var rho = Enumerable.Range(0, vector.Count()).Where(i => mu[i] > (sv[i] - total) / (i+1)).Last();
            var theta = (sv[rho] - total) / (rho + 1);
            var w = vector.Select(x => Math.Max(x - theta, 0d)).ToArray();
            return w;
        }

        private class MeanReversionSymbolData
        {
            public Identity Identity;
            public SimpleMovingAverage Sma;

            public MeanReversionSymbolData(QCAlgorithm algo, Symbol symbol, int windowSize, Resolution resolution)
            {
                // Indicator of price
                Identity = algo.Identity(symbol, resolution);
                // Moving average indicator for mean reversion level
                Sma = algo.SMA(symbol, windowSize, resolution);

                // Warmup indicator
                algo.WarmUpIndicator(symbol, Identity, resolution);
                algo.WarmUpIndicator(symbol, Sma, resolution);
            }

            public void Reset()
            {
                Identity.Reset();
                Sma.Reset();
            }
            
            public bool IsReady()
            {
                return (Identity.IsReady & Sma.IsReady);
            }
        }
    }
}