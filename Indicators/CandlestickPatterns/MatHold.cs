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
    /// Mat Hold candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long white candle
    /// - upside gap between the first and the second bodies
    /// - second candle: small black candle
    /// - third and fourth candles: falling small real body candlesticks(commonly black) that hold within the long
    /// white candle's body and are higher than the reaction days of the rising three methods
    /// - fifth candle: white candle that opens above the previous small candle's close and closes higher than the 
    /// high of the highest reaction day
    /// The meaning of "short" and "long" is specified with SetCandleSettings; 
    /// "hold within" means "a part of the real body must be within";
    /// penetration is the maximum percentage of the first white body the reaction days can penetrate(it is 
    /// to specify how much the reaction days should be "higher than the reaction days of the rising three methods")
    /// The returned value is positive(+1): mat hold is always bullish
    /// </remarks>
    public class MatHold : CandlestickPattern
    {
        private readonly decimal _penetration;

        private readonly int _bodyShortAveragePeriod;
        private readonly int _bodyLongAveragePeriod;

        private decimal[] _bodyPeriodTotal = new decimal[5];

        /// <summary>
        /// Initializes a new instance of the <see cref="MatHold"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        public MatHold(string name, decimal penetration = 0.5m) 
            : base(name, Math.Max(CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod) + 4 + 1)
        {
            _penetration = penetration;

            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatHold"/> class.
        /// </summary>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        public MatHold(decimal penetration)
            : this("MATHOLD", penetration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatHold"/> class.
        /// </summary>
        public MatHold()
            : this("MATHOLD")
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
        protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar input)
        {
            if (!IsReady)
            {
                if (Samples > Period - _bodyShortAveragePeriod)
                {
                    _bodyPeriodTotal[3] += GetCandleRange(CandleSettingType.BodyShort, window[3]);
                    _bodyPeriodTotal[2] += GetCandleRange(CandleSettingType.BodyShort, window[2]);
                    _bodyPeriodTotal[1] += GetCandleRange(CandleSettingType.BodyShort, window[1]);
                }

                if (Samples > Period - _bodyLongAveragePeriod)
                {
                    _bodyPeriodTotal[4] += GetCandleRange(CandleSettingType.BodyLong, window[4]);
                }

                return 0m;
            }

            decimal value;
            if ( 
                // 1st long, then 3 small
                GetRealBody(window[4]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyPeriodTotal[4], window[4]) &&
                GetRealBody(window[3]) < GetCandleAverage(CandleSettingType.BodyShort, _bodyPeriodTotal[3], window[3]) &&
                GetRealBody(window[2]) < GetCandleAverage(CandleSettingType.BodyShort, _bodyPeriodTotal[2], window[2]) &&
                GetRealBody(window[1]) < GetCandleAverage(CandleSettingType.BodyShort, _bodyPeriodTotal[1], window[1]) &&
                // white, black, 2 black or white, white
                GetCandleColor(window[4]) == CandleColor.White &&
                GetCandleColor(window[3]) == CandleColor.Black &&
                GetCandleColor(input) == CandleColor.White &&
                // upside gap 1st to 2nd
                GetRealBodyGapUp(window[3], window[4]) &&
                // 3rd to 4th hold within 1st: a part of the real body must be within 1st real body
                Math.Min(window[2].Open, window[2].Close) < window[4].Close &&
                Math.Min(window[1].Open, window[1].Close) < window[4].Close &&
                // reaction days penetrate first body less than optInPenetration percent
                Math.Min(window[2].Open, window[2].Close) > window[4].Close - GetRealBody(window[4]) * _penetration &&
                Math.Min(window[1].Open, window[1].Close) > window[4].Close - GetRealBody(window[4]) * _penetration &&
                // 2nd to 4th are falling
                Math.Max(window[2].Close, window[2].Open) < window[3].Open &&
                Math.Max(window[1].Close, window[1].Open) < Math.Max(window[2].Close, window[2].Open) &&
                // 5th opens above the prior close
                input.Open > window[1].Close &&
                // 5th closes above the highest high of the reaction days
                input.Close > Math.Max(Math.Max(window[3].High, window[2].High), window[1].High)
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyPeriodTotal[4] += GetCandleRange(CandleSettingType.BodyLong, window[4]) -
                                   GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod + 4]);

            for (var i = 3; i >= 1; i--)
            {
                _bodyPeriodTotal[i] += GetCandleRange(CandleSettingType.BodyShort, window[i]) -
                                       GetCandleRange(CandleSettingType.BodyShort, window[i + _bodyShortAveragePeriod]);
            }

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyPeriodTotal = new decimal[5];
            base.Reset();
        }
    }
}
