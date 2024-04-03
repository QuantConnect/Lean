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

namespace QuantConnect.Indicators
{
    /// <summary>\
    /// Represents the Derivative Oscillator Indicator, utilizing
    /// a moving average convergence-divergence (MACD) histogram to a double-smoothed relative strength index (RSI).
    /// </summary>
    public class IndicatorDerivativeOscillator : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly RelativeStrengthIndex _rsi;
        private readonly ExponentialMovingAverage _smoothedRsi;
        private readonly ExponentialMovingAverage _doubleSmoothedRsi;
        private readonly SimpleMovingAverage _signalLine;
        private readonly int _rsiPeriod;
        private readonly int _smoothingRsiPeriod;
        private readonly int _doubleSmoothingRsiPeriod;
        private readonly int _signalLinePeriod;

        /// <summary>
        /// Initializes a new instance of the IndicatorDerivativeOscillator class with the specified name and periods.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="rsiPeriod">The period for the RSI calculation</param>
        /// <param name="smoothingRsiPeriod">The period for the smoothing RSI</param>
        /// <param name="doubleSmoothingRsiPeriod">The period for the double smoothing RSI</param>
        /// <param name="signalLinePeriod">The period for the signal line</param>
        public IndicatorDerivativeOscillator(string name, int rsiPeriod, int smoothingRsiPeriod, int doubleSmoothingRsiPeriod, int signalLinePeriod) : base(name)
        {
            _rsiPeriod = rsiPeriod;
            _smoothingRsiPeriod = smoothingRsiPeriod;
            _doubleSmoothingRsiPeriod = doubleSmoothingRsiPeriod;
            _signalLinePeriod = signalLinePeriod;
            _rsi = new RelativeStrengthIndex($"{name}_RSI", rsiPeriod);
            _smoothedRsi = new ExponentialMovingAverage($"{name}_SmoothedRSI", smoothingRsiPeriod);
            _doubleSmoothedRsi = new ExponentialMovingAverage($"{name}_DoubleSmoothedRSI", doubleSmoothingRsiPeriod);
            _signalLine = new SimpleMovingAverage($"{name}_SignalLine", signalLinePeriod);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady =>
            _rsi.IsReady && _smoothedRsi.IsReady && _doubleSmoothedRsi.IsReady && _signalLine.IsReady;

        /// <summary>
        /// Computes the next value for the derivative oscillator indicator from the given state
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (!IsReady)
            {
                return 0;
            }

            _rsi.Update(input);

            _smoothedRsi.Update(_rsi.Current);

            _doubleSmoothedRsi.Update(_smoothedRsi.Current);

            _signalLine.Update(_doubleSmoothedRsi.Current);

            return _doubleSmoothedRsi.Current.Value - _signalLine.Current.Value;
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }
    }
}
