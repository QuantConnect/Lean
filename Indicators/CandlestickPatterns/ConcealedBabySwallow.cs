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
    /// Concealed Baby Swallow candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: black marubozu (very short shadows)
    /// - second candle: black marubozu(very short shadows)
    /// - third candle: black candle that opens gapping down but has an upper shadow that extends into the prior body
    /// - fourth candle: black candle that completely engulfs the third candle, including the shadows
    /// The meanings of "very short shadow" are specified with SetCandleSettings;
    /// The returned value is positive(+1): concealing baby swallow is always bullish;
    /// The user should consider that concealing baby swallow is significant when it appears in downtrend, while 
    /// this function does not consider it
    /// </remarks>
    public class ConcealedBabySwallow : CandlestickPattern
    {
        private readonly int _shadowVeryShortAveragePeriod;

        private decimal[] _shadowVeryShortPeriodTotal = new decimal[4];

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcealedBabySwallow"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ConcealedBabySwallow(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod + 3 + 1)
        {
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcealedBabySwallow"/> class.
        /// </summary>
        public ConcealedBabySwallow()
            : this("CONCEALEDBABYSWALLOW")
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
                    _shadowVeryShortPeriodTotal[3] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[3]);
                    _shadowVeryShortPeriodTotal[2] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[2]);
                    _shadowVeryShortPeriodTotal[1] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st black
                GetCandleColor(window[3]) == CandleColor.Black &&
                // 2nd black
                GetCandleColor(window[2]) == CandleColor.Black &&
                // 3rd black
                GetCandleColor(window[1]) == CandleColor.Black &&
                // 4th black
                GetCandleColor(input) == CandleColor.Black &&
                // 1st: marubozu
                GetLowerShadow(window[3]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[3], window[3]) &&
                GetUpperShadow(window[3]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[3], window[3]) &&
                // 2nd: marubozu
                GetLowerShadow(window[2]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[2], window[2]) &&
                GetUpperShadow(window[2]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[2], window[2]) &&
                // 3rd: opens gapping down
                GetRealBodyGapDown(window[1], window[2]) &&
                //      and has an upper shadow
                GetUpperShadow(window[1]) > GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[1], window[1]) &&
                //      that extends into the prior body
                window[1].High > window[2].Close &&
                // 4th: engulfs the 3rd including the shadows
                input.High > window[1].High && input.Low < window[1].Low
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 3; i >= 1; i--)
            {
                _shadowVeryShortPeriodTotal[i] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[i]) -
                                                  GetCandleRange(CandleSettingType.ShadowVeryShort, window[i + _shadowVeryShortAveragePeriod]);
            }

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _shadowVeryShortPeriodTotal = new decimal[4];
            base.Reset();
        }
    }
}
