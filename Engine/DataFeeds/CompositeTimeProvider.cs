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
using System.Linq;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// The composite time provider will source it's current time using the smallest time from the given providers
    /// </summary>
    public class CompositeTimeProvider : ITimeProvider
    {
        private readonly ITimeProvider[] _timeProviders;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="timeProviders">The time providers to use. Will default to the real time provider if empty</param>
        public CompositeTimeProvider(IEnumerable<ITimeProvider> timeProviders)
        {
            _timeProviders = timeProviders.DefaultIfEmpty(RealTimeProvider.Instance).ToArray();
        }

        /// <summary>
        /// Gets the current time in UTC
        /// </summary>
        /// <returns>The current time in UTC</returns>
        public DateTime GetUtcNow()
        {
            var result = DateTime.MaxValue;
            for (var i = 0; i < _timeProviders.Length; i++)
            {
                var utcNow = _timeProviders[i].GetUtcNow();

                if (utcNow < result)
                {
                    // we return the smallest
                    result = utcNow;
                }
            }
            return result;
        }
    }
}
