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
    /// Harami Cross candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long white (black) real body
    /// - second candle: doji totally engulfed by the first
    /// The meaning of "doji" and "long" is specified with SetCandleSettings
    /// The returned value is positive(+1) when bullish or negative(-1) when bearish;
    /// The user should consider that a harami cross is significant when it appears in a downtrend if bullish or
    /// in an uptrend when bearish, while this function does not consider the trend
    /// </remarks>
    public class HaramiCross : CandlestickPattern
    {
        private readonly int _bodyLongAveragePeriod;
        private readonly int _bodyDojiAveragePeriod;

        private decimal _bodyLongPeriodTotal;
        private decimal _bodyDojiPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="HaramiCross"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public HaramiCross(string name) 
            : base(name, Math.Max(CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod, CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod) + 1 + 1)
        {
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
            _bodyDojiAveragePeriod = CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HaramiCross"/> class.
        /// </summary>
        public HaramiCross()
            : this("HARAMICROSS")
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
                if (Samples >= Period - _bodyLongAveragePeriod - 1 && Samples < Period - 1)
                {
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, input);
                }

                if (Samples >= Period - _bodyDojiAveragePeriod)
                {
                    _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st: long
                GetRealBody(window[1]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[1]) &&
                // 2nd: doji
                GetRealBody(input) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, input) &&
                //      engulfed by 1st
                Math.Max(input.Close, input.Open) < Math.Max(window[1].Close, window[1].Open) &&
                Math.Min(input.Close, input.Open) > Math.Min(window[1].Close, window[1].Open)
              )
                value = -(int)GetCandleColor(window[1]);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[1]) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod + 1]);

            _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input) -
                                     GetCandleRange(CandleSettingType.BodyDoji, window[_bodyDojiAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0m;
            _bodyDojiPeriodTotal = 0m;
            base.Reset();
        }
    }
}
