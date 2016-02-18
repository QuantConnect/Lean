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
 *
*/

using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators.CandlestickPatterns
{
    /// <summary>
    /// Three Stars In The South candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long black candle with long lower shadow
    /// - second candle: smaller black candle that opens higher than prior close but within prior candle's range 
    /// and trades lower than prior close but not lower than prior low and closes off of its low(it has a shadow)
    /// - third candle: small black marubozu(or candle with very short shadows) engulfed by prior candle's range
    /// The meanings of "long body", "short body", "very short shadow" are specified with CandleSettings;
    /// The returned value is positive (+1): 3 stars in the south is always bullish;
    /// The user should consider that 3 stars in the south is significant when it appears in downtrend, while this function
    /// does not consider it
    /// </remarks>
    public class ThreeStarsInSouth : CandlestickPattern
    {
        private decimal _bodyLongPeriodTotal;
        private decimal _shadowLongPeriodTotal;
        private decimal[] _shadowVeryShortPeriodTotal = new decimal[2];
        private decimal _bodyShortPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeStarsInSouth"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ThreeStarsInSouth(string name) 
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod, CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod),
                  Math.Max(CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod)) + 2)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeInside"/> class.
        /// </summary>
        public ThreeStarsInSouth()
            : this("THREESTARSINSOUTH")
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Samples > Period; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<TradeBar> window, TradeBar input)
        {
            if (!IsReady)
            {
                if (Samples > 2) _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]);
                if (Samples > 2) _shadowLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowLong, window[2]);
                if (Samples > 2) _shadowVeryShortPeriodTotal[1] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]);
                if (Samples > 2) _shadowVeryShortPeriodTotal[0] += GetCandleRange(CandleSettingType.ShadowVeryShort, input);
                if (Samples > 2) _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                return 0m;
            }

            decimal value;
            if (GetCandleColor(window[2]) == CandleColor.Black &&
                GetCandleColor(window[1]) == CandleColor.Black &&
                GetCandleColor(input) == CandleColor.Black &&
                GetRealBody(window[2]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[2]) &&
                GetLowerShadow(window[2]) > GetCandleAverage(CandleSettingType.ShadowLong, _shadowLongPeriodTotal, window[2]) &&
                GetRealBody(window[1]) < GetRealBody(window[2]) &&
                window[1].Open > window[2].Close && window[1].Open <= window[2].High &&
                window[1].Low < window[2].Close &&
                window[1].Low >= window[2].Low &&
                GetLowerShadow(window[1]) > GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[1], window[1]) &&
                GetRealBody(input) < GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, input) &&
                GetLowerShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[0], input) &&
                GetUpperShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[0], input) &&
                input.Low > window[1].Low && input.High < window[1].High
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]) - GetCandleRange(CandleSettingType.BodyLong, window[Period - 1]);
            _shadowLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowLong, window[2]) - GetCandleRange(CandleSettingType.ShadowLong, window[Period - 1]);
            for (var i = 1; i >= 0; i--)
            {
                _shadowVeryShortPeriodTotal[i] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[i]) -
                                                  GetCandleRange(CandleSettingType.ShadowVeryShort, window[Period - 3 + i]);
            }
            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input) - GetCandleRange(CandleSettingType.BodyShort, window[Period - 3]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0;
            _shadowLongPeriodTotal = 0;
            _shadowVeryShortPeriodTotal = new decimal[2];
            _bodyShortPeriodTotal = 0;
            base.Reset();
        }
    }
}
