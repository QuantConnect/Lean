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
    /// Rising/Falling Three Methods candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long white (black) candlestick
    /// - then: group of falling(rising) small real body candlesticks(commonly black (white)) that hold within
    /// the prior long candle's range: ideally they should be three but two or more than three are ok too
    /// - final candle: long white(black) candle that opens above(below) the previous small candle's close 
    /// and closes above(below) the first long candle's close
    /// The meaning of "short" and "long" is specified with SetCandleSettings; here only patterns with 3 small candles
    /// are considered;
    /// The returned value is positive(+1) or negative(-1)
    /// </remarks>
    public class RiseFallThreeMethods : CandlestickPattern
    {
        private readonly int _bodyShortAveragePeriod;
        private readonly int _bodyLongAveragePeriod;

        private decimal[] _bodyPeriodTotal = new decimal[5];

        /// <summary>
        /// Initializes a new instance of the <see cref="RiseFallThreeMethods"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public RiseFallThreeMethods(string name)
            : base(name, Math.Max(CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod) + 4 + 1)
        {
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RiseFallThreeMethods"/> class.
        /// </summary>
        public RiseFallThreeMethods()
            : this("RISEFALLTHREEMETHODS")
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
                    _bodyPeriodTotal[0] += GetCandleRange(CandleSettingType.BodyLong, input);
                }

                return 0m;
            }

            decimal value;
            if ( 
                // 1st long, then 3 small, 5th long
                GetRealBody(window[4]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyPeriodTotal[4], window[4]) &&
                GetRealBody(window[3]) < GetCandleAverage(CandleSettingType.BodyShort, _bodyPeriodTotal[3], window[3]) &&
                GetRealBody(window[2]) < GetCandleAverage(CandleSettingType.BodyShort, _bodyPeriodTotal[2], window[2]) &&
                GetRealBody(window[1]) < GetCandleAverage(CandleSettingType.BodyShort, _bodyPeriodTotal[1], window[1]) &&
                GetRealBody(input) > GetCandleAverage(CandleSettingType.BodyLong, _bodyPeriodTotal[0], input) &&
                // white, 3 black, white  ||  black, 3 white, black
                (int)GetCandleColor(window[4]) == -(int)GetCandleColor(window[3]) &&
                GetCandleColor(window[3]) == GetCandleColor(window[2]) &&
                GetCandleColor(window[2]) == GetCandleColor(window[1]) &&
                (int)GetCandleColor(window[1]) == -(int)GetCandleColor(input) &&
                // 2nd to 4th hold within 1st: a part of the real body must be within 1st range
                Math.Min(window[3].Open, window[3].Close) < window[4].High && Math.Max(window[3].Open, window[3].Close) > window[4].Low &&
                Math.Min(window[2].Open, window[2].Close) < window[4].High && Math.Max(window[2].Open, window[2].Close) > window[4].Low &&
                Math.Min(window[1].Open, window[1].Close) < window[4].High && Math.Max(window[1].Open, window[1].Close) > window[4].Low &&
                // 2nd to 4th are falling (rising)
                window[2].Close * (int)GetCandleColor(window[4]) < window[3].Close * (int)GetCandleColor(window[4]) &&
                window[1].Close * (int)GetCandleColor(window[4]) < window[2].Close * (int)GetCandleColor(window[4]) &&
                // 5th opens above (below) the prior close
                input.Open * (int)GetCandleColor(window[4]) > window[1].Close * (int)GetCandleColor(window[4]) &&
                // 5th closes above (below) the 1st close
                input.Close * (int)GetCandleColor(window[4]) > window[4].Close * (int)GetCandleColor(window[4])
              )
                value = (int)GetCandleColor(window[4]);
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

            _bodyPeriodTotal[0] += GetCandleRange(CandleSettingType.BodyLong, input) -
                                   GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod]);

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
