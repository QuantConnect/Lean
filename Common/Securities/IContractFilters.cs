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

namespace QuantConnect.Securities
{
    /// <summary>
    /// The contract filters shared by every derivative universe selection and chain: expirations, contract types and liquidity.
    /// <see cref="IOptionContractFilters{TSelf}"/> and <see cref="IFutureContractFilters{TSelf}"/> add the option and future specific ones
    /// </summary>
    /// <typeparam name="TSelf">The implementing type, returned by every filter for chaining</typeparam>
    public interface IContractFilters<TSelf>
    {
        /// <summary>
        /// Selects the contracts expiring in the given range relative to the current date
        /// </summary>
        TSelf Expiration(TimeSpan minExpiry, TimeSpan maxExpiry);

        /// <summary>
        /// Selects the contracts expiring in the given range of days relative to the current date
        /// </summary>
        TSelf Expiration(int minExpiryDays, int maxExpiryDays);

        /// <summary>
        /// Selects the contracts expiring on any of the given dates, ignoring the time of day
        /// </summary>
        TSelf Expiration(IEnumerable<DateTime> expiries);

        /// <summary>
        /// Selects the contracts expiring after the given date, excluding it
        /// </summary>
        TSelf ExpiringAfter(DateTime date);

        /// <summary>
        /// Selects the contracts expiring before the given date, excluding it
        /// </summary>
        TSelf ExpiringBefore(DateTime date);

        /// <summary>
        /// Selects the contracts expiring today
        /// </summary>
        TSelf ZeroDte();

        /// <summary>
        /// Selects the standard contracts, excluding weeklys
        /// </summary>
        TSelf StandardsOnly();

        /// <summary>
        /// Selects the non standard weekly contracts
        /// </summary>
        TSelf WeeklysOnly();

        /// <summary>
        /// Selects the contracts of the nearest expiration
        /// </summary>
        TSelf FrontMonth();

        /// <summary>
        /// Selects the contracts of the farthest expiration
        /// </summary>
        TSelf FarthestExpiration();

        /// <summary>
        /// Selects the contracts of all expirations but the nearest one
        /// </summary>
        TSelf BackMonths();

        /// <summary>
        /// Selects the contracts of the second nearest expiration
        /// </summary>
        TSelf BackMonth();

        /// <summary>
        /// Selects the contracts with open interest in the given range
        /// </summary>
        TSelf OpenInterest(long min, long max);

        /// <summary>
        /// Selects the contracts with open interest in the given range. Alias for <see cref="OpenInterest"/>
        /// </summary>
        TSelf OI(long min, long max);

        /// <summary>
        /// Selects the contracts with volume in the given range
        /// </summary>
        TSelf Volume(long min, long max);
    }
}
