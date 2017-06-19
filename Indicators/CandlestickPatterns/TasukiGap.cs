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
    /// Tasuki Gap candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - upside (downside) gap
    /// - first candle after the window: white(black) candlestick
    /// - second candle: black(white) candlestick that opens within the previous real body and closes under(above)
    /// the previous real body inside the gap
    /// - the size of two real bodies should be near the same
    /// The meaning of "near" is specified with SetCandleSettings
    /// The returned value is positive(+1) when bullish or negative(-1) when bearish;
    /// The user should consider that tasuki gap is significant when it appears in a trend, while this function does 
    /// not consider it
    /// </remarks>
    public class TasukiGap : CandlestickPattern
    {
        private readonly int _nearAveragePeriod;

        private decimal _nearPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="TasukiGap"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public TasukiGap(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.Near).AveragePeriod + 2 + 1)
        {
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TasukiGap"/> class.
        /// </summary>
        public TasukiGap()
            : this("TASUKIGAP")
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
                if (Samples >= Period - _nearAveragePeriod)
                {
                    _nearPeriodTotal += GetCandleRange(CandleSettingType.Equal, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                (
                    // upside gap    
                    GetRealBodyGapUp(window[1], window[2]) &&
                    // 1st: white
                    GetCandleColor(window[1]) == CandleColor.White &&
                    // 2nd: black
                    GetCandleColor(input) == CandleColor.Black &&
                    //      that opens within the white rb
                    input.Open < window[1].Close && input.Open > window[1].Open &&
                    //      and closes under the white rb
                    input.Close < window[1].Open &&
                    //      inside the gap
                    input.Close > Math.Max(window[2].Close, window[2].Open) &&
                    // size of 2 rb near the same
                    Math.Abs(GetRealBody(window[1]) - GetRealBody(input)) < GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[1])
                ) ||
                (
                    // downside gap
                    GetRealBodyGapDown(window[1], window[2]) &&
                    // 1st: black
                    GetCandleColor(window[1]) == CandleColor.Black &&
                    // 2nd: white
                    GetCandleColor(input) == CandleColor.White &&
                    //      that opens within the black rb
                    input.Open < window[1].Open && input.Open > window[1].Close &&
                    //      and closes above the black rb
                    input.Close > window[1].Open &&
                    //      inside the gap
                    input.Close < Math.Min(window[2].Close, window[2].Open) &&
                    // size of 2 rb near the same
                    Math.Abs(GetRealBody(window[1]) - GetRealBody(input)) < GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[1])
                )
              )
                value = (int)GetCandleColor(window[1]);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, window[1]) -
                                GetCandleRange(CandleSettingType.Near, window[_nearAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _nearPeriodTotal = 0m;
            base.Reset();
        }
    }
}
