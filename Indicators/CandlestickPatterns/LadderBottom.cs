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
    /// Ladder Bottom candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - three black candlesticks with consecutively lower opens and closes
    /// - fourth candle: black candle with an upper shadow(it's supposed to be not very short)
    /// - fifth candle: white candle that opens above prior candle's body and closes above prior candle's high
    /// The meaning of "very short" is specified with SetCandleSettings
    /// The returned value is positive (+1): ladder bottom is always bullish;
    /// The user should consider that ladder bottom is significant when it appears in a downtrend,
    /// while this function does not consider it
    /// </remarks>
    public class LadderBottom : CandlestickPattern
    {
        private readonly int _shadowVeryShortAveragePeriod;

        private decimal _shadowVeryShortPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="LadderBottom"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public LadderBottom(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod + 4 + 1)
        {
            _shadowVeryShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowVeryShort).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LadderBottom"/> class.
        /// </summary>
        public LadderBottom()
            : this("LADDERBOTTOM")
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
                if (Samples >= Period - _shadowVeryShortAveragePeriod)
                {
                    _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                // 3 black candlesticks
                GetCandleColor(window[4]) == CandleColor.Black && 
                GetCandleColor(window[3]) == CandleColor.Black && 
                GetCandleColor(window[2]) == CandleColor.Black &&
                // with consecutively lower opens
                window[4].Open > window[3].Open && window[3].Open > window[2].Open &&
                // and closes
                window[4].Close > window[3].Close && window[3].Close > window[2].Close &&
                // 4th: black with an upper shadow
                GetCandleColor(window[1]) == CandleColor.Black &&
                GetUpperShadow(window[1]) > GetCandleAverage(CandleSettingType.ShadowVeryShort, _shadowVeryShortPeriodTotal, window[1]) &&
                // 5th: white
                GetCandleColor(input) == CandleColor.White &&
                // that opens above prior candle's body
                input.Open > window[1].Open &&
                // and closes above prior candle's high
                input.Close > window[1].High
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _shadowVeryShortPeriodTotal += GetCandleRange(CandleSettingType.ShadowVeryShort, window[1]) -
                                           GetCandleRange(CandleSettingType.ShadowVeryShort, window[_shadowVeryShortAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _shadowVeryShortPeriodTotal = 0m;
            base.Reset();
        }
    }
}
