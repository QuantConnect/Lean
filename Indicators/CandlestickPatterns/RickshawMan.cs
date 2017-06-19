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
    /// Rickshaw Man candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - doji body
    /// - two long shadows
    /// - body near the midpoint of the high-low range
    /// The meaning of "doji" and "near" is specified with SetCandleSettings
    /// The returned value is always positive(+1) but this does not mean it is bullish: rickshaw man shows uncertainty
    /// </remarks>
    public class RickshawMan : CandlestickPattern
    {
        private readonly int _bodyDojiAveragePeriod;
        private readonly int _shadowLongAveragePeriod;
        private readonly int _nearAveragePeriod;

        private decimal _bodyDojiPeriodTotal;
        private decimal _shadowLongPeriodTotal;
        private decimal _nearPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="RickshawMan"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public RickshawMan(string name)
            : base(name, Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod, CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod),
                  CandleSettings.Get(CandleSettingType.Near).AveragePeriod) + 1)
        {
            _bodyDojiAveragePeriod = CandleSettings.Get(CandleSettingType.BodyDoji).AveragePeriod;
            _shadowLongAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod;
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RickshawMan"/> class.
        /// </summary>
        public RickshawMan()
            : this("RICKSHAWMAN")
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
                if (Samples >= Period - _bodyDojiAveragePeriod)
                {
                    _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input);
                }

                if (Samples >= Period - _shadowLongAveragePeriod)
                {
                    _shadowLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowLong, input);
                }

                if (Samples >= Period - _nearAveragePeriod)
                {
                    _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, input);
                }

                return 0m;
            }

            decimal value;
            if (
                // doji
                GetRealBody(input) <= GetCandleAverage(CandleSettingType.BodyDoji, _bodyDojiPeriodTotal, input) &&
                // long shadow
                GetLowerShadow(input) > GetCandleAverage(CandleSettingType.ShadowLong, _shadowLongPeriodTotal, input) &&
                // long shadow
                GetUpperShadow(input) > GetCandleAverage(CandleSettingType.ShadowLong, _shadowLongPeriodTotal, input) &&
                // body near midpoint
                (
                    Math.Min(input.Open, input.Close)
                        <= input.Low + GetHighLowRange(input) / 2 + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, input)
                    &&
                    Math.Max(input.Open, input.Close)
                        >= input.Low + GetHighLowRange(input) / 2 - GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal, input)
                )
              )
                value = 1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            _bodyDojiPeriodTotal += GetCandleRange(CandleSettingType.BodyDoji, input) -
                                    GetCandleRange(CandleSettingType.BodyDoji, window[_bodyDojiAveragePeriod]);

            _shadowLongPeriodTotal += GetCandleRange(CandleSettingType.ShadowLong, input) -
                                      GetCandleRange(CandleSettingType.ShadowLong, window[_shadowLongAveragePeriod]);

            _nearPeriodTotal += GetCandleRange(CandleSettingType.Near, input) -
                                GetCandleRange(CandleSettingType.Near, window[_nearAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _bodyDojiPeriodTotal = 0;
            _shadowLongPeriodTotal = 0;
            _nearPeriodTotal = 0;
            base.Reset();
        }
    }
}
