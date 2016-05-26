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
    public class FractalAdaptiveMovingAverage : TradeBarIndicator
    {

        double _filt;
        int _n = 16;
        double _w = -4.6;
        RollingWindow<double> _high;
        RollingWindow<double> _low;

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
                throw new ArgumentException("N must be even.");
            }
            _n = n;
            _w = CalculateW(longPeriod);
            _high = new RollingWindow<double>(n);
            _low = new RollingWindow<double>(n);
        }

        /// <summary>
        /// Initializes a new instance of the average class
        /// </summary>
        /// <param name="name">The name of the indicator instance</param>
        /// <param name="n">The window period (must be even). Example value: 16</param>
        public FractalAdaptiveMovingAverage(int n)
            : this("FRAMA" + n, n, 198)
        {

        }

        /// <summary>
        /// Computes the average value
        /// </summary>
        /// <param name="input">The data for the calculation</param>
        /// <returns>The average value</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var price = (double)(input.High + input.Low) / 2;
            _high.Add((double)input.High);
            _low.Add((double)input.Low);

            // our first data point just return identity
            if (!_high.IsReady)
            {
                _filt = price;
            }
            double n1;
            double n2;
            double n3;
            double hh;
            double ll;
            double dimen = 0;
            double alpha;

            n3 = (_high.Max() - _low.Min()) / _n;

            hh = _high.Take(_n / 2).Max();
            ll = _low.Take(_n / 2).Min();

            n1 = (hh - ll) / (_n / 2);

            if (_high.IsReady)
            {
                hh = _high.Skip(_n / 2).Take(_n / 2).Max();
                ll = _low.Skip(_n / 2).Take(_n / 2).Min();
            }

            n2 = (hh - ll) / (_n / 2);

            if (n1 > 0 && n2 > 0 && n3 > 0)
            {
                dimen = (Math.Log(n1 + n2) - Math.Log(n3)) / Math.Log(2);
            };

            alpha = Math.Exp(_w * (dimen - 1));
            if (alpha < .01) { alpha = .01; }
            if (alpha > 1) { alpha = 1; }

            _filt = alpha * price + (1 - alpha) * _filt;

            return (decimal)_filt;

        }

        private double CalculateW(int period)
        {
            return Math.Log(2d / (period + 1d));
        }


        /// <summary>
        /// Returns whether the indicator will return valid results
        /// </summary>
        public override bool IsReady
        {
            get { return _high.IsReady; }
        }

        /// <summary>
        /// Resets the average to its initial state
        /// </summary>
        public override void Reset()
        {
            _filt = 0;
            _high.Reset();
			_low.Reset();
            _n = 16;
            _w = -4.6;
            base.Reset();
        }

    }
}
