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
 *
*/

using System;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary> 
    /// Oscillator indicator that measures momentum and mean-reversion over a specified
    /// period n.
    /// Source: Harris, Michael. "Momersion Indicator." Price Action Lab.,
    ///             13 Aug. 2015. Web. http://www.priceactionlab.com/Blog/2015/08/momersion-indicator/.
    /// </summary>
    public class MomersionIndicator : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The minimum observations needed to consider the indicator ready. After that observation
        /// number is reached, the indicator will continue gathering data until the full period.
        /// </summary>
        private readonly int? _minPeriod;

        /// <summary>
        /// The rolling window used to store the momentum.
        /// </summary>
        private readonly RollingWindow<decimal> _multipliedDiffWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="MomersionIndicator"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="minPeriod">The minimum period.</param>
        /// <param name="fullPeriod">The full period.</param>
        /// <exception cref="System.ArgumentException">The minimum period should be greater of 3.;minPeriod</exception>
        public MomersionIndicator(string name, int? minPeriod, int fullPeriod)
            : base(name, fullPeriod)
        {
            if (minPeriod < 4)
            {
                throw new ArgumentException("The minimum period should be 4.", nameof(minPeriod));
            }
            _minPeriod = minPeriod;
            _multipliedDiffWindow = new RollingWindow<decimal>(fullPeriod);
            WarmUpPeriod = (minPeriod + 2) ?? (fullPeriod + 3);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MomersionIndicator"/> class.
        /// </summary>
        /// <param name="minPeriod">The minimum period.</param>
        /// <param name="fullPeriod">The full period.</param>
        public MomersionIndicator(int? minPeriod, int fullPeriod)
            : this($"Momersion({minPeriod},{fullPeriod})", minPeriod, fullPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MomersionIndicator"/> class.
        /// </summary>
        /// <param name="fullPeriod">The full period.</param>
        public MomersionIndicator(int fullPeriod)
            : this(null, fullPeriod)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get
            {
                if (_minPeriod.HasValue)
                {
                    return _multipliedDiffWindow.Count >= _minPeriod;
                }
                return _multipliedDiffWindow.Samples > _multipliedDiffWindow.Size;
            }
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _multipliedDiffWindow.Reset();
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window"></param>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>
        /// A new value for this indicator
        /// </returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (window.Count >= 3)
            {
                _multipliedDiffWindow.Add((window[0].Value - window[1].Value) * (window[1].Value - window[2].Value));
            }

            // Estimate the indicator if less than 50% of observation are zero. Avoid division by
            // zero and estimations with few real observations in case of forward filled data.
            if (IsReady && _multipliedDiffWindow.Count(obs => obs == 0) < 0.5 * _multipliedDiffWindow.Count)
            {
                var mc = _multipliedDiffWindow.Count(obs => obs > 0);
                var mRc = _multipliedDiffWindow.Count(obs => obs < 0);
                return 100m * mc / (mc + mRc);
            }
            return 50m;
        }
    }
}
