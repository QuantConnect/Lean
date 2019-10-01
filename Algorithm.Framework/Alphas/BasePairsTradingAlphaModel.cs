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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using static System.FormattableString;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// This alpha model is designed to accept every possible pair combination
    /// from securities selected by the universe selection model
    /// This model generates alternating long ratio/short ratio insights emitted as a group
    /// </summary>
    public class BasePairsTradingAlphaModel : AlphaModel
    {
        private readonly int _lookback;
        private readonly Resolution _resolution;
        private readonly TimeSpan _predictionInterval;
        private readonly decimal _threshold;
        private readonly Dictionary<Tuple<Symbol, Symbol>, PairData> _pairs;

        /// <summary>
        /// List of security objects present in the universe
        /// </summary>
        public HashSet<Security> Securities { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePairsTradingAlphaModel"/> class
        /// </summary>
        /// <param name="lookback">Lookback period of the analysis</param>
        /// <param name="resolution">Analysis resolution</param>
        /// <param name="threshold">The percent [0, 100] deviation of the ratio from the mean before emitting an insight</param>
        public BasePairsTradingAlphaModel(
            int lookback = 1,
            Resolution resolution = Resolution.Daily,
            decimal threshold = 1m
            )
        {
            _lookback = lookback;
            _resolution = resolution;
            _threshold = threshold;
            _predictionInterval = _resolution.ToTimeSpan().Multiply(_lookback);
            _pairs = new Dictionary<Tuple<Symbol, Symbol>, PairData>();

            Securities = new HashSet<Security>();
            Name = Invariant($"{nameof(BasePairsTradingAlphaModel)}({_lookback},{_resolution},{_threshold.Normalize()})");
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();

            foreach (var kvp in _pairs)
            {
                insights.AddRange(kvp.Value.GetInsightGroup());
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            NotifiedSecurityChanges.UpdateCollection(Securities, changes);

            UpdatePairs(algorithm);

            // Remove pairs that has assets that were removed from the universe
            foreach (var security in changes.RemovedSecurities)
            {
                var symbol = security.Symbol;
                var keys = _pairs.Keys.Where(k => k.Item1 == symbol || k.Item2 == symbol).ToList();

                foreach (var key in keys)
                {
                    _pairs.Remove(key);
                }
            }
        }

        /// <summary>
        /// Check whether the assets pass a pairs trading test
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="asset1">The first asset's symbol in the pair</param>
        /// <param name="asset2">The second asset's symbol in the pair</param>
        /// <returns>True if the statistical test for the pair is successful</returns>
        public virtual bool HasPassedTest(QCAlgorithm algorithm, Symbol asset1, Symbol asset2) => true;

        private void UpdatePairs(QCAlgorithm algorithm)
        {
            var assets = Securities.Select(x => x.Symbol).ToArray();

            for (var i = 0; i < assets.Length; i++)
            {
                var assetI = assets[i];

                for (var j = i + 1; j < assets.Length; j++)
                {
                    var assetJ = assets[j];

                    var pairSymbol = Tuple.Create(assetI, assetJ);
                    var invert = Tuple.Create(assetJ, assetI);

                    if (_pairs.ContainsKey(pairSymbol) || _pairs.ContainsKey(invert))
                    {
                        continue;
                    }

                    if (!HasPassedTest(algorithm, assetI, assetJ))
                    {
                        continue;
                    }

                    var pairData = new PairData(algorithm, assetI, assetJ, _predictionInterval, _threshold);
                    _pairs.Add(pairSymbol, pairData);
                }
            }
        }

        private class PairData
        {
            private enum State
            {
                ShortRatio,
                FlatRatio,
                LongRatio
            };

            private State _state = State.FlatRatio;

            private readonly Symbol _asset1;
            private readonly Symbol _asset2;

            private readonly IndicatorBase<IndicatorDataPoint> _asset1Price;
            private readonly IndicatorBase<IndicatorDataPoint> _asset2Price;
            private readonly IndicatorBase<IndicatorDataPoint> _ratio;
            private readonly IndicatorBase<IndicatorDataPoint> _mean;
            private readonly IndicatorBase<IndicatorDataPoint> _upperThreshold;
            private readonly IndicatorBase<IndicatorDataPoint> _lowerThreshold;
            private readonly TimeSpan _predictionInterval;

            /// <summary>
            /// Create a new pair
            /// </summary>
            /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
            /// <param name="asset1">The first asset's symbol in the pair</param>
            /// <param name="asset2">The second asset's symbol in the pair</param>
            /// <param name="period">Period over which this insight is expected to come to fruition</param>
            /// <param name="threshold">The percent [0, 100] deviation of the ratio from the mean before emitting an insight</param>
            public PairData(QCAlgorithm algorithm, Symbol asset1, Symbol asset2, TimeSpan period, decimal threshold)
            {
                _asset1 = asset1;
                _asset2 = asset2;

                _asset1Price = algorithm.Identity(asset1);
                _asset2Price = algorithm.Identity(asset2);

                _ratio = _asset1Price.Over(_asset2Price);
                _mean = new ExponentialMovingAverage(500).Of(_ratio);

                var upper = new ConstantIndicator<IndicatorDataPoint>("ct", 1 + threshold / 100m);
                _upperThreshold = _mean.Times(upper, "UpperThreshold");

                var lower = new ConstantIndicator<IndicatorDataPoint>("ct", 1 - threshold / 100m);
                _lowerThreshold = _mean.Times(lower, "LowerThreshold");

                _predictionInterval = period;
            }

            /// <summary>
            /// Gets the insights group for the pair
            /// </summary>
            /// <returns>Insights grouped by an unique group id</returns>
            public IEnumerable<Insight> GetInsightGroup()
            {
                if (!_mean.IsReady)
                {
                    return Enumerable.Empty<Insight>();
                }

                // don't re-emit the same direction
                if (_state != State.LongRatio && _ratio > _upperThreshold)
                {
                    _state = State.LongRatio;

                    // asset1/asset2 is more than 2 std away from mean, short asset1, long asset2
                    var shortAsset1 = Insight.Price(_asset1, _predictionInterval, InsightDirection.Down);
                    var longAsset2 = Insight.Price(_asset2, _predictionInterval, InsightDirection.Up);

                    // creates a group id and set the GroupId property on each insight object
                    return Insight.Group(shortAsset1, longAsset2);
                }

                // don't re-emit the same direction
                if (_state != State.ShortRatio && _ratio < _lowerThreshold)
                {
                    _state = State.ShortRatio;

                    // asset1/asset2 is less than 2 std away from mean, long asset1, short asset2
                    var longAsset1 = Insight.Price(_asset1, _predictionInterval, InsightDirection.Up);
                    var shortAsset2 = Insight.Price(_asset2, _predictionInterval, InsightDirection.Down);

                    // creates a group id and set the GroupId property on each insight object
                    return Insight.Group(longAsset1, shortAsset2);
                }

                return Enumerable.Empty<Insight>();
            }
        }
    }
}