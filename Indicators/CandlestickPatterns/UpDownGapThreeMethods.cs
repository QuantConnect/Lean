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
    /// Up/Down Gap Three Methods candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: white (black) candle
    /// - second candle: white(black) candle
    /// - upside(downside) gap between the first and the second real bodies
    /// - third candle: black(white) candle that opens within the second real body and closes within the first real body
    /// The returned value is positive(+1) when bullish or negative(-1) when bearish;
    /// The user should consider that up/downside gap 3 methods is significant when it appears in a trend, while this
    /// function does not consider it
    /// </remarks>
    public class UpDownGapThreeMethods : CandlestickPattern
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpDownGapThreeMethods"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public UpDownGapThreeMethods(string name) 
            : base(name, 2 + 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpDownGapThreeMethods"/> class.
        /// </summary>
        public UpDownGapThreeMethods()
            : this("UPDOWNGAPTHREEMETHODS")
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
                return 0m;
            }

            decimal value;
            if (
                // 1st and 2nd of same color
                GetCandleColor(window[2]) == GetCandleColor(window[1]) &&
                // 3rd opposite color
                (int)GetCandleColor(window[1]) == -(int)GetCandleColor(input) &&
                // 3rd opens within 2nd rb
                input.Open < Math.Max(window[1].Close, window[1].Open) &&
                input.Open > Math.Min(window[1].Close, window[1].Open) &&
                // 3rd closes within 1st rb
                input.Close < Math.Max(window[2].Close, window[2].Open) &&
                input.Close > Math.Min(window[2].Close, window[2].Open) &&
                ((
                    // when 1st is white
                    GetCandleColor(window[2]) == CandleColor.White &&
                    // upside gap
                    GetRealBodyGapUp(window[1], window[2])
                  ) ||
                  (
                    // when 1st is black
                    GetCandleColor(window[2]) == CandleColor.Black &&
                    // downside gap
                    GetRealBodyGapDown(window[1], window[2])
                  )
                )
            )
                value = (int)GetCandleColor(window[2]);
            else
                value = 0;

            return value;
        }
    }
}
