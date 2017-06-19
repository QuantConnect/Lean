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
    /// Takuri (Dragonfly Doji with very long lower shadow) candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - doji body
    /// - open and close at the high of the day = no or very short upper shadow
    /// - very long lower shadow
    /// The meaning of "doji", "very short" and "very long" is specified with SetCandleSettings
    /// The returned value is always positive(+1) but this does not mean it is bullish: takuri must be considered
    /// relatively to the trend
    /// </remarks>
    public class Takuri : CandlestickPattern
    {
        private readonly int _bodyDojiAveragePeriod;
        private readonly int _shadowVeryShortAveragePeriod;
        private readonly int _shadowVeryLongAveragePeriod;

        private decimal _bodyDojiPeriodTotal;
        private decimal _shadowVeryShortPeriodTotal;
        private decimal _shadowVeryLongPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Takuri"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public Takuri(string name) 
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod, CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod),
                CandleSettings.Get(CandleSettingType.ShadowVeryLong).AveragePeriod) + 1)
        {
            _bodyDojiAveragePeriod = CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod;
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
            _shadowVeryLongAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryLong).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Takuri"/> class.
        /// </summary>
        public Takuri()
            : this("TAKURI")
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
                if (Samples >= Period - _bodyDojiAveragePeriod)
                {
                    _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input);
                }

                if (Samples >= Period - _shadowVeryShortAveragePeriod)
                {
                    _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, input);
                }

                if (Samples >= Period - _shadowVeryLongAveragePeriod)
                {
                    _shadowVeryLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryLong, input);
                }

                return 0m;
            }

            decimal value;
            if (GetRealBody(input) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, input) &&
                GetUpperShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal, input) &&
                GetLowerShadow(input) > GetCandleAverage(CandleSettingType.ShadowVeryLong, _shadowVeryLongPeriodTotal, input)
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input) -
                                    GetCandleRange(CandleSettingType.BodyDoji, window[_bodyDojiAveragePeriod]);

            _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, input) -
                                           GetCandleRange(CandleSettingType.ShadowVeryShort, window[_shadowVeryShortAveragePeriod]);

            _shadowVeryLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryLong, input) -
                                          GetCandleRange(CandleSettingType.ShadowVeryLong, window[_shadowVeryLongAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyDojiPeriodTotal = 0m;
            _shadowVeryShortPeriodTotal = 0m;
            _shadowVeryLongPeriodTotal = 0m;
            base.Reset();
        }
    }
}
