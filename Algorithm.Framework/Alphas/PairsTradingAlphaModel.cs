using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// This alpha model is designed to work against a single, predefined pair.
    /// This model generates alternating long ratio/short ratio insights emitted as a group
    /// </summary>
    public class PairsTradingAlphaModel : AlphaModel
    {
        private readonly TimeSpan _period;
        private readonly decimal _threshold;

        private readonly HashSet<Security> _securities;
        private readonly Dictionary<string, Pair> _pairs;

        /// <summary>
        /// Initializes a new instance of the <see cref="PairsTradingAlphaModel"/> class
        /// </summary>
        /// <param name="period">Period over which this insight is expected to come to fruition</param>
        /// <param name="threshold">The percent [0, 100] deviation of the ratio from the mean before emitting an insight</param>
        public PairsTradingAlphaModel(TimeSpan period, decimal threshold = 1m)
        {
            _period = period;
            _threshold = threshold;

            _securities = new HashSet<Security>();
            _pairs = new Dictionary<string, Pair>();

            Name = $"{nameof(PairsTradingAlphaModel)}({_period},{_threshold.Normalize()})";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            var insights = new List<Insight>();

            foreach (var kvp in _pairs)
            {
                var pair = kvp.Value;

                if (pair.IsReady)
                {
                    insights.AddRange(pair.GetInsightGroup());
                }
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            NotifiedSecurityChanges.UpdateCollection(_securities, changes);

            UpdatePairs(algorithm);

            // Remove pairs that has assets that were removed from the universe 
            foreach (var security in changes.RemovedSecurities)
            {
                var assetId = security.Symbol.ID.ToString();
                var keys = _pairs.Keys.Where(k => k.Contains(assetId));

                foreach (var key in keys)
                {
                    _pairs.Remove(key);
                }
            }
        }

        private void UpdatePairs(QCAlgorithm algorithm)
        {
            var securities = _securities.ToArray();

            for (var i = 0; i < securities.Length; i++)
            {
                var assetI = securities[i].Symbol;

                for (var j = i + 1; j < securities.Length; j++)
                {
                    var assetJ = securities[j].Symbol;

                    var pairName = Pair.GetPairName(assetI, assetJ);

                    if (_pairs.ContainsKey(pairName))
                    {
                        continue;
                    }

                    if (!HasPassedTest(algorithm, assetI, assetJ))
                    {
                        continue;
                    }

                    var pair = new Pair(algorithm, assetI, assetJ, _period, _threshold);
                    _pairs.Add(pairName, pair);
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

        internal class Pair
        {
            private enum State
            {
                ShortRatio,
                FlatRatio,
                LongRatio
            };

            /// <summary>
            /// Creates a name for the pair
            /// </summary>
            /// <param name="asset1">The first asset's symbol in the pair</param>
            /// <param name="asset2">The second asset's symbol in the pair</param>
            /// <returns>String: name of the pair</returns>
            public static string GetPairName(Symbol asset1, Symbol asset2)
            {
                var asset1Id = asset1.ID.ToString();
                var asset2Id = asset2.ID.ToString();

                return asset1Id.CompareTo(asset2Id) < 0
                    ? $"{asset1Id}/{asset2Id}"
                    : $"{asset2Id}/{asset1Id}";
            }

            private State _state = State.FlatRatio;

            private readonly Symbol _asset1;
            private readonly Symbol _asset2;

            private readonly IndicatorBase<IndicatorDataPoint> _asset1Price;
            private readonly IndicatorBase<IndicatorDataPoint> _asset2Price;
            private readonly IndicatorBase<IndicatorDataPoint> _ratio;
            private readonly IndicatorBase<IndicatorDataPoint> _mean;
            private readonly IndicatorBase<IndicatorDataPoint> _upperThreshold;
            private readonly IndicatorBase<IndicatorDataPoint> _lowerThreshold;

            private readonly TimeSpan _period;

            public bool IsReady => _mean.IsReady;

            /// <summary>
            /// Create a new pair
            /// </summary>
            /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
            /// <param name="asset1">The first asset's symbol in the pair</param>
            /// <param name="asset2">The second asset's symbol in the pair</param>
            /// <param name="period">Period over which this insight is expected to come to fruition</param>
            /// <param name="threshold">The percent [0, 100] deviation of the ratio from the mean before emitting an insight</param>
            public Pair(QCAlgorithm algorithm, Symbol asset1, Symbol asset2, TimeSpan period, decimal threshold)
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

                _period = period;
            }

            /// <summary>
            /// Gets the insights group for the pair
            /// </summary>
            /// <returns>Insights grouped by an unique group id</returns>
            public IEnumerable<Insight> GetInsightGroup()
            {
                // don't re-emit the same direction
                if (_state != State.LongRatio && _ratio > _upperThreshold)
                {
                    _state = State.LongRatio;

                    // asset1/asset2 is more than 2 std away from mean, short asset1, long asset2
                    var shortAsset1 = Insight.Price(_asset1, _period, InsightDirection.Down);
                    var longAsset2 = Insight.Price(_asset2, _period, InsightDirection.Up);

                    // creates a group id and set the GroupId property on each insight object
                    return Insight.Group(shortAsset1, longAsset2);
                }

                // don't re-emit the same direction
                if (_state != State.ShortRatio && _ratio < _lowerThreshold)
                {
                    _state = State.ShortRatio;

                    // asset1/asset2 is less than 2 std away from mean, long asset1, short asset2
                    var longAsset1 = Insight.Price(_asset1, _period, InsightDirection.Up);
                    var shortAsset2 = Insight.Price(_asset2, _period, InsightDirection.Down);

                    // creates a group id and set the GroupId property on each insight object
                    return Insight.Group(longAsset1, shortAsset2);
                }

                return Enumerable.Empty<Insight>();
            }
        }
    }
}