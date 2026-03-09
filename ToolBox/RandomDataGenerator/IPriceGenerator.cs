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

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Defines a type capable of producing random prices
    /// </summary>
    /// <remarks>
    /// Any parameters referenced as a percentage value are always in 'percent space', meaning 1 is 1%.
    /// </remarks>
    public interface IPriceGenerator
    {
        /// <summary>
        /// Generates an asset price
        /// </summary>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// <param name="referenceDate">date used in price calculation</param>
        /// <returns>Returns a new decimal as price</returns>
        public decimal NextValue(decimal maximumPercentDeviation, DateTime referenceDate);

        /// <summary>
        /// Indicates Price generator warmed up and ready to generate new values
        /// </summary>
        public bool WarmedUp { get; }
    }
}
