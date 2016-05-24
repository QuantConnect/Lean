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
using System.Linq;

namespace QuantConnect.Indicators
{

    /// <summary>
    /// The Fractal Adaptive Moving Average (FRAMA) by John Ehlers
    /// </summary>
    public class FractalAdaptiveMovingAverage : WindowIndicator<IndicatorDataPoint>
    {

        double _filt;
        int _n = 16;
        double _w = -4.6;
        RollingWindow<double> _series;

        /// <summary>
        /// Initializes a new instance of the average class
        /// </summary>
        /// <param name="name"></param>
        /// <param name="n">The window period (must be even). Example value: 16</param>
        /// <param name="longPeriod">The average period. Example value: 198</param>
        public FractalAdaptiveMovingAverage(string name, int n, int longPeriod)
            : base(name, n)
        {
            if (n % 2 > 0)
            {
                throw new ArgumentException("N must be even.");
            }
            _n = n;
            _w = CalculateW(longPeriod);
            _series = new RollingWindow<double>(n);
        }

        /// <summary>
        /// Initializes a new instance of the average class
        /// </summary>
        /// <param name="name">The window period (must be even). Example value: 16</param>
        /// <param name="n"></param>
        public FractalAdaptiveMovingAverage(string name, int n)
            : this("FRAMA" + n, n, 198)
        {

        }

        /// <summary>
        /// Calculates the average value based on the input
        /// </summary>
        /// <param name="window"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            _series.Add((double)input.Price);

            // our first data point just return identity
            if (!_series.IsReady)
            {
                _filt = (double)input.Price;
                return input;
            }
            double n1;
            double n2;
            double n3;
            double hh;
            double ll;
            double dimen = 0;
            double alpha;
            double price = (double)input.Price;


            n3 = (_series.Max() - _series.Min()) / _n;

            hh = _series.Take(_n / 2).Max();
            ll = _series.Take(_n / 2).Min();

            n1 = (hh - ll) / (_n / 2);

            hh = _series.Skip(_n / 2).Take(_n / 2).Max();
            ll = _series.Skip(_n / 2).Take(_n / 2).Min();

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
            get { return _series.IsReady; }
        }

        /// <summary>
        /// Resets the average to its initial state
        /// </summary>
        public override void Reset()
        {
            _filt = 0;
            _series.Reset();
            _n = 16;
            _w = -4.6;
            base.Reset();
        }

    }
}
