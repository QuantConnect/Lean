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

namespace QuantConnect.Indicators.CandlestickPatterns
{
    /// <summary>
    /// Three Black Crows candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - three consecutive and declining black candlesticks
    /// - each candle must have no or very short lower shadow
    /// - each candle after the first must open within the prior candle's real body
    /// - the first candle's close should be under the prior white candle's high
    /// The meaning of "very short" is specified with SetCandleSettings
    /// The returned value is negative (-1): three black crows is always bearish;
    /// The user should consider that 3 black crows is significant when it appears after a mature advance or at high levels,
    /// while this function does not consider it
    /// </remarks>
    public class ThreeBlackCrows : CandlestickPattern
    {
        private readonly int _shadowVeryShortAveragePeriod;

        private decimal[] _shadowVeryShortPeriodTotal = new decimal[3];

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeBlackCrows"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ThreeBlackCrows(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod + 3 + 1)
        {
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeBlackCrows"/> class.
        /// </summary>
        public ThreeBlackCrows()
            : this("THREEBLACKCROWS")
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

                return 0m;
            }

            decimal value;
            if (
                // white
                GetCandleColor(window[3]) == CandleColor.White &&
                // 1st black
                GetCandleColor(window[2]) == CandleColor.Black &&
                // very short lower shadow
                GetLowerShadow(window[2]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[2], window[2]) &&
                // 2nd black
                GetCandleColor(window[1]) == CandleColor.Black &&
                // very short lower shadow
                GetLowerShadow(window[1]) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[1], window[1]) &&
                // 3rd black
                GetCandleColor(input) == CandleColor.Black &&
                // very short lower shadow
                GetLowerShadow(input) < GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal[0], input) &&
                // 2nd black opens within 1st black's rb
                window[1].Open < window[2].Open && window[1].Open > window[2].Close &&
                // 3rd black opens within 2nd black's rb
                input.Open < window[1].Open && input.Open > window[1].Close &&
                // 1st black closes under prior candle's high
                window[3].High > window[2].Close &&
                // three declining
                window[2].Close > window[1].Close &&
                // three declining
                window[1].Close > input.Close
              )
                value = -1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 2; i >= 0; i--)
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
            _shadowVeryShortPeriodTotal = new decimal[3];
            base.Reset();
        }
    }
}
