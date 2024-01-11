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
using System;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Fractal Adaptive Moving Average (FRAMA) by John Ehlers
    /// </summary>
    public class FractalAdaptiveMovingAverage : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _n = 16;
        private readonly double _w = -4.6;
        private readonly RollingWindow<double> _high;
        private readonly RollingWindow<double> _low;

        /// <summary>
        /// Initializes a new instance of the average class
        /// </summary>
        /// <param name="name">The name of the indicator instance</param>
        /// <param name="n">The window period (must be even). Example value: 16</param>
        /// <param name="longPeriod">The average period. Example value: 198</param>
        public FractalAdaptiveMovingAverage(string name, int n, int longPeriod)
            : base(name)
        {
            if (n % 2 > 0)
            {
                throw new ArgumentException($"{name}: N must be even, N = {n}", nameof(n));
            }
            _n = n;
            _w = Math.Log(2d / (1 + longPeriod));
            _high = new RollingWindow<double>(n);
            _low = new RollingWindow<double>(n);
        }

        /// <summary>
        /// Initializes a new instance of the average class
        /// </summary>
        /// <param name="n">The window period (must be even). Example value: 16</param>
        /// <param name="longPeriod">The average period. Example value: 198</param>
        public FractalAdaptiveMovingAverage(int n, int longPeriod)
            : this($"FRAMA({n},{longPeriod})", n, longPeriod)
        {

        }

        /// <summary>
        /// Initializes a new instance of the average class
        /// </summary>
        /// <param name="n">The window period (must be even). Example value: 16</param>
        public FractalAdaptiveMovingAverage(int n)
            : this(n, 198)
        {
        }

        /// <summary>
        /// Computes the average value
        /// </summary>
        /// <param name="input">The data for the calculation</param>
        /// <returns>The average value</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var price = (input.High + input.Low) / 2;
            _high.Add((double)input.High);
            _low.Add((double)input.Low);

            // our first data point just return identity
            if (_high.Samples <= _high.Size)
            {
                return price;
            }

            var hh = _high.Take(_n / 2).Max();
            var ll = _low.Take(_n / 2).Min();
            var n1 = (hh - ll) / (_n / 2);

            hh = _high.Skip(_n / 2).Take(_n / 2).Max();
            ll = _low.Skip(_n / 2).Take(_n / 2).Min();

            var n2 = (hh - ll) / (_n / 2);
            var n3 = (_high.Max() - _low.Min()) / _n;

            double dimen = 0;

            if (n1 + n2 > 0 && n3 > 0)
            {
                var log = Math.Log((n1 + n2) / n3);
                dimen = (double.IsNaN(log) ? 0 : log) / Math.Log(2);
            }

            var alpha = Math.Exp(_w * (dimen - 1));

            if (alpha < .01)
            {
                alpha = .01;
            }
            if (alpha > 1)
            {
                alpha = 1;
            }

            return (decimal)alpha * price + (1 - (decimal)alpha) * Current.Value;
        }

        /// <summary>
        /// Returns whether the indicator will return valid results
        /// </summary>
        public override bool IsReady => _high.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _high.Size;

        /// <summary>
        /// Resets the average to its initial state
        /// </summary>
        public override void Reset()
        {
            _high.Reset();
            _low.Reset();
            base.Reset();
        }
    }
}