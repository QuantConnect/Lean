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
    /// The Relative Vigor Index (RVI) compares the ratio of the closing price of a security to its trading range.
    /// For illustration, let:
    /// <para>a = Close−Open</para>
    /// <para>b = Close−Open of One Bar Prior to a</para>
    /// <para>c = Close−Open of One Bar Prior to b</para>
    /// <para>d = Close−Open of One Bar Prior to c</para>
    /// <para>e = High−Low of Bar a</para>
    /// <para>f = High−Low of Bar b</para>
    /// <para>g = High−Low of Bar c</para>
    /// <para>h = High−Low of Bar d</para>
    ///
    /// Then let (a+2*(b+c)+d)/6 be NUM and (e+2*(f+g)+h)/6 be DENOM.
    /// <para>RVI = SMA(NUM)/SMA(DENOM)</para>
    /// for a specified period.
    /// 
    /// https://www.investopedia.com/terms/r/relative_vigor_index.asp
    /// </summary>
    public class RelativeVigorIndex : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// An optional signal line which behaves like a slowed version of the RVI.
        /// </summary>
        public RviSignal Signal { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => CloseBand.IsReady && RangeBand.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }
        
        private readonly RollingWindow<IBaseDataBar> _previousInputs;

        /// <summary>
        /// Gets the band of Closes for the RVI.
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> CloseBand { get; }

        /// <summary>
        /// Gets the band of Ranges for the RVI.
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> RangeBand { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeVigorIndex"/> (RVI) class.
        /// </summary>
        /// <param name="period">The period for the RelativeVigorIndex.</param>
        /// <param name="type">The type of Moving Average to use</param>
        public RelativeVigorIndex(int period, MovingAverageType type)
            : this($"RVI({period},{type})", period, type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeVigorIndex"/> (RVI) class.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        /// <param name="period">The period for the RelativeVigorIndex.</param>
        /// <param name="type">The type of Moving Average to use</param>
        public RelativeVigorIndex(string name, int period, MovingAverageType type = MovingAverageType.Simple)
            : base(name)
        {
            WarmUpPeriod = period + 3;
            CloseBand = type.AsIndicator("_closingBand", period);
            RangeBand = type.AsIndicator("_rangeBand", period);
            _previousInputs = new RollingWindow<IBaseDataBar>(3);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeVigorIndex"/> (RVI) class along with a signal term.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        /// <param name="signalName">The name of the signal associated with this indicator.</param>
        /// <param name="period">The period for the RelativeVigorIndex.</param>
        /// <param name="type">The type of Moving Average to use</param>
        public RelativeVigorIndex(
            string name, string signalName, int period, MovingAverageType type = MovingAverageType.Simple)
            : base(name)
        {
            WarmUpPeriod = period + 3;
            CloseBand = type.AsIndicator("_closingBand", period);
            RangeBand = type.AsIndicator("_rangeBand", period);
            _previousInputs = new RollingWindow<IBaseDataBar>(3);
            Signal = new RviSignal(signalName);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (_previousInputs.IsReady)
            {
                var a = input.Close - input.Open;
                var b = _previousInputs[0].Close - _previousInputs[0].Open;
                var c = _previousInputs[1].Close - _previousInputs[1].Open;
                var d = _previousInputs[2].Close - _previousInputs[2].Open;
                var e = input.High - input.Low;
                var f = _previousInputs[0].High - _previousInputs[0].Low;
                var g = _previousInputs[1].High - _previousInputs[1].Low;
                var h = _previousInputs[2].High - _previousInputs[2].Low;
                CloseBand.Update(input.Time, (a + 2 * (b + c) + d) / 6);
                RangeBand.Update(input.Time, (e + 2 * (f + g) + h) / 6);
            }

            _previousInputs.Add(input);
            if (CloseBand.IsReady && RangeBand.IsReady)
            {
                var rvi = CloseBand / RangeBand;
                Signal?.Update(input.Time, rvi); // Checks for null before updating.
                return rvi;
            }

            return 0m;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            CloseBand.Reset();
            RangeBand.Reset();
            _previousInputs.Reset();
            Signal?.Reset();
        }

        /// <summary>
        /// The signal for the Relative Vigor Index, itself an indicator. 
        /// </summary>
        public class RviSignal : Indicator, IIndicatorWarmUpPeriodProvider
        {
            private readonly RollingWindow<IndicatorDataPoint> _rollingRvi;

            /// <summary>
            /// Initializes the signal term.
            /// </summary>
            /// <param name="name"></param>
            public RviSignal(string name)
                : base(name)
            {
                WarmUpPeriod = 3;
                _rollingRvi = new RollingWindow<IndicatorDataPoint>(3);
            }

            /// <summary>
            /// Gets a flag indicating when this indicator is ready and fully initialized
            /// </summary>
            public override bool IsReady => _rollingRvi.IsReady;

            /// <summary>
            /// Resets this indicator to its initial state
            /// </summary>
            public int WarmUpPeriod { get; }

            /// <summary>
            /// Computes the next value of this indicator from the given state
            /// </summary>
            /// <param name="input">The input given to the indicator</param>
            /// <returns>A new value for this indicator</returns>
            protected override decimal ComputeNextValue(IndicatorDataPoint input)
            {
                if (IsReady)
                {
                    var output = (input.Value + 2 * (_rollingRvi[0] + _rollingRvi[1]) + _rollingRvi[2]) / 6;
                    _rollingRvi.Add(input);
                    return output;
                }

                _rollingRvi.Add(input);
                return 0m;
            }

            /// <summary>
            /// Resets this indicator to its initial state
            /// </summary>
            public override void Reset()
            {
                base.Reset();
                _rollingRvi.Reset();
            }
        }
    }
}
