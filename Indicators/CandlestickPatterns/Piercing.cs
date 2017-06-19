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
    /// Piercing candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long black candle
    /// - second candle: long white candle with open below previous day low and close at least at 50% of previous day
    /// real body
    /// The meaning of "long" is specified with SetCandleSettings
    /// The returned value is positive(+1): piercing pattern is always bullish
    /// The user should consider that a piercing pattern is significant when it appears in a downtrend, while 
    /// this function does not consider it
    /// </remarks>
    public class Piercing : CandlestickPattern
    {
        private readonly int _bodyLongAveragePeriod;

        private decimal[] _bodyLongPeriodTotal = new decimal[2];

        /// <summary>
        /// Initializes a new instance of the <see cref="Piercing"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public Piercing(string name) 
            : base(name, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod + 1 + 1)
        {
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Piercing"/> class.
        /// </summary>
        public Piercing()
            : this("PIERCING")
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
                    _bodyLongPeriodTotal[1] += GetCandleRange(CandleSettingType.BodyLong, window[1]);
                    _bodyLongPeriodTotal[0] += GetCandleRange(CandleSettingType.BodyLong, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st: black
                GetCandleColor(window[1]) == CandleColor.Black &&
                //      long
                GetRealBody(window[1]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal[1], window[1]) &&
                // 2nd: white
                GetCandleColor(input) == CandleColor.White &&
                //      long
                GetRealBody(input) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal[0], input) &&
                //      open below prior low
                input.Open < window[1].Low &&
                //      close within prior body
                input.Close < window[1].Open &&
                //      above midpoint
                input.Close > window[1].Close + GetRealBody(window[1]) * 0.5m
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 1; i >= 0; i--)
            {
                _bodyLongPeriodTotal[i] += GetCandleRange(CandleSettingType.BodyLong, window[i]) -
                                           GetCandleRange(CandleSettingType.BodyLong, window[i + _bodyLongAveragePeriod]);
            }

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = new decimal[2];
            base.Reset();
        }
    }
}
