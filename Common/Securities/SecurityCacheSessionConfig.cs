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
using QuantConnect;

namespace Common.Securities
{
    /// <summary>
    /// Configuration for the SecurityCache's Session,  
    /// </summary>
    public class SecurityCacheSessionConfig
    {
        /// <summary>
        /// True if daily strict end times are enabled
        /// </summary>
        public bool DailyStrictEndTimeEnabled { get; }

        /// <summary>
        /// True to allow extended market hours data, false otherwise
        /// </summary>
        public bool ExtendedMarketHours { get; }

        /// <summary>
        /// The data type
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// Type of the Tick
        /// </summary>
        public TickType TickType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityCacheSessionConfig"/> class
        /// </summary>
        public SecurityCacheSessionConfig(bool dailyStrictEndTimeEnabled, bool extendedMarketHours, Type dataType, TickType tickType)
        {
            DailyStrictEndTimeEnabled = dailyStrictEndTimeEnabled;
            ExtendedMarketHours = extendedMarketHours;
            DataType = dataType;
            TickType = tickType;
        }
    }
}