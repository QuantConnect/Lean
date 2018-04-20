using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// This alpha model is designed to work against a single, predefined pair.
    /// This model generates alternating long ratio/short ratio insights emitted as a group
    /// </summary>
    public class PairsTradingAlphaModel : IAlphaModel, INamedModel
    {
        private enum State
        {
            ShortRatio,
            FlatRatio,
            LongRatio
        };

        private readonly Symbol _asset1;
        private readonly Symbol _asset2;
        private readonly decimal _threshold;
        private State _state = State.FlatRatio;

        private IndicatorBase<IndicatorDataPoint> _asset1Price;
        private IndicatorBase<IndicatorDataPoint> _asset2Price;
        private IndicatorBase<IndicatorDataPoint> _ratio;
        private IndicatorBase<IndicatorDataPoint> _mean;
        private IndicatorBase<IndicatorDataPoint> _upperThreshold;
        private IndicatorBase<IndicatorDataPoint> _lowerThreshold;

        /// <summary>
        /// Defines a name for a framework model
        /// </summary>
        public string Name => $"{nameof(PairsTradingAlphaModel)}({_asset1},{_asset2},{_threshold.Normalize()})";

        /// <summary>
        /// Initializes a new instance of the <see cref="PairsTradingAlphaModel"/> class
        /// </summary>
        /// <param name="asset1">The first asset's symbol in the pair</param>
        /// <param name="asset2">The second asset's symbol in the pair</param>
        /// <param name="threshold">The percent [0, 100] deviation of the ratio from the mean before emitting an insight</param>
        public PairsTradingAlphaModel(Symbol asset1, Symbol asset2, decimal threshold = 1m)
        {
            _asset1 = asset1;
            _asset2 = asset2;
            _threshold = threshold;
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            if (_mean?.IsReady != true)
            {
                return Enumerable.Empty<Insight>();
            }

            // don't re-emit the same direction
            if (_state != State.LongRatio && _ratio > _upperThreshold)
            {
                _state = State.LongRatio;

                // asset1/asset2 is more than 2 std away from mean, short asset1, long asset2
                var shortAsset1 = Insight.Price(_asset1, TimeSpan.FromMinutes(15), InsightDirection.Down);
                var longAsset2 = Insight.Price(_asset2, TimeSpan.FromMinutes(15), InsightDirection.Up);

                // creates a group id and set the GroupId property on each insight object
                Insight.Group(shortAsset1, longAsset2);
                return new[] {shortAsset1, longAsset2};
            }

            // don't re-emit the same direction
            if (_state != State.ShortRatio && _ratio < _lowerThreshold)
            {
                _state = State.ShortRatio;

                // asset1/asset2 is less than 2 std away from mean, long asset1, short asset2
                var longAsset1 = Insight.Price(_asset1, TimeSpan.FromMinutes(15), InsightDirection.Up);
                var shortAsset2 = Insight.Price(_asset2, TimeSpan.FromMinutes(15), InsightDirection.Down);

                // creates a group id and set the GroupId property on each insight object
                Insight.Group(longAsset1, shortAsset2);
                return new[] {longAsset1, shortAsset2};
            }

            return Enumerable.Empty<Insight>();
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                // this model is limitted to looking at a single pair of assets
                if (added.Symbol != _asset1 && added.Symbol != _asset2)
                {
                    continue;
                }

                if (added.Symbol == _asset1)
                {
                    _asset1Price = algorithm.Identity(added.Symbol);
                }
                else
                {
                    _asset2Price = algorithm.Identity(added.Symbol);
                }
            }

            if (_ratio == null)
            {
                // initialize indicators dependent on both assets
                if (_asset1Price != null && _asset2Price != null)
                {
                    _ratio = _asset1Price.Over(_asset2Price);
                    _mean = new ExponentialMovingAverage(500).Of(_ratio);

                    var upper = new ConstantIndicator<IndicatorDataPoint>("ct", 1 + _threshold / 100m);
                    _upperThreshold = _mean.Times(upper, "UpperThreshold");

                    var lower = new ConstantIndicator<IndicatorDataPoint>("ct", 1 - _threshold / 100m);
                    _lowerThreshold = _mean.Times(lower, "LowerThreshold");
                }
            }
        }
    }
}