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
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the upper and lower band of the Donchian Channel.
    /// The upper band is computed by finding the highest high over the given period.
    /// The lower band is computed by finding the lowest low over the given period.
    /// The primary output value of the indicator is the mean of the upper and lower band for 
    /// the given timeframe.
    /// </summary>
    public class RelativeVigorIndex : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly RollingWindow<IBaseDataBar> _previousInputs;
        public RviSignal Signal {get;  }


        /// <summary>
        /// Gets the upper band of the Donchian Channel.
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> CloseBand { get; }

        /// <summary>
        /// Gets the lower band of the Donchian Channel.
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> RangeBand { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="DonchianChannel"/> class.
        /// </summary>
        /// <param name="upperPeriod">The period for the upper channel.</param>
        /// <param name="lowerPeriod">The period for the lower channel</param>
        public RelativeVigorIndex(int period)
            : this($"DCH({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DonchianChannel"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="upperPeriod">The period for the upper channel.</param>
        /// <param name="lowerPeriod">The period for the lower channel</param>
        public RelativeVigorIndex(
            string name,
            int period,
            MovingAverageType type = MovingAverageType.Simple
            )
            : base(name)
        {
            WarmUpPeriod = 1 + period;
            CloseBand = type.AsIndicator("_closingBand", period);
            RangeBand = type.AsIndicator("_rangeBand", period);
            _previousInputs = new RollingWindow<IBaseDataBar>(3);
            Signal = new RviSignal("Signal");
        }


        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => CloseBand.IsReady && CloseBand.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator, which by convention is the mean value of the upper band and lower band.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var a = input.Close - input.Open;
            var b = input.Close - _previousInputs[0].Open;
            var c = input.Close - _previousInputs[1].Open;
            var d = input.Close - _previousInputs[2].Open;
            var e = input.High - input.Low;
            var f = input.High - _previousInputs[0].Low;
            var g = input.High - _previousInputs[1].Low;
            var h = input.High - _previousInputs[2].Low;
            CloseBand.Update(input.Time, (a + 2 * (b + c) + d) / 6);
            RangeBand.Update(input.Time, (e + 2 * (f + g) + h) / 6);
            var rvi = CloseBand / RangeBand;
            Signal.Update(rvi.ConvertInvariant<BaseData>());
            return IsReady ? rvi : 0m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            CloseBand.Reset();
            RangeBand.Reset();
        }

        public class RviSignal : WindowIndicator<BaseData>, IIndicatorWarmUpPeriodProvider
        {
            private int _warmUpPeriod;

            public RviSignal(string name)
                : base(name, 3)
            {
            }

            protected override decimal ComputeNextValue(IReadOnlyWindow<BaseData> window, BaseData input)
            {
                return IsReady ? input.Value + 2 * (window[0].Value + window[1].Value) + window[2].Value : 0m;
            }

            public int WarmUpPeriod => _warmUpPeriod;
        }
    }
}
