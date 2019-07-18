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
    /// Represents the result of the <see cref="Isolator"/> limiter callback
    /// </summary>
    public class IsolatorLimitResult
    {
        /// <summary>
        /// Gets the amount of time spent on the current time step
        /// </summary>
        public TimeSpan CurrentTimeStepElapsed { get; }

        /// <summary>
        /// Gets the error message or an empty string if no error on the current time step
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Returns true if there are no errors in the current time step
        /// </summary>
        public bool IsWithinCustomLimits => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatorLimitResult"/> class
        /// </summary>
        /// <param name="currentTimeStepElapsed">The amount of time spent on the current time step</param>
        /// <param name="errorMessage">The error message or an empty string if no error on the current time step</param>
        public IsolatorLimitResult(TimeSpan currentTimeStepElapsed, string errorMessage)
        {
            CurrentTimeStepElapsed = currentTimeStepElapsed;
            ErrorMessage = errorMessage;
        }
    }
}
