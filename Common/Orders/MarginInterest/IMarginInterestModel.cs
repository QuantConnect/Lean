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

using QuantConnect.Securities;
using System;

namespace QuantConnect.Orders.MarginInterest
{
    /// <summary>
    /// Represents a model the simulates margin interest
    /// </summary>
    public interface IMarginInterestModel
    {
        /// <summary>
        /// Total interest paid during the algorithm operation across all securities in portfolio.
        /// </summary>
        decimal TotalInterestPaid { get;  }

        /// <summary>
        /// Gets the margin interest associated with the total loan in our portfolio. 
        /// This returns the cost of the borrowed money in the account currency
        /// </summary>
        /// <param name="securities">Securities collection for the portfolio summation</param>
        /// <param name="applicationTimeUtc">Time margin interest payment is made</param>
        /// <param name="totalMarginUsed">Total amount of margin used</param>
        decimal PayMarginInterest(SecurityManager securities, DateTime applicationTimeUtc, decimal totalMarginUsed);
    }
}