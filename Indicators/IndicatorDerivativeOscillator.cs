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
        private readonly int _r1;
        private readonly int _a1;
        private readonly int _a2;
        private readonly int _a3;

        /// <summary>
        /// Initializes a new instance of the IndicatorDerivativeOscillator class with the specified name and periods.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="r1">The period for the RSI calculation</param>
        /// <param name="a1">The period for the smoothing RSI</param>
        /// <param name="a2">The period for the double smoothing RSI</param>
        /// <param name="a3">The period for the signal line</param>
        public IndicatorDerivativeOscillator(
            string name,
            int r1,
            int a1,
            int a2,
            int a3
            )
            : base(name)
        {
            _r1 = r1;
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _rsi = new RelativeStrengthIndex($"{name}_RSI", r1);
            _smoothedRsi = new ExponentialMovingAverage($"{name}_SmoothedRSI", a1);
            _doubleSmoothedRsi = new ExponentialMovingAverage($"{name}_DoubleSmoothedRSI", a2);
            _signalLine = new SimpleMovingAverage($"{name}_SignalLine", a3);
        }

        public override bool IsReady { get; }
        /* 
            (Lars) this is what I want it to be as this is how other Indicators implemented it,
            But then the test cases fail
        */
        // public override bool IsReady => _rsi.IsReady && _smoothedRsi.IsReady && _doubleSmoothedRsi.IsReady && _signalLine.IsReady;

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            /* 
                (Lars) Here as well, just check your own IsReady in one go
                instead of doing them one by one
                
            */
            // if (!IsReady)
            // {
            //     return 0;
            // }

            _rsi.Update(input);

            if (!_rsi.IsReady)
            {
                return 0;
            }

            _smoothedRsi.Update(_rsi.Current);

            if (!_smoothedRsi.IsReady)
            {
                return 0;
            }

            _doubleSmoothedRsi.Update(_smoothedRsi.Current);

            if (!_doubleSmoothedRsi.IsReady)
            {
                return 0;
            }

            _signalLine.Update(_doubleSmoothedRsi.Current);

            if (!_signalLine.IsReady)
            {
                return 0;
            }

            return _doubleSmoothedRsi.Current.Value - _signalLine.Current.Value;
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }
    }
}
