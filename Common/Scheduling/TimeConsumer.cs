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

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Represents a timer consumer instance
    /// </summary>
    public class TimeConsumer
    {
        /// <summary>
        /// True if the consumer already finished it's work and no longer consumes time
        /// </summary>
        public bool Finished { get; set; }

        /// <summary>
        /// The time provider associated with this consumer
        /// </summary>
        public ITimeProvider TimeProvider { get; set; }

        /// <summary>
        /// The isolator limit provider to be used with this consumer
        /// </summary>
        public IIsolatorLimitResultProvider IsolatorLimitProvider { get; set; }

        /// <summary>
        /// The next time, base on the <see cref="TimeProvider"/>, that time should be requested
        /// to be <see cref="IsolatorLimitProvider"/>
        /// </summary>
        public DateTime? NextTimeRequest { get; set; }
    }
}
