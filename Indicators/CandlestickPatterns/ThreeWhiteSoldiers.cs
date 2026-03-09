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
    /// Three Advancing White Soldiers candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - three white candlesticks with consecutively higher closes
    /// - Greg Morris wants them to be long, Steve Nison doesn't; anyway they should not be short
    /// - each candle opens within or near the previous white real body
    /// - each candle must have no or very short upper shadow
    /// - to differentiate this pattern from advance block, each candle must not be far shorter than the prior candle
    /// The meanings of "not short", "very short shadow", "far" and "near" are specified with SetCandleSettings;
    /// here the 3 candles must be not short, if you want them to be long use SetCandleSettings on BodyShort;
    /// The returned value is positive (+1): advancing 3 white soldiers is always bullish;
    /// The user should consider that 3 white soldiers is significant when it appears in downtrend, while this function 
    /// does not consider it
    /// </remarks>
    public class ThreeWhiteSoldiers : CandlestickPattern
    {
        private readonly int _shadowVeryShortAveragePeriod;
        private readonly int _nearAveragePeriod;
        private readonly int _farAveragePeriod;
        private readonly int _bodyShortAveragePeriod;

        private decimal[] _shadowVeryShortPeriodTotal = new decimal[3];
        private decimal[] _nearPeriodTotal = new decimal[3];
        private decimal[] _farPeriodTotal = new decimal[3];
        private decimal _bodyShortPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeWhiteSoldiers"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ThreeWhiteSoldiers(string name) 
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod),
                  Math.Max(CandleSettings.Get(CandleSettingType.Far).AveragePeriod, CandleSettings.Get(CandleSettingType.Near).AveragePeriod)) + 2 + 1)
        {
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
            _farAveragePeriod = CandleSettings.Get(CandleSettingType.Far).AveragePeriod;
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeWhiteSoldiers"/> class.
        /// </summary>
        public ThreeWhiteSoldiers()
            : this("THREEWHITESOLDIERS")
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
                    _shadowVeryShortPeriodTotal[2] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[2]);
                    _shadowVeryShortPeriodTotal[1] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]);
                    _shadowVeryShortPeriodTotal[0] += GetCandleRange(CandleSettingType.ShadowVeryShort, input);
                }

                if (Samples >= Period - _nearAveragePeriod)
                {
                    _nearPeriodTotal[2] += GetCandleRange(CandleSettingType.Near, window[2]);
                    _nearPeriodTotal[1] += GetCandleRange(CandleSettingType.Near, window[1]);
                }

                if (Samples >= Period - _farAveragePeriod)
                {
                    _farPeriodTotal[2] += GetCandleRange(CandleSettingType.Far, window[2]);
                    _farPeriodTotal[1] += GetCandleRange(CandleSettingType.Far, window[1]);
                }

                if (Samples >= Period - _bodyShortAveragePeriod)
                {
                    _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st white
                GetCandleColor(window[2]) == CandleColor.White &&
                // very short upper shadow
                GetUpperShadow(window[2]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[2], window[2]) &&
                // 2nd white
                GetCandleColor(window[1]) == CandleColor.White &&
                // very short upper shadow
                GetUpperShadow(window[1]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[1], window[1]) &&
                // 3rd white
                GetCandleColor(input) == CandleColor.White &&
                // very short upper shadow
                GetUpperShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[0], input) &&
                // consecutive higher closes
                input.Close > window[1].Close && window[1].Close > window[2].Close &&
                // 2nd opens within/near 1st real body
                window[1].Open > window[2].Open &&
                window[1].Open <= window[2].Close + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[2], window[2]) &&
                // 3rd opens within/near 2nd real body
                input.Open > window[1].Open &&
                input.Open <= window[1].Close + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[1], window[1]) &&
                // 2nd not far shorter than 1st
                GetRealBody(window[1]) > GetRealBody(window[2]) - GetCandleAverage(CandleSettingType.Far, _farPeriodTotal[2], window[2]) &&
                // 3rd not far shorter than 2nd
                GetRealBody(input) > GetRealBody(window[1]) - GetCandleAverage(CandleSettingType.Far, _farPeriodTotal[1], window[1]) &&
                // not short real body
                GetRealBody(input) > GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, input)
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 2; i >= 0; i--)
            {
                _shadowVeryShortPeriodTotal[i] += GetCandleRange(CandleSettingType.ShadowVeryShort, window[i]) -
                                                  GetCandleRange(CandleSettingType.ShadowVeryShort, window[i + _shadowVeryShortAveragePeriod]);
            }

            for (var i = 2; i >= 1; i--)
            {
                _farPeriodTotal[i] += GetCandleRange(CandleSettingType.Far, window[i]) -
                                      GetCandleRange(CandleSettingType.Far, window[i + _farAveragePeriod]);
            }

            for (var i = 2; i >= 1; i--)
            {
                _nearPeriodTotal[i] += GetCandleRange(CandleSettingType.Near, window[i]) -
                                       GetCandleRange(CandleSettingType.Near, window[i + _nearAveragePeriod]);
            }

            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input) -
                                     GetCandleRange(CandleSettingType.BodyShort, window[_bodyShortAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _shadowVeryShortPeriodTotal = new decimal[3];
            _nearPeriodTotal = new decimal[3];
            _farPeriodTotal = new decimal[3];
            _bodyShortPeriodTotal = 0;
            base.Reset();
        }
    }
}
