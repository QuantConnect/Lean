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

namespace QuantConnect.Securities.CryptoFuture
{
    /// <summary>
    /// The responsability of this model is to apply future funding rate cash flows to the portfolio based on open positions
    /// </summary>
    public class BinanceFutureMarginInterestRateModel : IMarginInterestRateModel
    {
        private DateTime _nextFundingRateApplication = DateTime.MaxValue;

        /// <summary>
        /// Apply margin interest rates to the portfolio
        /// </summary>
        /// <param name="marginInterestRateParameters">The parameters to use</param>
        public void ApplyMarginInterestRate(MarginInterestRateParameters marginInterestRateParameters)
        {
            var security = marginInterestRateParameters.Security;
            var time = marginInterestRateParameters.Time;
            var cryptoFuture = (CryptoFuture)security;
            if (!cryptoFuture.Invested)
            {
                // nothing to do
                _nextFundingRateApplication = DateTime.MaxValue;
                return;
            }
            else if (_nextFundingRateApplication == DateTime.MaxValue)
            {
                // we opened a new position
                _nextFundingRateApplication = GetNextFundingRateApplication(time);
            }

            var marginInterest = cryptoFuture.Cache.GetData<MarginInterestRate>();
            if(marginInterest == null)
            {
                return;
            }

            while(time >= _nextFundingRateApplication)
            {
                // When the funding rate is positive, the price of the perpetual contract is higher than the mark price,
                // thus, traders who are long pay for short positions. Conversely, a negative funding rate indicates that perpetual
                // prices are below the mark price, which means that short positions pay for longs.
                // Funding Amount = Nominal Value of Positions * Funding Rate

                var holdings = cryptoFuture.Holdings;

                var positionValue = cryptoFuture.Holdings.GetQuantityValue(holdings.Quantity);

                var funding = marginInterest.InterestRate * positionValue.Amount;

                funding *= -1;
                // '* -1' because:
                // - we pay when 'funding' positive:
                //      long position & positive rate
                //      short position & negative rate
                // - we ear when 'funding' negative:
                //      long position & negative rate
                //      short position & positive rate
                positionValue.Cash.AddAmount(funding);

                _nextFundingRateApplication = GetNextFundingRateApplication(_nextFundingRateApplication);
            }
        }

        private static DateTime GetNextFundingRateApplication(DateTime currentTime)
        {
            if(currentTime.Hour >= 16)
            {
                // tomorrow 00:00
                return currentTime.Date.AddDays(1);
            }
            else if (currentTime.Hour >= 8)
            {
                return currentTime.Date.AddHours(16);
            }
            else
            {
                return currentTime.Date.AddHours(8);
            }
        }
    }
}
