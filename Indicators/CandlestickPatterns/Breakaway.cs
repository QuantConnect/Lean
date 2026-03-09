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
    /// Breakaway candlestick pattern indicator
    /// </summary>
    /// <remarks>
    ///  Must have:
    /// - first candle: long black(white)
    /// - second candle: black(white) day whose body gaps down(up)
    /// - third candle: black or white day with lower(higher) high and lower(higher) low than prior candle's
    /// - fourth candle: black(white) day with lower(higher) high and lower(higher) low than prior candle's
    /// - fifth candle: white(black) day that closes inside the gap, erasing the prior 3 days
    /// The meaning of "long" is specified with SetCandleSettings
    /// The returned value is positive(+1) when bullish or negative(-1) when bearish;
    /// The user should consider that breakaway is significant in a trend opposite to the last candle, while this
    /// function does not consider it
    /// </remarks>
    public class Breakaway : CandlestickPattern
    {
        private readonly int _bodyLongAveragePeriod;

        private decimal _bodyLongPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Breakaway"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public Breakaway(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod + 4 + 1)
        {
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Breakaway"/> class.
        /// </summary>
        public Breakaway()
            : this("BREAKAWAY")
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
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[4]);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st long
                GetRealBody(window[4]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[4]) &&
                // 1st, 2nd, 4th same color, 5th opposite
                GetCandleColor(window[4]) == GetCandleColor(window[3]) &&
                GetCandleColor(window[3]) == GetCandleColor(window[1]) &&
                (int)GetCandleColor(window[1]) == -(int)GetCandleColor(input) &&
                (
                  (
                    // when 1st is black:
                    GetCandleColor(window[4]) == CandleColor.Black &&
                    // 2nd gaps down
                    GetRealBodyGapDown(window[3], window[4]) &&
                    // 3rd has lower high and low than 2nd
                    window[2].High < window[3].High && window[2].Low < window[3].Low &&
                    // 4th has lower high and low than 3rd
                    window[1].High < window[2].High && window[1].Low < window[2].Low &&
                    // 5th closes inside the gap
                    input.Close > window[3].Open && input.Close < window[4].Close
                  )
                  ||
                  (
                    // when 1st is white:
                    GetCandleColor(window[4]) == CandleColor.White &&
                    // 2nd gaps up
                    GetRealBodyGapUp(window[3], window[4]) &&
                    // 3rd has higher high and low than 2nd
                    window[2].High > window[3].High && window[2].Low > window[3].Low &&
                    // 4th has higher high and low than 3rd
                    window[1].High > window[2].High && window[1].Low > window[2].Low &&
                    // 5th closes inside the gap
                    input.Close < window[3].Open && input.Close > window[4].Close
                  )
                )
              )
                value = (int)GetCandleColor(input);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[4]) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[4 + _bodyLongAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0m;
            base.Reset();
        }
    }
}
