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
    /// Hikkake Modified candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle
    /// - second candle: candle with range less than first candle and close near the bottom(near the top)
    /// - third candle: lower high and higher low than 2nd
    /// - fourth candle: lower high and lower low(higher high and higher low) than 3rd
    /// The returned value for the hikkake bar is positive(+1) or negative(-1) meaning bullish or bearish hikkake
    /// Confirmation could come in the next 3 days with:
    /// - a day that closes higher than the high(lower than the low) of the 3rd candle
    /// The returned value for the confirmation bar is equal to 1 + the bullish hikkake result or -1 - the bearish hikkake result
    /// Note: if confirmation and a new hikkake come at the same bar, only the new hikkake is reported(the new hikkake
    /// overwrites the confirmation of the old hikkake);
    /// The user should consider that modified hikkake is a reversal pattern, while hikkake could be both a reversal
    /// or a continuation pattern, so bullish(bearish) modified hikkake is significant when appearing in a downtrend(uptrend)
    /// </remarks>
    public class HikkakeModified : CandlestickPattern
    {
        private readonly int _nearAveragePeriod;

        private decimal _nearPeriodTotal;

        private int _patternIndex;
        private int _patternResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="HikkakeModified"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public HikkakeModified(string name) 
            : base(name, Math.Max(1, CandleSettings.Get(CandleSettingType.Near).AveragePeriod) + 5 + 1)
        {
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HikkakeModified"/> class.
        /// </summary>
        public HikkakeModified()
            : this("HIKKAKEMODIFIED")
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
                if (Samples >= Period - _nearAveragePeriod - 3 && Samples < Period - 3)
                {
                    _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, window[2]);
                }

                else if (Samples >= Period - 3)
                {
                        // copy here the pattern recognition code below
                        // 2nd: lower high and higher low than 1st
                        if (window[2].High < window[3].High && window[2].Low > window[3].Low &&
                        // 3rd: lower high and higher low than 2nd
                        window[1].High < window[2].High && window[1].Low > window[2].Low &&
                        // (bull) 4th: lower high and lower low
                        ((input.High < window[1].High && input.Low < window[1].Low &&
                          // (bull) 2nd: close near the low
                          window[2].Close <= window[2].Low + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[2])
                            )
                         ||
                         // (bear) 4th: higher high and higher low
                         (input.High > window[1].High && input.Low > window[1].Low &&
                          // (bull) 2nd: close near the top
                          window[2].Close >= window[2].High - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[2])
                             )
                            )
                        )
                    {
                        _patternResult = (input.High < window[1].High ? 1 : -1);
                        _patternIndex = (int) Samples - 1;
                    }
                    else
                    {
                        // search for confirmation if modified hikkake was no more than 3 bars ago
                        if (Samples <= _patternIndex + 4 &&
                            // close higher than the high of 3rd
                            ((_patternResult > 0 && input.Close > window[(int) Samples - _patternIndex].High)
                             ||
                             // close lower than the low of 3rd
                             (_patternResult < 0 && input.Close < window[(int) Samples - _patternIndex].Low))
                                )
                            _patternIndex = 0;
                    }

                    // add the current range and subtract the first range: this is done after the pattern recognition 
                    // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

                    _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, window[2]) -
                                        GetCandleRange(CandleSettingType.Near, window[(int)Samples - 1]);
                }

                return 0m;
            }

            decimal value;
            // 2nd: lower high and higher low than 1st
            if (window[2].High < window[3].High && window[2].Low > window[3].Low &&
                // 3rd: lower high and higher low than 2nd
                window[1].High < window[2].High && window[1].Low > window[2].Low &&
                // (bull) 4th: lower high and lower low
                ((input.High < window[1].High && input.Low < window[1].Low &&
                  // (bull) 2nd: close near the low
                  window[2].Close <= window[2].Low + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[2])
                    )
                 ||
                 // (bear) 4th: higher high and higher low
                 (input.High > window[1].High && input.Low > window[1].Low &&
                  // (bull) 2nd: close near the top
                  window[2].Close >= window[2].High - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, window[2])
                     )
                    )
                )
            {
                _patternResult = (input.High < window[1].High ? 1 : -1);
                _patternIndex = (int) Samples - 1;
                value = _patternResult;
            }
            else
            {
                // search for confirmation if modified hikkake was no more than 3 bars ago
                if (Samples <= _patternIndex + 4 &&
                    // close higher than the high of 3rd
                    ((_patternResult > 0 && input.Close > window[(int)Samples - _patternIndex].High)
                     ||
                     // close lower than the low of 3rd
                     (_patternResult < 0 && input.Close < window[(int)Samples - _patternIndex].Low))
                        )
                {
                    value = _patternResult + (_patternResult > 0 ? 1 : -1);
                    _patternIndex = 0;
                }
                else
                    value = 0;
            }

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, window[2]) -
                                GetCandleRange(CandleSettingType.Near, window[_nearAveragePeriod + 5]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _nearPeriodTotal = 0;
            _patternIndex = 0;
            _patternResult = 0;
            base.Reset();
        }
    }
}
