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
    /// Abandoned Baby candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long white (black) real body
    /// - second candle: doji
    /// - third candle: black(white) real body that moves well within the first candle's real body
    /// - upside(downside) gap between the first candle and the doji(the shadows of the two candles don't touch)
    /// - downside (upside) gap between the doji and the third candle(the shadows of the two candles don't touch)
    /// The meaning of "doji" and "long" is specified with SetCandleSettings
    /// The meaning of "moves well within" is specified with penetration and "moves" should mean the real body should
    /// not be short ("short" is specified with SetCandleSettings) - Greg Morris wants it to be long, someone else want
    /// it to be relatively long
    /// The returned value is positive (+1) when it's an abandoned baby bottom or negative (-1) when it's
    /// an abandoned baby top; the user should consider that an abandoned baby is significant when it appears in 
    /// an uptrend or downtrend, while this function does not consider the trend
    /// </remarks>
    public class AbandonedBaby : CandlestickPattern
    {
        private readonly decimal _penetration;

        private readonly int _bodyDojiAveragePeriod;
        private readonly int _bodyLongAveragePeriod;
        private readonly int _bodyShortAveragePeriod;

        private decimal _bodyDojiPeriodTotal;
        private decimal _bodyLongPeriodTotal;
        private decimal _bodyShortPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbandonedBaby"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        public AbandonedBaby(string name, decimal penetration = 0.3m) 
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod),
                  CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod) + 2)
        {
            _penetration = penetration;

            _bodyDojiAveragePeriod = CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod;
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _bodyShortAveragePeriod = CandleSettings.Get(CandleSettingType.BodyShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbandonedBaby"/> class.
        /// </summary>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        public AbandonedBaby(decimal penetration)
            : this("ABANDONEDBABY", penetration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbandonedBaby"/> class.
        /// </summary>
        public AbandonedBaby()
            : this("ABANDONEDBABY")
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
                if (Samples > Period - _bodyLongAveragePeriod)
                {
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]);
                }
                if (Samples > Period - _bodyDojiAveragePeriod)
                {
                    _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, window[1]);
                }
                if (Samples > Period - _bodyShortAveragePeriod)
                {
                    _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st: long
                GetRealBody(window[2]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[2]) &&
                // 2nd: doji
                GetRealBody(window[1]) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, window[1]) &&
                // 3rd: longer than short
                GetRealBody(input) > GetCandleAverage(CandleSettingType.BodyShort, _bodyShortPeriodTotal, input) &&
                ((
                    // 1st white
                    GetCandleColor(window[2]) == CandleColor.White &&
                    // 3rd black
                    GetCandleColor(input) == CandleColor.Black &&
                    // 3rd closes well within 1st rb
                    input.Close < window[2].Close - GetRealBody(window[2]) * _penetration &&
                    // upside gap between 1st and 2nd
                    GetCandleGapUp(window[1], window[2]) &&
                    // downside gap between 2nd and 3rd
                    GetCandleGapDown(input, window[1])
                  )
                  ||
                  (
                    // 1st black
                    GetCandleColor(window[2]) == CandleColor.Black &&
                    // 3rd white
                    GetCandleColor(input) == CandleColor.White &&
                    // 3rd closes well within 1st rb
                    input.Close > window[2].Close + GetRealBody(window[2]) * _penetration &&
                    // downside gap between 1st and 2nd
                    GetCandleGapDown(window[1], window[2]) &&
                    // upside gap between 2nd and 3rd
                    GetCandleGapUp(input, window[1])
                  )
                )
              )
                value = (int)GetCandleColor(input);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod - 1]);

            _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, window[1]) -
                                    GetCandleRange(CandleSettingType.BodyDoji, window[_bodyDojiAveragePeriod]);

            _bodyShortPeriodTotal += GetCandleRange(CandleSettingType.BodyShort, input) -
                                     GetCandleRange(CandleSettingType.BodyShort, window[_bodyShortAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0;
            _bodyDojiPeriodTotal = 0;
            _bodyShortPeriodTotal = 0;
            base.Reset();
        }
    }
}
