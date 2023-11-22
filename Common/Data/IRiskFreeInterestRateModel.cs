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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents a model that provides risk free interest rate data
    /// </summary>
    public interface IRiskFreeInterestRateModel
    {
        /// <summary>
        /// Get interest rate by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Interest rate on the given date</returns>
        decimal GetInterestRate(DateTime date);
    }

    /// <summary>
    /// Provide extension and static methods for <see cref="IRiskFreeInterestRateModel"/>
    /// </summary>
    public static class RiskFreeInterestRateModelExtensions
    {
        /// <summary>
        /// Gets the average risk free annual return rate
        /// </summary>
        /// <param name="model">The interest rate model</param>
        /// <param name="startDate">Start date to calculate the average</param>
        /// <param name="endDate">End date to calculate the average</param>
        public static decimal GetRiskFreeRate(this IRiskFreeInterestRateModel model, DateTime startDate, DateTime endDate)
        {
            return model.GetAverageRiskFreeRate(Time.EachDay(startDate, endDate));
        }

        /// <summary>
        /// Gets the average Risk Free Rate from the interest rate of the given dates
        /// </summary>
        /// <param name="model">The interest rate model</param>
        /// <param name="dates">
        /// Collection of dates from which the interest rates will be computed and then the average of them
        /// </param>
        public static decimal GetAverageRiskFreeRate(this IRiskFreeInterestRateModel model, IEnumerable<DateTime> dates)
        {
            var interestRates = dates.Select(x => model.GetInterestRate(x)).DefaultIfEmpty(0);
            return interestRates.Average();
        }
    }
}
