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
using QuantConnect.Util;

namespace QuantConnect.Data.Custom.Intrinio
{
    /// <summary>
    ///     Auxiliary class to access all Intrinio API data.
    /// </summary>
    public static class IntrinioConfig
    {
        /// <summary>
        /// </summary>
        public static RateGate RateGate =
            new RateGate(1, TimeSpan.FromMilliseconds(5000));

        /// <summary>
        ///     Check if Intrinio API user and password are not empty or null.
        /// </summary>
        public static bool IsInitialized => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);

        /// <summary>
        ///     Intrinio API password
        /// </summary>
        public static string Password = string.Empty;

        /// <summary>
        ///     Intrinio API user
        /// </summary>
        public static string User = string.Empty;

        /// <summary>
        /// Sets the time interval between calls.
        /// For more information, please refer to: https://intrinio.com/documentation/api#limits
        /// </summary>
        /// <param name="timeSpan">Time interval between to consecutive calls.</param>
        /// <remarks>
        /// Paid subscription has limits of 1 call per second.
        /// Free subscription has limits of 1 call per minute.
        /// </remarks>
        public static void SetTimeIntervalBetweenCalls(TimeSpan timeSpan)
        {
            RateGate = new RateGate(1, timeSpan);
        }

        /// <summary>
        ///     Set the Intrinio API user and password.
        /// </summary>
        public static void SetUserAndPassword(string user, string password)
        {
            User = user;
            Password = password;

            if (!IsInitialized)
            {
                throw new InvalidOperationException("Please set a valid Intrinio user and password.");
            }
        }
    }
}