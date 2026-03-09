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
    /// Hanging Man candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - small real body
    /// - long lower shadow
    /// - no, or very short, upper shadow
    /// - body above or near the highs of the previous candle
    /// The meaning of "short", "long" and "near the highs" is specified with SetCandleSettings;
    /// The returned value is negative (-1): hanging man is always bearish;
    /// The user should consider that a hanging man must appear in an uptrend, while this function does not consider it
    /// </remarks>
    public class HangingMan : CandlestickPattern
    {
        private readonly int _bodyShortAveragePeriod;
        private readonly int _shadowLongAveragePeriod;
        private readonly int _shadowVeryShortAveragePeriod;
        private readonly int _nearAveragePeriod;

        private decimal _bodyShortPeriodTotal;
        private decimal _shadowLongPeriodTotal;
        private decimal _shadowVeryShortPeriodTotal;
        private decimal _nearPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="HangingMan"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public HangingMan(string name) 
            : base(name, Math.Max(Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod, CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod),
                CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod), CandleSettings.Get(CandleSettingType.Near).AveragePeriod) + 1 + 1)
        {
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _shadowLongAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod;
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HangingMan"/> class.
        /// </summary>
        public HangingMan()
            : this("HANGINGMAN")
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
                if (Samples >= Period - _bodyShortAveragePeriod)
                {
                    _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                }

                if (Samples >= Period - _shadowLongAveragePeriod)
                {
                    _shadowLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowLong, input);
                }

                if (Samples >= Period - _shadowVeryShortAveragePeriod)
                {
                    _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, input);
                }

                if (Samples >= Period - _nearAveragePeriod - 1 && Samples < Period - 1)
                {
                    _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // small rb
                GetRealBody(input) < GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, input) &&
                // long lower shadow
                GetLowerShadow(input) > GetCandleAverage(CandleSettingType.ShadowLong, _shadowLongPeriodTotal, input) &&
                // very short upper shadow
                GetUpperShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal, input) &&
                // rb near the prior candle's highs
                Math.Min(input.Close, input.Open) >= window[1].High - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[1])
              )
                value = -1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input) -
                                    GetCandleRange(CandleSettingType.BodyShort, window[_bodyShortAveragePeriod]);

            _shadowLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowLong, input) -
                                      GetCandleRange(CandleSettingType.ShadowLong, window[_shadowLongAveragePeriod]);

            _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, input) -
                                           GetCandleRange(CandleSettingType.ShadowVeryShort, window[_shadowVeryShortAveragePeriod]);

            _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, window[1]) -
                                GetCandleRange(CandleSettingType.Near, window[_nearAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyShortPeriodTotal = 0m;
            _shadowLongPeriodTotal = 0m;
            _shadowVeryShortPeriodTotal = 0m;
            _nearPeriodTotal = 0m;
            base.Reset();
        }
    }
}
