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
    /// Represents a model that provides dividend yield data
    /// </summary>
    public interface IDividendYieldModel
    {
        /// <summary>
        /// Get dividend yield by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Dividend yield on the given date</returns>
        decimal GetDividendYield(DateTime date);
    }

    /// <summary>
    /// Provide extension and static methods for <see cref="IDividendYieldModel"/>
    /// </summary>
    public static class DividendYieldModelExtensions
    {
        /// <summary>
        /// Gets the average dividend yield
        /// </summary>
        /// <param name="model">The dividend yield model</param>
        /// <param name="startDate">Start date to calculate the average</param>
        /// <param name="endDate">End date to calculate the average</param>
        public static decimal GetDividendYield(this IDividendYieldModel model, DateTime startDate, DateTime endDate)
        {
            return model.GetAverageDividendYield(Time.EachDay(startDate, endDate));
        }

        /// <summary>
        /// Gets the average dividend yield from the dividend yield of the given dates
        /// </summary>
        /// <param name="model">The dividend yield model</param>
        /// <param name="dates">
        /// Collection of dates from which the dividend yield will be computed and then the average of them
        /// </param>
        public static decimal GetAverageDividendYield(this IDividendYieldModel model, IEnumerable<DateTime> dates)
        {
            var dividendYields = dates.Select(x => model.GetDividendYield(x)).DefaultIfEmpty(0);
            return dividendYields.Average();
        }
    }
}
