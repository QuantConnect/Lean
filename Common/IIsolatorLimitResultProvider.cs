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

namespace QuantConnect
{
    /// <summary>
    /// Provides an abstraction for managing isolator limit results.
    /// This is originally intended to be used by the training feature to permit a single
    /// algorithm time loop to extend past the default of ten minutes
    /// </summary>
    public interface IIsolatorLimitResultProvider
    {
        /// <summary>
        /// Determines whether or not a custom isolator limit has be reached.
        /// </summary>
        IsolatorLimitResult IsWithinLimit();

        /// <summary>
        /// Requests additional time from the isolator result provider. This is intended
        /// to prevent <see cref="IsWithinLimit"/> from returning an error result.
        /// This method will throw a <see cref="TimeoutException"/> if there is insufficient
        /// resources available to fulfill the specified number of minutes.
        /// </summary>
        /// <param name="minutes">The number of additional minutes to request</param>
        void RequestAdditionalTime(int minutes);

        /// <summary>
        /// Attempts to request additional time from the isolator result provider. This is intended
        /// to prevent <see cref="IsWithinLimit"/> from returning an error result.
        /// This method will only return false if there is insufficient resources available to fulfill
        /// the specified number of minutes.
        /// </summary>
        /// <param name="minutes">The number of additional minutes to request</param>
        bool TryRequestAdditionalTime(int minutes);
    }
}