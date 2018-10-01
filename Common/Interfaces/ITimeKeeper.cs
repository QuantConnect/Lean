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
using NodaTime;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Interface implemented by <see cref="TimeKeeper"/>
    /// </summary>
    public interface ITimeKeeper
    {
        /// <summary>
        /// Gets the current time in UTC
        /// </summary>
        DateTime UtcTime { get; }

        /// <summary>
        /// Adds the specified time zone to this time keeper
        /// </summary>
        /// <param name="timeZone"></param>
        void AddTimeZone(DateTimeZone timeZone);

        /// <summary>
        /// Gets the <see cref="LocalTimeKeeper"/> instance for the specified time zone
        /// </summary>
        /// <param name="timeZone">The time zone whose <see cref="LocalTimeKeeper"/> we seek</param>
        /// <returns>The <see cref="LocalTimeKeeper"/> instance for the specified time zone</returns>
        LocalTimeKeeper GetLocalTimeKeeper(DateTimeZone timeZone);
    }
}
