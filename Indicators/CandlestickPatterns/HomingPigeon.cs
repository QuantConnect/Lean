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
    /// Homing Pigeon candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long black candle
    /// - second candle: short black real body completely inside the previous day's body
    /// The meaning of "short" and "long" is specified with SetCandleSettings
    /// The returned value is positive(+1): homing pigeon is always bullish;
    /// The user should consider that homing pigeon is significant when it appears in a downtrend,
    /// while this function does not consider the trend
    /// </remarks>
    public class HomingPigeon : CandlestickPattern
    {
        private readonly int _bodyLongAveragePeriod;
        private readonly int _bodyShortAveragePeriod;

        private decimal _bodyLongPeriodTotal;
        private decimal _bodyShortPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomingPigeon"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public HomingPigeon(string name) 
            : base(name, Math.Max(CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod) + 1 + 1)
        {
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HomingPigeon"/> class.
        /// </summary>
        public HomingPigeon()
            : this("HOMINGPIGEON")
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
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[1]);
                }

                if (Samples >= Period - _bodyShortAveragePeriod)
                {
                    _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st black
                GetCandleColor(window[1]) == CandleColor.Black &&
                // 2nd black
                GetCandleColor(input) == CandleColor.Black &&
                // 1st long
                GetRealBody(window[1]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[1]) &&
                // 2nd short
                GetRealBody(input) <= GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, input) &&
                // 2nd engulfed by 1st
                input.Open < window[1].Open &&
                input.Close > window[1].Close
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[1]) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod + 1]);

            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input) -
                                     GetCandleRange(CandleSettingType.BodyShort, window[_bodyShortAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0m;
            _bodyShortPeriodTotal = 0m;
            base.Reset();
        }
    }
}
