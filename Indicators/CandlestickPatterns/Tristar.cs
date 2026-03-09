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
    /// Tristar candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - 3 consecutive doji days
    /// - the second doji is a star
    /// The meaning of "doji" is specified with SetCandleSettings
    /// The returned value is positive(+1) when bullish or negative(-1) when bearish
    /// </remarks>
    public class Tristar : CandlestickPattern
    {
        private readonly int _bodyDojiAveragePeriod;

        private decimal _bodyDojiPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tristar"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public Tristar(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod + 2 + 1)
        {
            _bodyDojiAveragePeriod = CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tristar"/> class.
        /// </summary>
        public Tristar()
            : this("TRISTAR")
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
                if (Samples >= Period - _bodyDojiAveragePeriod - 2 && Samples < Period - 2)
                {
                    _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st: doji
                GetRealBody(window[2]) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, window[2]) &&
                // 2nd: doji
                GetRealBody(window[1]) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, window[2]) &&
                // 3rd: doji
                GetRealBody(input) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, window[2]))
            {     
                value = 0;
                if (
                    // 2nd gaps up
                    GetRealBodyGapUp(window[1], window[2]) &&
                    // 3rd is not higher than 2nd
                    Math.Max(input.Open, input.Close) < Math.Max(window[1].Open, window[1].Close)
                   )
                    value = -1m;
                if (
                    // 2nd gaps down
                    GetRealBodyGapDown(window[1], window[2]) &&
                    // 3rd is not lower than 2nd 
                    Math.Min(input.Open, input.Close) > Math.Min(window[1].Open, window[1].Close)
                   )
                    value = 1m;
            }
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, window[2]) -
                                    GetCandleRange(CandleSettingType.BodyDoji, window[_bodyDojiAveragePeriod + 2]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyDojiPeriodTotal = 0m;
            base.Reset();
        }
    }
}
