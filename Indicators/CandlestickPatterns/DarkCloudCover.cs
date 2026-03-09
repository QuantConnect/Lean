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
    /// Dark Cloud Cover candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - first candle: long white candle
    /// - second candle: black candle that opens above previous day high and closes within previous day real body; 
    /// Greg Morris wants the close to be below the midpoint of the previous real body
    /// The meaning of "long" is specified with SetCandleSettings, the penetration of the first real body is specified
    /// with optInPenetration
    /// The returned value is negative(-1): dark cloud cover is always bearish
    /// The user should consider that a dark cloud cover is significant when it appears in an uptrend, while 
    /// this function does not consider it
    /// </remarks>
    public class DarkCloudCover : CandlestickPattern
    {
        private readonly decimal _penetration;

        private readonly int _bodyLongAveragePeriod;

        private decimal _bodyLongPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="DarkCloudCover"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        public DarkCloudCover(string name, decimal penetration = 0.5m) 
            : base(name, CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod + 1 + 1)
        {
            _penetration = penetration;

            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DarkCloudCover"/> class.
        /// </summary>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        public DarkCloudCover(decimal penetration)
            : this("DARKCLOUDCOVER", penetration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DarkCloudCover"/> class.
        /// </summary>
        public DarkCloudCover()
            : this("DARKCLOUDCOVER")
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
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st: white
                GetCandleColor(window[1]) == CandleColor.White &&
                //      long
                GetRealBody(window[1]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[1]) &&
                // 2nd: black
                GetCandleColor(input) == CandleColor.Black &&
                //      open above prior high
                input.Open > window[1].High &&
                //      close within prior body
                input.Close > window[1].Open &&
                input.Close < window[1].Close - GetRealBody(window[1]) * _penetration
              )
                value = -1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[1]) -
                                    GetCandleRange(CandleSettingType.BodyLong, window[_bodyLongAveragePeriod + 1]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyLongPeriodTotal = 0;
            base.Reset();
        }
    }
}
