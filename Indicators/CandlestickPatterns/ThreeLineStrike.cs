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
    /// Three Line Strike candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - three white soldiers (three black crows): three white (black) candlesticks with consecutively higher (lower) closes,
    /// each opening within or near the previous real body
    /// - fourth candle: black (white) candle that opens above (below) prior candle's close and closes below (above) 
    /// the first candle's open
    /// The meaning of "near" is specified with TA_SetCandleSettings;
    /// The returned value is positive (+1) when bullish or negative (-1) when bearish;
    /// The user should consider that 3-line strike is significant when it appears in a trend in the same direction of
    /// the first three candles, while this function does not consider it
    /// </remarks>
    public class ThreeLineStrike : CandlestickPattern
    {
        private decimal[] _nearPeriodTotal = new decimal[4];

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeLineStrike"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public ThreeLineStrike(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.Near).AveragePeriod + 3 + 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeLineStrike"/> class.
        /// </summary>
        public ThreeLineStrike()
            : this("THREELINESTRIKE")
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
        protected override decimal ComputeNextValue(IReadOnlyWindow<TradeBar> window, TradeBar input)
        {
            if (!IsReady)
            {
                if (window.Count > 3)
                {
                    _nearPeriodTotal[3] += GetCandleRange(CandleSettingType.Near, window[3]);
                    _nearPeriodTotal[2] += GetCandleRange(CandleSettingType.Near, window[2]);
                }
                return 0m;
            }

            decimal value;
            if (GetCandleColor(window[3]) == GetCandleColor(window[2]) &&
                GetCandleColor(window[2]) == GetCandleColor(window[1]) &&
                (int)GetCandleColor(input) == -(int)GetCandleColor(window[1]) &&
                window[2].Open >= Math.Min(window[3].Open, window[3].Close) - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[3], window[3]) &&
                window[2].Open <= Math.Max(window[3].Open, window[3].Close) + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[3], window[3]) &&
                window[1].Open >= Math.Min(window[2].Open, window[2].Close) - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[2], window[2]) &&
                window[1].Open <= Math.Max(window[2].Open, window[2].Close) + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[2], window[2]) &&
                (
                    (
                        GetCandleColor(window[1]) == CandleColor.White &&
                        window[1].Close > window[2].Close && window[2].Close > window[3].Close &&
                        input.Open > window[1].Close &&
                        input.Close < window[3].Open
                    ) ||
                    (
                        GetCandleColor(window[1]) == CandleColor.Black &&
                        window[1].Close < window[2].Close && window[2].Close < window[3].Close &&
                        input.Open < window[1].Close &&
                        input.Close > window[3].Open
                    )
                )
              )
                value = (int)GetCandleColor(window[1]);
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 3; i >= 2; i--)
            {
                _nearPeriodTotal[i] += GetCandleRange(CandleSettingType.Near, window[i]) -
                                       GetCandleRange(CandleSettingType.Near, window[Period - 4 + i]);
            }

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _nearPeriodTotal = new decimal[4];
            base.Reset();
        }
    }
}
