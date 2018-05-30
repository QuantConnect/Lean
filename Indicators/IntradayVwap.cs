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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Defines the canonical intraday VWAP indicator
    /// </summary>
    public class IntradayVwap : IndicatorBase<BaseData>
    {
        private DateTime _lastDate;
        private decimal _sumOfVolume;
        private decimal _sumOfPriceTimesVolume;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _sumOfVolume > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntradayVwap"/> class
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        public IntradayVwap(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Computes the new VWAP
        /// </summary>
        protected override IndicatorResult ValidateAndComputeNextValue(BaseData input)
        {
            decimal volume, averagePrice;
            if (!TryGetVolumeAndAveragePrice(input, out volume, out averagePrice))
            {
                return new IndicatorResult(0, IndicatorStatus.InvalidInput);
            }

            // reset vwap on daily boundaries
            if (_lastDate != input.EndTime.Date)
            {
                _sumOfVolume = 0m;
                _sumOfPriceTimesVolume = 0m;
                _lastDate = input.EndTime.Date;
            }

            // running totals for Σ PiVi / Σ Vi
            _sumOfVolume += volume;
            _sumOfPriceTimesVolume += averagePrice * volume;

            if (_sumOfVolume == 0m)
            {
                // if we have no trade volume then use the current price as VWAP
                return input.Value;
            }

            return _sumOfPriceTimesVolume / _sumOfVolume;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// NOTE: This must be overriden since it's abstract in the base, but
        /// will never be invoked since we've override the validate method above.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(BaseData input)
        {
            throw new NotImplementedException($"{nameof(IntradayVwap)}.{nameof(ComputeNextValue)} should never be invoked.");
        }

        /// <summary>
        /// Determines the volume and price to be used for the current input in the VWAP computation
        /// </summary>
        protected bool TryGetVolumeAndAveragePrice(BaseData input, out decimal volume, out decimal averagePrice)
        {
            var tick = input as Tick;

            if (tick?.TickType == TickType.Trade)
            {
                volume = tick.Quantity;
                averagePrice = tick.LastPrice;
                return true;
            }

            var tradeBar = input as TradeBar;
            if (tradeBar?.IsFillForward == false)
            {
                volume = tradeBar.Volume;
                averagePrice = (tradeBar.High + tradeBar.Low + tradeBar.Close) / 3m;
                return true;
            }

            volume = 0;
            averagePrice = 0;
            return false;
        }
    }
}