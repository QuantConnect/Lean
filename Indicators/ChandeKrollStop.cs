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
    /// This indicator computes the short stop and lower stop values of the Chande Kroll Stop Indicator.
    /// It is used to determine the optimal placement of a stop-loss order.
    /// </summary>
    public class ChandeKrollStop : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly AverageTrueRange _atr;
        private readonly decimal _atrMult;
        private readonly RollingWindow<IBaseDataBar> _inputValues;
        private readonly RollingWindow<decimal> _high_stop_list;
        private readonly RollingWindow<decimal> _low_stop_list;

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
        public override bool IsReady => Samples > 0;

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
        public ChandeKrollStop(int atrPeriod, decimal atrMult, int period)
            : this($"CKS({atrPeriod},{atrMult},{period})", atrPeriod, atrMult, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChandeKrollStop"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="atrPeriod">The period over which to compute the average true range.</param>
        /// <param name="atrMult">The ATR multiplier to be used to compute stops distance.</param>
        /// <param name="period">The period over which to compute the max of high stop and min of low stop.</param>
        public ChandeKrollStop(string name, int atrPeriod, decimal atrMult, int period)
            : base(name)
        {
            WarmUpPeriod = 1;

            _high_stop_list = new RollingWindow<decimal>(period);
            _low_stop_list = new RollingWindow<decimal>(period);

            _atr = new AverageTrueRange(atrPeriod);
            _atrMult = atrMult;
            _inputValues = new RollingWindow<IBaseDataBar>(atrPeriod);

            LongStop = new Identity(name + "_LongStop");
            ShortStop = new Identity(name + "_ShortStop");
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _atr.Update(input);
            _inputValues.Add(input);

            var highs = _inputValues.Select(input => input.High).ToList();
            var high_stop = highs.Max() - _atr.Current.Value * _atrMult;
            _high_stop_list.Add(high_stop);

            var lows = _inputValues.Select(input => input.Low).ToList(); ;
            var low_stop = lows.Min() + _atr.Current.Value * _atrMult;
            _low_stop_list.Add(low_stop);

            if (!_high_stop_list.IsReady || !_low_stop_list.IsReady)
            {
                return 0m;
            }

            ShortStop.Update(input.EndTime, _high_stop_list.Max());
            LongStop.Update(input.EndTime, _low_stop_list.Min());

            return input.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _atr.Reset();
            _inputValues.Reset();
            _high_stop_list.Reset();
            _low_stop_list.Reset();
            ShortStop.Reset();
            LongStop.Reset();
        }
    }
}
