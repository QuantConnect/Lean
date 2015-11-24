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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for applying cash settlement rules
    /// </summary>
    /// <remarks>This model applies cash settlement after T+N days</remarks>
    public class DelayedSettlementModel : ISettlementModel
    {
        private readonly int _numberOfDays;
        private readonly TimeSpan _timeOfDay;

        /// <summary>
        /// Creates an instance of the <see cref="DelayedSettlementModel"/> class
        /// </summary>
        /// <param name="numberOfDays">The number of days required for settlement</param>
        /// <param name="timeOfDay">The time of day used for settlement</param>
        public DelayedSettlementModel(int numberOfDays, TimeSpan timeOfDay)
        {
            _numberOfDays = numberOfDays;
            _timeOfDay = timeOfDay;
        }

        /// <summary>
        /// Applies cash settlement rules
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The fill's security</param>
        /// <param name="applicationTimeUtc">The fill time (in UTC)</param>
        /// <param name="currency">The currency symbol</param>
        /// <param name="amount">The amount of cash to apply</param>
        public void ApplyFunds(SecurityPortfolioManager portfolio, Security security, DateTime applicationTimeUtc, string currency, decimal amount)
        {
            if (amount > 0)
            {
                // positive amount: sell order filled

                portfolio.UnsettledCashBook[currency].AddAmount(amount);

                // find the correct settlement date (usually T+3 or T+1)
                var settlementDate = applicationTimeUtc.ConvertFromUtc(security.Exchange.TimeZone).Date;
                for (var i = 0; i < _numberOfDays; i++)
                {
                    settlementDate = settlementDate.AddDays(1);

                    // only count days when market is open
                    if (!security.Exchange.Hours.IsDateOpen(settlementDate))
                        i--;
                }

                // use correct settlement time
                var settlementTimeUtc = settlementDate.Add(_timeOfDay).ConvertToUtc(security.Exchange.Hours.TimeZone);

                portfolio.AddUnsettledCashAmount(new UnsettledCashAmount(settlementTimeUtc, currency, amount));
            }
            else
            {
                // negative amount: buy order filled

                portfolio.CashBook[currency].AddAmount(amount);
            }
        }
    }
}
