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
using QuantConnect.Securities;
using System.Linq;

namespace QuantConnect.Orders.MarginInterest
{
    /// <summary>
    /// Provides an order margin interest model that always returns the same margin interest.
    /// </summary>
    public class ConstantMarginInterestModel : IMarginInterestModel
    {
        private const int _daysPerYear = 365;
        private readonly decimal _marginInterestRate;
        private decimal _totalInterestPaid;

        /// <summary>
        /// Total interest paid during the algorithm operation across all securities in portfolio.
        /// </summary>
        public decimal TotalInterestPaid
        {
            get
            {
                return _totalInterestPaid;
            }
        } 

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantMarginInterestModel"/> class with the specified <paramref name="marginInterestRate"/>
        /// </summary>
        /// <param name="marginInterestRate">The constant annual margin interest rate used by the model</param>
        public ConstantMarginInterestModel(decimal marginInterestRate)
        {
            _marginInterestRate = marginInterestRate;
            _totalInterestPaid = 0m;
        }

        /// <summary>
        /// Gets the margin interest associated with the total loan in our portfolio. 
        /// This returns the cost of the borrowed money in the account currency
        /// </summary>
        /// <param name="securities">Securities collection for the portfolio summation</param>
        /// <param name="applicationTimeUtc">Time margin interest payment is made</param>
        /// <param name="totalMarginUsed">Total amount of margin used</param>
        public decimal PayMarginInterest(SecurityManager securities, DateTime applicationTimeUtc, decimal totalMarginUsed)
        {
            if (totalMarginUsed == 0)
            {
                return 0m;
            }

            var totalHoldingsValue = securities.Values.Sum(x => x.Holdings.HoldingsValue);
            if (totalHoldingsValue <= 0)
            {
                return 0m;
            }

            var holdingDays = int.MaxValue;

            foreach (var security in securities)
            {
                // If market was opened this date, check if previous day(s) were closed
                if (security.Value.Exchange.DateIsOpen(applicationTimeUtc))
                {
                    var past = 1;
                    while (!security.Value.Exchange.DateIsOpen(applicationTimeUtc.AddDays(-past)))
                    {
                        past++;
                    }
                    holdingDays = Math.Min(holdingDays, past - 1);
                }
                else
                {
                    holdingDays = -1;
                }
            }

            holdingDays++;

            var factor = _marginInterestRate / _daysPerYear * holdingDays;
            var marginInterest = totalMarginUsed * factor;

            _totalInterestPaid += marginInterest;
            
            return marginInterest;
        }
    }
}