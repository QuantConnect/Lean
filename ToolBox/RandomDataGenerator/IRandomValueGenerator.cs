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

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Defines a type capable of producing random values for use in random data generation
    /// </summary>
    /// <remarks>
    /// Any parameters referenced as a percentage value are always in 'percent space', meaning 1 is 1%.
    /// </remarks>
    public interface IRandomValueGenerator
    {
        /// <summary>
        /// Randomly return a <see cref="bool"/> value with the specified odds of being true
        /// </summary>
        /// <param name="percentOddsForTrue">The percent odds of being true in percent space, so 10 => 10%</param>
        /// <returns>True or false</returns>
        bool NextBool(double percentOddsForTrue);

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0
        /// </summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        double NextDouble();

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">the inclusive lower bound of the random number returned</param>
        /// <param name="maxValue">the exclusive upper bound of the random number returned</param>
        /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue.</returns>
        int NextInt(int minValue, int maxValue);

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">the exclusive upper bound of the random number to be generated.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than maxValue.</returns>
        int NextInt(int maxValue);

        /// <summary>
        /// Generates a random <see cref="DateTime"/> between the specified <paramref name="minDateTime"/> and
        /// <paramref name="maxDateTime"/>. <paramref name="dayOfWeek"/> is optionally specified to force the
        /// result to a particular day of the week
        /// </summary>
        /// <param name="minDateTime">The minimum date time, inclusive</param>
        /// <param name="maxDateTime">The maximum date time, inclusive</param>
        /// <param name="dayOfWeek">Optional. The day of week to force</param>
        /// <returns>A new <see cref="DateTime"/> within the specified range and optionally of the specified day of week</returns>
        DateTime NextDate(DateTime minDateTime, DateTime maxDateTime, DayOfWeek? dayOfWeek);

        /// <summary>
        /// Generates a random <see cref="decimal"/> suitable as a price. This should observe minimum price
        /// variations if available in <see cref="SymbolPropertiesDatabase"/>, and if not, truncating to 2
        /// decimal places.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when the <paramref name="referencePrice"/> or <paramref name="maximumPercentDeviation"/>
        /// is less than or equal to zero.</exception>
        /// <param name="securityType">The security type the price is being generated for</param>
        /// <param name="market">The market of the security the price is being generated for</param>
        /// <param name="referencePrice">The reference price used as the mean of random price generation</param>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// <returns>A new decimal suitable for usage as price within the specified deviation from the reference price</returns>
        decimal NextPrice(
            SecurityType securityType,
            string market,
            decimal referencePrice,
            decimal maximumPercentDeviation
        );
    }
}
