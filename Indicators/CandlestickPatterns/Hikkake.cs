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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators.CandlestickPatterns
{
    /// <summary>
    /// Hikkake candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first and second candle: inside bar (2nd has lower high and higher low than 1st)
    /// - third candle: lower high and lower low than 2nd(higher high and higher low than 2nd)
    /// The returned value for the hikkake bar is positive(+1) or negative(-1) meaning bullish or bearish hikkake
    /// Confirmation could come in the next 3 days with:
    /// - a day that closes higher than the high(lower than the low) of the 2nd candle
    /// The returned value for the confirmation bar is equal to 1 + the bullish hikkake result or -1 - the bearish hikkake result
    /// Note: if confirmation and a new hikkake come at the same bar, only the new hikkake is reported(the new hikkake
    /// overwrites the confirmation of the old hikkake)
    /// </remarks>
    public class Hikkake : CandlestickPattern
    {
        private int _patternIndex;
        private int _patternResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hikkake"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public Hikkake(string name) 
            : base(name, 5 + 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hikkake"/> class.
        /// </summary>
        public Hikkake()
            : this("HIKKAKE")
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
                if (Samples >= 3)
                {
                    // copy here the pattern recognition code below
                    // 1st + 2nd: lower high and higher low
                    if (window[1].High < window[2].High && window[1].Low > window[2].Low &&
                        // (bull) 3rd: lower high and lower low
                        ((input.High < window[1].High && input.Low < window[1].Low)
                          ||
                          // (bear) 3rd: higher high and higher low
                          (input.High > window[1].High && input.Low > window[1].Low)
                        )
                    )
                    {
                        _patternResult = (input.High < window[1].High ? 1 : -1);
                        _patternIndex = (int)Samples - 1;
                    }
                    else
                        // search for confirmation if hikkake was no more than 3 bars ago
                        if (Samples <= _patternIndex + 4 &&
                            // close higher than the high of 2nd
                            ((_patternResult > 0 && input.Close > window[(int)Samples - _patternIndex].High)
                              ||
                              // close lower than the low of 2nd
                              (_patternResult < 0 && input.Close < window[(int)Samples - _patternIndex].Low)
                            )
                        )
                        _patternIndex = 0;
                }

                return 0m;
            }

            decimal value;
            // 1st + 2nd: lower high and higher low
            if (window[1].High < window[2].High && window[1].Low > window[2].Low &&
                // (bull) 3rd: lower high and lower low
                ((input.High < window[1].High && input.Low < window[1].Low)
                 ||
                 // (bear) 3rd: higher high and higher low
                 (input.High > window[1].High && input.Low > window[1].Low)
                    )
                )
            {
                _patternResult = (input.High < window[1].High ? 1 : -1);
                _patternIndex = (int) Samples - 1;
                value = _patternResult;
            }
            else
            {
                // search for confirmation if hikkake was no more than 3 bars ago
                if (Samples <= _patternIndex + 4 &&
                    // close higher than the high of 2nd
                    ((_patternResult > 0 && input.Close > window[(int) Samples - _patternIndex].High)
                     ||
                     // close lower than the low of 2nd
                     (_patternResult < 0 && input.Close < window[(int) Samples - _patternIndex].Low)
                        )
                    )
                {
                    value = _patternResult + (_patternResult > 0 ? 1 : -1);
                    _patternIndex = 0;
                }
                else
                    value = 0;
            }

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _patternIndex = 0;
            _patternResult = 0;
            base.Reset();
        }
    }
}
