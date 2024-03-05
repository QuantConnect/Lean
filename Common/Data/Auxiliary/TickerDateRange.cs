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


namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents stock data for a specific ticker within a date range.
    /// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public readonly struct TickerDateRange
    {
        /// <summary>
        /// Ticker simple name of stock
        /// </summary>
        public string Ticker { get; }

        /// <summary>
        /// Ticker Start Date Time in UTC
        /// </summary>
        public DateTime StartDateTimeUtc { get; }

        /// <summary>
        /// Ticker End Date Time in UTC
        /// </summary>
        public DateTime EndDateTimeUtc { get; }

        /// <summary>
        /// Create the instance of <see cref="TickerDateRange"/> struct.
        /// </summary>
        /// <param name="ticker">Name of ticker</param>
        /// <param name="startDateTimeUtc">Start Date Time in UTC</param>
        /// <param name="endDateTimeUtc">End Date Time in UTC</param>
        public TickerDateRange(string ticker, DateTime startDateTimeUtc, DateTime endDateTimeUtc)
        {
            Ticker = ticker;
            StartDateTimeUtc = startDateTimeUtc;
            EndDateTimeUtc = endDateTimeUtc;
        }
    }
#pragma warning restore CA1815
}
