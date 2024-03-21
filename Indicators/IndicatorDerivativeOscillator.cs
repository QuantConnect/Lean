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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="r1"></param>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="a3"></param>
        public IndicatorDerivativeOscillator(string name, int r1, int a1, int a2, int a3)
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
    }
}
