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
    /// Advance Block candlestick pattern
    /// </summary>
    /// <remarks>
    /// Must have:
    /// - three white candlesticks with consecutively higher closes
    /// - each candle opens within or near the previous white real body
    /// - first candle: long white with no or very short upper shadow(a short shadow is accepted too for more flexibility)
    /// - second and third candles, or only third candle, show signs of weakening: progressively smaller white real bodies
    /// and/or relatively long upper shadows; see below for specific conditions
    /// The meanings of "long body", "short shadow", "far" and "near" are specified with SetCandleSettings;
    /// The returned value is negative(-1): advance block is always bearish;
    /// The user should consider that advance block is significant when it appears in uptrend, while this function
    /// does not consider it
    /// </remarks>
    public class AdvanceBlock : CandlestickPattern
    {
        private readonly int _shadowShortAveragePeriod;
        private readonly int _shadowLongAveragePeriod;
        private readonly int _nearAveragePeriod;
        private readonly int _farAveragePeriod;
        private readonly int _bodyLongAveragePeriod;

        private decimal[] _shadowShortPeriodTotal = new decimal[3];
        private decimal[] _shadowLongPeriodTotal = new decimal[2];
        private decimal[] _nearPeriodTotal = new decimal[3];
        private decimal[] _farPeriodTotal = new decimal[3];
        private decimal _bodyLongPeriodTotal;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvanceBlock"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        public AdvanceBlock(string name) 
            : base(name, Math.Max(Math.Max(Math.Max(CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod, CandleSettings.Get(CandleSettingType.ShadowShort).AveragePeriod),
                  Math.Max(CandleSettings.Get(CandleSettingType.Far).AveragePeriod, CandleSettings.Get(CandleSettingType.Near).AveragePeriod)),
                  CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod) + 2 + 1)
        {
            _shadowShortAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowShort).AveragePeriod;
            _shadowLongAveragePeriod = CandleSettings.Get(CandleSettingType.ShadowLong).AveragePeriod;
            _nearAveragePeriod = CandleSettings.Get(CandleSettingType.Near).AveragePeriod;
            _farAveragePeriod = CandleSettings.Get(CandleSettingType.Far).AveragePeriod;
            _bodyLongAveragePeriod = CandleSettings.Get(CandleSettingType.BodyLong).AveragePeriod;
        }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvanceBlock"/> class.
    /// </summary>
    public AdvanceBlock()
            : this("ADVANCEBLOCK")
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
                if (Samples >= Period - _shadowShortAveragePeriod)
                {
                    _shadowShortPeriodTotal[2] += GetCandleRange(CandleSettingType.ShadowShort, window[2]);
                    _shadowShortPeriodTotal[1] += GetCandleRange(CandleSettingType.ShadowShort, window[1]);
                    _shadowShortPeriodTotal[0] += GetCandleRange(CandleSettingType.ShadowShort, input);
                }

                if (Samples >= Period - _shadowLongAveragePeriod)
                {
                    _shadowLongPeriodTotal[1] += GetCandleRange(CandleSettingType.ShadowLong, window[1]);
                    _shadowLongPeriodTotal[0] += GetCandleRange(CandleSettingType.ShadowLong, input);
                }

                if (Samples >= Period - _bodyLongAveragePeriod)
                {
                    _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]);
                }

                if (Samples >= Period - _nearAveragePeriod)
                {
                    _nearPeriodTotal[2] += GetCandleRange(CandleSettingType.Near, window[2]);
                    _nearPeriodTotal[1] += GetCandleRange(CandleSettingType.Near, window[1]);
                }

                if (Samples >= Period - _farAveragePeriod)
                {
                    _farPeriodTotal[2] += GetCandleRange(CandleSettingType.Far, window[2]);
                    _farPeriodTotal[1] += GetCandleRange(CandleSettingType.Far, window[1]);
                }

                return 0m;
            }

            decimal value;
            if (
                // 1st white
                GetCandleColor(window[2]) == CandleColor.White &&
                // 2nd white
                GetCandleColor(window[1]) == CandleColor.White &&
                // 3rd white
                GetCandleColor(input) == CandleColor.White &&
                // consecutive higher closes
                input.Close > window[1].Close && window[1].Close > window[2].Close &&
                // 2nd opens within/near 1st real body
                window[1].Open > window[2].Open &&
                window[1].Open <= window[2].Close + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[2], window[2]) &&
                // 3rd opens within/near 2nd real body
                input.Open > window[1].Open &&
                input.Open <= window[1].Close + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[1], window[1]) &&
                // 1st: long real body
                GetRealBody(window[2]) > GetCandleAverage(CandleSettingType.BodyLong, _bodyLongPeriodTotal, window[2]) &&
                // 1st: short upper shadow
                GetUpperShadow(window[2]) < GetCandleAverage(CandleSettingType.ShadowShort, _shadowShortPeriodTotal[2], window[2]) &&
                (
                    // ( 2 far smaller than 1 && 3 not longer than 2 )
                    // advance blocked with the 2nd, 3rd must not carry on the advance
                    (
                        GetRealBody(window[1]) < GetRealBody(window[2]) - GetCandleAverage(CandleSettingType.Far, _farPeriodTotal[2], window[2]) &&
                        GetRealBody(input) < GetRealBody(window[1]) + GetCandleAverage(CandleSettingType.Near, _nearPeriodTotal[1], window[1])
                    ) ||
                    // 3 far smaller than 2
                    // advance blocked with the 3rd
                    (
                        GetRealBody(input) < GetRealBody(window[1]) - GetCandleAverage(CandleSettingType.Far, _farPeriodTotal[1], window[1])
                    ) ||
                    // ( 3 smaller than 2 && 2 smaller than 1 && (3 or 2 not short upper shadow) )
                    // advance blocked with progressively smaller real bodies and some upper shadows
                    (
                        GetRealBody(input) < GetRealBody(window[1]) &&
                        GetRealBody(window[1]) < GetRealBody(window[2]) &&
                        (
                            GetUpperShadow(input) > GetCandleAverage(CandleSettingType.ShadowShort, _shadowShortPeriodTotal[0], input) ||
                            GetUpperShadow(window[1]) > GetCandleAverage(CandleSettingType.ShadowShort, _shadowShortPeriodTotal[1], window[1])
                        )
                    ) ||
                    // ( 3 smaller than 2 && 3 long upper shadow )
                    // advance blocked with 3rd candle's long upper shadow and smaller body
                    (
                        GetRealBody(input) < GetRealBody(window[1]) &&
                        GetUpperShadow(input) > GetCandleAverage(CandleSettingType.ShadowLong, _shadowLongPeriodTotal[0], input)
                    )
                )
              )
                value = -1m;
            else
                value = 0m;

            // add the current range and subtract the first range: this is done after the pattern recognition 
            // when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)

            for (var i = 2; i >= 0; i--)
            {
                _shadowShortPeriodTotal[i] += GetCandleRange(CandleSettingType.ShadowShort, window[i]) -
                                              GetCandleRange(CandleSettingType.ShadowShort, window[i + _shadowShortAveragePeriod]);
            }

            for (var i = 1; i >= 0; i--)
            {
                _shadowLongPeriodTotal[i] += GetCandleRange(CandleSettingType.ShadowLong, window[i]) -
                                             GetCandleRange(CandleSettingType.ShadowLong, window[i + _shadowLongAveragePeriod]);
            }

            for (var i = 2; i >= 1; i--)
            {
                _farPeriodTotal[i] += GetCandleRange(CandleSettingType.Far, window[i]) -
                                      GetCandleRange(CandleSettingType.Far, window[i + _farAveragePeriod]);
                _nearPeriodTotal[i] += GetCandleRange(CandleSettingType.Near, window[i]) -
                                       GetCandleRange(CandleSettingType.Near, window[i + _nearAveragePeriod]);
            }

            _bodyLongPeriodTotal += GetCandleRange(CandleSettingType.BodyLong, window[2]) - 
                                    GetCandleRange(CandleSettingType.BodyLong, window[2 + _bodyLongAveragePeriod]);

            return value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _shadowShortPeriodTotal = new decimal[3];
            _shadowLongPeriodTotal = new decimal[2];
            _nearPeriodTotal = new decimal[3];
            _farPeriodTotal = new decimal[3];
            _bodyLongPeriodTotal = 0;
            base.Reset();
        }
    }
}
