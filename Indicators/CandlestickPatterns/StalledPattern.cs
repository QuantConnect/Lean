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
    /// Stalled Pattern candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - three white candlesticks with consecutively higher closes
    /// - first candle: long white
    /// - second candle: long white with no or very short upper shadow opening within or near the previous white real body
    /// and closing higher than the prior candle
    /// - third candle: small white that gaps away or "rides on the shoulder" of the prior long real body(= it's at 
    /// the upper end of the prior real body)
    /// The meanings of "long", "very short", "short", "near" are specified with SetCandleSettings;
    /// The returned value is negative(-1): stalled pattern is always bearish;
    /// The user should consider that stalled pattern is significant when it appears in uptrend, while this function
    /// does not consider it
    /// </remarks>
    public class StalledPattern : CandlestickPattern
    {
        private readonly int _bodyLongAveragePeriod;
        private readonly int _bodyShortAveragePeriod;
        private readonly int _shadowVeryShortAveragePeriod;
        private readonly int _nearAveragePeriod;

        private decimal[] _bodyLongPeriodTotal = new decimal[3];
        private decimal _bodyShortPeriodTotal;
        private decimal _shadowVeryShortPeriodTotal;
        private decimal[] _nearPeriodTotal = new decimal[3];

        /// <summary>
        /// Initializes a new instance of the <see cref="StalledPattern"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public StalledPattern(string name) 
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod),
                  Math.Max(CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod, CandleSettings.Get(CandleSettingType.Near).AveragePeriod)) + 2 + 1)
        {
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StalledPattern"/> class.
        /// </summary>
        public StalledPattern()
            : this("STALLEDPATTERN")
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
                if (Samples >= Period - _bodyLongAveragePeriod)
                {
                    _bodyLongPeriodTotal[2] += GetCandleRange(CandleSettingType.BodyLong, window[2]);
                    _bodyLongPeriodTotal[1] += GetCandleRange(CandleSettingType.BodyLong, window[1]);
                }

                if (Samples >= Period - _bodyShortAveragePeriod)
                {
                    _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                }

                if (Samples >= Period - _shadowVeryShortAveragePeriod)
                {
                    _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]);
                }

                if (Samples >= Period - _nearAveragePeriod)
                {
                    _nearPeriodTotal[2] += GetCandleRange(CandleSettingType.Near, window[2]);
                    _nearPeriodTotal[1] += GetCandleRange(CandleSettingType.Near, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st white
                GetCandleColor(window[2]) == CandleColor.White &&
                // 2nd white
                GetCandleColor(window[1]) == CandleColor.White &&
                // 3rd white
                GetCandleColor(input) == CandleColor.White &&
                // consecutive higher closes
                input.Close > window[1].Close && window[1].Close > window[2].Close &&
                // 1st: long real body
                GetRealBody(window[2]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal[2], window[2]) &&
                // 2nd: long real body
                GetRealBody(window[1]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal[1], window[1]) &&
                // very short upper shadow
                GetUpperShadow(window[1]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal, window[1]) &&
                // opens within/near 1st real body
                window[1].Open > window[2].Open &&
                window[1].Open <= window[2].Close + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[2], window[2]) &&
                // 3rd: small real body
                GetRealBody(input) < GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, input) &&
                // rides on the shoulder of 2nd real body
                input.Open >= window[1].Close - GetRealBody(input) - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[1], window[1])
              )
                value = -1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 2; i >= 1; i--)
            {
                _bodyLongPeriodTotal[i] += GetCandleRange(CandleSettingType.BodyLong, window[i]) -
                                           GetCandleRange(CandleSettingType.BodyLong, window[i + _bodyLongAveragePeriod]);
                _nearPeriodTotal[i] += GetCandleRange(CandleSettingType.Near, window[i]) -
                                       GetCandleRange(CandleSettingType.Near, window[i + _nearAveragePeriod]);
            }

            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input) - 
                                     GetCandleRange(CandleSettingType.BodyShort, window[_bodyShortAveragePeriod]);

            _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]) -
                                           GetCandleRange(CandleSettingType.ShadowVeryShort, window[_bodyShortAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = new decimal[3];
            _bodyShortPeriodTotal = 0;
            _shadowVeryShortPeriodTotal = 0;
            _nearPeriodTotal = new decimal[3];
            base.Reset();
        }
    }
}
