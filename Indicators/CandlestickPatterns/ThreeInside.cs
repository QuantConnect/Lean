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
    /// Three Inside Up/Down candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long white(black) real body
    /// - second candle: short real body totally engulfed by the first
    /// - third candle: black(white) candle that closes lower(higher) than the first candle's open
    /// The meaning of "short" and "long" is specified with SetCandleSettings
    /// The returned value is positive (+1) for the three inside up or negative (-1) for the three inside down;
    /// The user should consider that a three inside up is significant when it appears in a downtrend and a three inside
    /// down is significant when it appears in an uptrend, while this function does not consider the trend
    /// </remarks>
    public class ThreeInside : CandlestickPattern
    {
        private readonly int _bodyLongAveragePeriod;
        private readonly int _bodyShortAveragePeriod;

        private decimal _bodyLongPeriodTotal;
        private decimal _bodyShortPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeInside"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ThreeInside(string name) 
            : base(name, Math.Max(CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod) + 2 + 1)
        {
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeInside"/> class.
        /// </summary>
        public ThreeInside()
            : this("THREEINSIDE")
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
                if (Samples >= Period - _bodyLongAveragePeriod - 2 && Samples < Period - 2)
                {
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, input);
                }

                if (Samples >= Period - _bodyShortAveragePeriod - 1 && Samples < Period - 1)
                {
                    _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st: long
                GetRealBody(window[2]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[2]) &&
                // 2nd: short
                GetRealBody(window[1]) <= GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, window[1]) &&
                //      engulfed by 1st
                Math.Max(window[1].Close, window[1].Open) < Math.Max(window[2].Close, window[2].Open) &&
                Math.Min(window[1].Close, window[1].Open) > Math.Min(window[2].Close, window[2].Open) &&
                // 3rd: opposite to 1st
                ((GetCandleColor(window[2]) == CandleColor.White && GetCandleColor(input) == CandleColor.Black && input.Close < window[2].Open) ||
                  //      and closing out
                  (GetCandleColor(window[2]) == CandleColor.Black && GetCandleColor(input) == CandleColor.White && input.Close > window[2].Open)
                )
              )
                value = -(int)GetCandleColor(window[2]);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[2 + _bodyLongAveragePeriod]);

            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, window[1]) -
                                     GetCandleRange(CandleSettingType.BodyShort, window[1 + _bodyShortAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0;
            _bodyShortPeriodTotal = 0;
            base.Reset();
        }
    }
}
