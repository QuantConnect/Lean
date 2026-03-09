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
 *
*/

using System;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Throw this if there is an exception in the callback function of the scheduled event
    /// </summary>
    public class ScheduledEventException : Exception
    {
        /// <summary>
        /// Gets the name of the scheduled event
        /// </summary>
        public string ScheduledEventName { get; }

        /// <summary>
        /// ScheduledEventException constructor
        /// </summary>
        /// <param name="name">The name of the scheduled event</param>
        /// <param name="message">The exception as a string</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public ScheduledEventException(string name, string message, Exception innerException) : base(message, innerException)
        {
            ScheduledEventName = name;
        }
    }
}