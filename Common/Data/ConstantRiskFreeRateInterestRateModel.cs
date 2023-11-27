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

namespace QuantConnect.Data
{
    /// <summary>
    /// Constant risk free rate interest rate model
    /// </summary>
    public class ConstantRiskFreeRateInterestRateModel : IRiskFreeInterestRateModel
    {
        private readonly decimal _riskFreeRate;

        /// <summary>
        /// Instantiates a <see cref="ConstantRiskFreeRateInterestRateModel"/> with the specified risk free rate
        /// </summary>
        public ConstantRiskFreeRateInterestRateModel(decimal riskFreeRate)
        {
            _riskFreeRate = riskFreeRate;
        }

        /// <summary>
        /// Get interest rate by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Interest rate on the given date</returns>
        public decimal GetInterestRate(DateTime date)
        {
            return _riskFreeRate;
        }
    }
}
