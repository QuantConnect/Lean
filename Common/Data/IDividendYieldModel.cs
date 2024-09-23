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
    /// Represents a model that provides dividend yield data
    /// </summary>
    public interface IDividendYieldModel
    {
        /// <summary>
        /// Get dividend yield by a given date of a given symbol
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Dividend yield on the given date of the given symbol</returns>
        decimal GetDividendYield(DateTime date);

        /// <summary>
        /// Get dividend yield at given date and security price
        /// </summary>
        /// <param name="date">The date</param>
        /// <param name="securityPrice">The security price at the given date</param>
        /// <returns>Dividend yield on the given date of the given symbol</returns>
        public decimal GetDividendYield(DateTime date, decimal securityPrice);
    }
}
