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
    /// Three Outside Up/Down candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first: black(white) real body
    /// - second: white(black) real body that engulfs the prior real body
    /// - third: candle that closes higher(lower) than the second candle
    /// The returned value is positive (+1) for the three outside up or negative (-1) for the three outside down;
    /// The user should consider that a three outside up must appear in a downtrend and three outside down must appear
    /// in an uptrend, while this function does not consider it
    /// </remarks>
    public class ThreeOutside : CandlestickPattern
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeOutside"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ThreeOutside(string name) 
            : base(name, 3)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeOutside"/> class.
        /// </summary>
        public ThreeOutside()
            : this("THREEOUTSIDE")
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
               (
                  // white engulfs black
                  GetCandleColor(window[1]) == CandleColor.White && GetCandleColor(window[2]) == CandleColor.Black &&
                  window[1].Close > window[2].Open && window[1].Open < window[2].Close &&
                  // third candle higher
                  input.Close > window[1].Close
                )
                ||
                (
                  // black engulfs white
                  GetCandleColor(window[1]) == CandleColor.Black && GetCandleColor(window[2]) == CandleColor.White &&
                  window[1].Open > window[2].Close && window[1].Close < window[2].Open &&
                  // third candle lower
                  input.Close < window[1].Close
                )
              )
                value = (int)GetCandleColor(window[1]);
            else
                value = 0;

            return value;
        }
    }
}
