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
    /// <summary>
    /// The tools of the Swiss Army Knife. Some of the tools lend well to chaining with the "Of" Method, others may be treated as moving averages
    /// </summary>
    public enum SwissArmyKnifeTool
    {
        /// <summary>
        /// Two Pole Gaussian Filter
        /// </summary>
        Gauss,
        /// <summary>
        /// Two Pole Butterworth Filter
        /// </summary>
        Butter,
        /// <summary>
        /// High Pass Filter
        /// </summary>
        HighPass,
        /// <summary>
        /// Two Pole High Pass Filter
        /// </summary>
        TwoPoleHighPass,
        /// <summary>
        /// BandPass Filter
        /// </summary>
        BandPass,
    }

    /// <summary>
    /// Swiss Army Knife indicator by John Ehlers
    /// </summary>
    public class SwissArmyKnife : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly RollingWindow<double> _price;
        private readonly RollingWindow<double> _filt;
        private readonly int _period;
        private readonly double _a0 = 1;
        private readonly double _a1 = 0;
        private readonly double _a2 = 0;
        private readonly double _b0 = 1;
        private readonly double _b1 = 0;
        private readonly double _b2 = 0;
        private readonly double _c0 = 1;

        /// <summary>
        /// Swiss Army Knife indicator by John Ehlers
        /// </summary>
        /// <param name="period"></param>
        /// <param name="delta"></param>
        /// <param name="tool"></param>
        public SwissArmyKnife(int period, double delta, SwissArmyKnifeTool tool)
            : this($"Swiss({period},{delta},{tool})", period, delta, tool)
        {
        }

        /// <summary>
        /// Swiss Army Knife indicator by John Ehlers
        /// </summary>
        /// <param name="name"></param>
        /// <param name="period"></param>
        /// <param name="delta"></param>
        /// <param name="tool"></param>
        public SwissArmyKnife(string name, int period, double delta, SwissArmyKnifeTool tool)
            : base(name)
        {
            _filt = new RollingWindow<double>(2) {0, 0};
            _price = new RollingWindow<double>(3);
            _period = period;
            var beta = 2.415 * (1 - Math.Cos(2 * Math.PI / period));
            var alpha = -beta + Math.Sqrt(Math.Pow(beta, 2) + 2d * beta);

            switch (tool)
            {
                case SwissArmyKnifeTool.Gauss:
                    _c0 = alpha * alpha;
                    _a1 = 2d * (1 - alpha);
                    _a2 = -(1 - alpha) * (1 - alpha);
                    break;
                case SwissArmyKnifeTool.Butter:
                    _c0 = alpha * alpha / 4d;
                    _b1 = 2;
                    _b2 = 1;
                    _a1 = 2d * (1 - alpha);
                    _a2 = -(1 - alpha) * (1 - alpha);
                    break;
                case SwissArmyKnifeTool.HighPass:
                    alpha = (Math.Cos(2 * Math.PI / period) + Math.Sin(2 * Math.PI / period) - 1) / Math.Cos(2 * Math.PI / period);
                    _c0 = (1 + alpha) / 2;
                    _b1 = -1;
                    _a1 = 1 - alpha;
                    break;
                case SwissArmyKnifeTool.TwoPoleHighPass:
                    _c0 = (1 + alpha) * (1 + alpha) / 4;
                    _b1 = -2;
                    _b2 = 1;
                    _a1 = 2d * (1 - alpha);
                    _a2 = -(1 - alpha) * (1 - alpha);
                    break;
                case SwissArmyKnifeTool.BandPass:
                    beta = Math.Cos(2 * Math.PI / period);
                    var gamma = (1 / Math.Cos(4 * Math.PI * delta / period));
                    alpha = gamma - Math.Sqrt(Math.Pow(gamma, 2) - 1);
                    _c0 = (1 - alpha) / 2d;
                    _b0 = 1;
                    _b2 = -1;
                    _a1 = -beta * (1 - alpha);
                    _a2 = alpha;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tool), tool, "Invalid SwissArmyKnifeTool");
            }
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _price.Add((double)input.Price);

            if (_price.Samples == 1)
            {
                _price.Add(_price[0]);
                _price.Add(_price[0]);
            }

            var signal = _a0 * _c0 * (_b0 * _price[0] + _b1 * _price[1] + _b2 * _price[2]) + _a0 * (_a1 * _filt[0] + _a2 * _filt[1]);

            _filt.Add(signal);

            return (decimal)signal;
        }

        /// <summary>
        /// Resets to the initial state
        /// </summary>
        public override void Reset()
        {
            _price.Reset();
            _filt.Reset();
            _filt.Add(0);
            _filt.Add(0);
            base.Reset();
        }
    }
}