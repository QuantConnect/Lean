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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the short stop and lower stop values of the Chande Kroll Stop Indicator.
    /// It is used to determine the optimal placement of a stop-loss order.
    /// </summary>
    public class ChandeKrollStop : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly AverageTrueRange _atr;
        private readonly decimal _atrMult;

        private readonly Maximum _underlyingMaximum;
        private readonly Minimum _underlyingMinimum;

        /// <summary>
        /// Gets the short stop of ChandeKrollStop.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ShortStop { get; }

        /// <summary>
        /// Gets the long stop of ChandeKrollStop.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> LongStop { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChandeKrollStop"/> class.
        /// </summary>
        /// <param name="atrPeriod">The period over which to compute the average true range.</param>
        /// <param name="atrMult">The ATR multiplier to be used to compute stops distance.</param>
        /// <param name="period">The period over which to compute the max of high stop and min of low stop.</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public ChandeKrollStop(int atrPeriod, decimal atrMult, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"CKS({atrPeriod},{atrMult},{period})", atrPeriod, atrMult, period, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChandeKrollStop"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="atrPeriod">The period over which to compute the average true range.</param>
        /// <param name="atrMult">The ATR multiplier to be used to compute stops distance.</param>
        /// <param name="period">The period over which to compute the max of high stop and min of low stop.</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public ChandeKrollStop(string name, int atrPeriod, decimal atrMult, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            WarmUpPeriod = atrPeriod + period - 1;

            _atr = new AverageTrueRange(atrPeriod, movingAverageType);
            _atrMult = atrMult;
            _underlyingMaximum = new Maximum(atrPeriod);
            _underlyingMinimum = new Minimum(atrPeriod);

            LongStop = new Minimum(name + "_Long", period);
            ShortStop = new Maximum(name + "_Short", period);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _atr.Update(input);

            _underlyingMaximum.Update(input.EndTime, input.High);
            var highStop = _underlyingMaximum.Current.Value - _atr.Current.Value * _atrMult;

            _underlyingMinimum.Update(input.EndTime, input.Low);
            var lowStop = _underlyingMinimum.Current.Value + _atr.Current.Value * _atrMult;

            ShortStop.Update(input.EndTime, highStop);
            LongStop.Update(input.EndTime, lowStop);

            return input.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _atr.Reset();
            _underlyingMaximum.Reset();
            _underlyingMinimum.Reset();
            ShortStop.Reset();
            LongStop.Reset();
        }
    }
}
