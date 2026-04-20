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

namespace QuantConnect.Securities.Equity
{
    /// <summary>
    /// Short margin interest rate model
    ///
    /// When shorting charges the fee rate provided by the <see cref="QuantConnect.Interfaces.IShortableProvider"/>.
    /// When long adds the rebate fee provided by the <see cref="QuantConnect.Interfaces.IShortableProvider"/>.
    /// </summary>
    public class ShortMarginInterestRateModel : IMarginInterestRateModel
    {
        private bool _isShort;
        private DateTime _previousTime;

        /// <summary>
        /// Accumulated shorting fee, negative means paid, positive earned.
        ///
        /// Negative due to borrowing the asset to short, the fee rate.
        /// Positive due to lending the asset for shorting, the rebate rate.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Apply margin interest rates to the portfolio
        /// </summary>
        /// <param name="marginInterestRateParameters">The parameters to use</param>
        public void ApplyMarginInterestRate(MarginInterestRateParameters marginInterestRateParameters)
        {
            var security = marginInterestRateParameters.Security;
            if (!security.Holdings.HoldStock)
            {
                // clear state
                _previousTime = default;
                return;
            }

            if (_previousTime == default || _isShort != security.Holdings.IsShort)
            {
                // start the clock on initial state or when changing sides
                _isShort = security.Holdings.IsShort;
                _previousTime = marginInterestRateParameters.Time;
                return;
            }
            else if (marginInterestRateParameters.Time.Date == _previousTime.Date)
            {
                // charge once a day
                return;
            }

            decimal? feeRate;
            if (_isShort)
            {
                feeRate = security.ShortableProvider?.FeeRate(security.Symbol, security.LocalTime.Date);
            }
            else
            {
                feeRate = security.ShortableProvider?.RebateRate(security.Symbol, security.LocalTime.Date);
            }

            if (feeRate == null || feeRate.Value == 0)
            {
                // nothing todo
                _previousTime = default;
                return;
            }

            var dailyFeeRate = ((feeRate.Value * security.Holdings.HoldingsValue) / 360);
            var fee = dailyFeeRate * (marginInterestRateParameters.Time.Date - _previousTime.Date).Days;

            Amount += fee;
            security.QuoteCurrency.AddAmount(fee);
            // until next date
            _previousTime = marginInterestRateParameters.Time;
        }
    }
}
