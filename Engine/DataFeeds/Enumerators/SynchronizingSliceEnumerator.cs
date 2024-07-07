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
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Represents an enumerator capable of synchronizing other slice enumerators in time.
    /// This assumes that all enumerators have data time stamped in the same time zone
    /// </summary>
    public class SynchronizingSliceEnumerator : SynchronizingEnumerator<Slice>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizingSliceEnumerator"/> class
        /// </summary>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        public SynchronizingSliceEnumerator(params IEnumerator<Slice>[] enumerators)
            : this((IEnumerable<IEnumerator>)enumerators) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizingSliceEnumerator"/> class
        /// </summary>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        public SynchronizingSliceEnumerator(IEnumerable<IEnumerator> enumerators)
            : base((IEnumerable<IEnumerator<Slice>>)enumerators) { }

        /// <summary>
        /// Gets the Timestamp for the data
        /// </summary>
        protected override DateTime GetInstanceTime(Slice instance)
        {
            return instance.UtcTime;
        }
    }
}
