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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators.CandlestickPatterns
{
    /// <summary>
    /// Separating Lines candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: black (white) candle
    /// - second candle: bullish(bearish) belt hold with the same open as the prior candle
    /// The meaning of "long body" and "very short shadow" of the belt hold is specified with SetCandleSettings
    /// The returned value is positive(+1) when bullish or negative(-1) when bearish;
    /// The user should consider that separating lines is significant when coming in a trend and the belt hold has
    /// the same direction of the trend, while this function does not consider it
    /// </remarks>
    public class SeparatingLines : CandlestickPattern
    {
        private readonly int _shadowVeryShortAveragePeriod;
        private readonly int _bodyLongAveragePeriod;
        private readonly int _equalAveragePeriod;

        private decimal _shadowVeryShortPeriodTotal;
        private decimal _bodyLongPeriodTotal;
        private decimal _equalPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeparatingLines"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public SeparatingLines(string name) 
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod),
                CandleSettings.Get(CandleSettingType.Equal).AveragePeriod) + 1 + 1)
        {
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _equalAveragePeriod = CandleSettings.Get(CandleSettingType.Equal).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeparatingLines"/> class.
        /// </summary>
        public SeparatingLines()
            : this("SEPARATINGLINES")
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Samples >= Period; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar input)
        {
            if (!IsReady)
            {
                if (Samples >= Period - _shadowVeryShortAveragePeriod)
                {
                    _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, input);
                }

                if (Samples >= Period - _bodyLongAveragePeriod)
                {
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, input);
                }

                if (Samples >= Period - _equalAveragePeriod)
                {
                    _equalPeriodTotal += GetCandleRange(CandleSettingType.Equal, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                // opposite candles
                (int)GetCandleColor(window[1]) == -(int)GetCandleColor(input) &&
                // same open
                input.Open <= window[1].Open + GetCandleAverage(CandleSettingType.Equal, _equalPeriodTotal, window[1]) &&
                input.Open >= window[1].Open - GetCandleAverage(CandleSettingType.Equal, _equalPeriodTotal, window[1]) &&
                // belt hold: long body
                GetRealBody(input) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, input) &&
                (
                  // with no lower shadow if bullish
                  (GetCandleColor(input) == CandleColor.White &&
                    GetLowerShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal, input)
                  )
                  ||
                  // with no upper shadow if bearish
                  (GetCandleColor(input) == CandleColor.Black &&
                    GetUpperShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal, input)
                  )
                )
              )
                value = (int)GetCandleColor(input);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, input) -
                                           GetCandleRange(CandleSettingType.ShadowVeryShort, window[_shadowVeryShortAveragePeriod]);

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, input) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod]);

            _equalPeriodTotal += GetCandleRange(CandleSettingType.Equal, window[1]) -
                                 GetCandleRange(CandleSettingType.Equal, window[_equalAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _shadowVeryShortPeriodTotal = 0m;
            _bodyLongPeriodTotal = 0m;
            _equalPeriodTotal = 0m;
            base.Reset();
        }
    }
}
