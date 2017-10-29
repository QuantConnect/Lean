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
    /// Stick Sandwich candlestick pattern indicator
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: black candle
    /// - second candle: white candle that trades only above the prior close(low > prior close)
    /// - third candle: black candle with the close equal to the first candle's close
    /// The meaning of "equal" is specified with SetCandleSettings
    /// The returned value is always positive(+1): stick sandwich is always bullish;
    /// The user should consider that stick sandwich is significant when coming in a downtrend,
    /// while this function does not consider it
    /// </remarks>
    public class StickSandwich : CandlestickPattern
    {
        private readonly int _equalAveragePeriod;

        private decimal _equalPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="StickSandwich"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public StickSandwich(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.Equal).AveragePeriod + 2 + 1)
        {
            _equalAveragePeriod = CandleSettings.Get(CandleSettingType.Equal).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StickSandwich"/> class.
        /// </summary>
        public StickSandwich()
            : this("STICKSANDWICH")
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
                if (Samples >= Period - _equalAveragePeriod)
                {
                    _equalPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]);
                }

                return 0m;
            }

            decimal value;
            if (
                // first black
                GetCandleColor(window[2]) == CandleColor.Black &&
                // second white
                GetCandleColor(window[1]) == CandleColor.White &&
                // third black
                GetCandleColor(input) == CandleColor.Black &&
                // 2nd low > prior close
                window[1].Low > window[2].Close &&
                // 1st and 3rd same close
                input.Close <= window[2].Close + GetCandleAverage(CandleSettingType.Equal, _equalPeriodTotal, window[2]) &&
                input.Close >= window[2].Close - GetCandleAverage(CandleSettingType.Equal, _equalPeriodTotal, window[2])
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _equalPeriodTotal += GetCandleRange(CandleSettingType.Equal, window[2]) -
                                 GetCandleRange(CandleSettingType.Equal, window[_equalAveragePeriod + 2]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _equalPeriodTotal = 0m;
            base.Reset();
        }
    }
}
