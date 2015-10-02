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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents a universe subscription, that is, a data subscription used to
    /// provide data for universe selection purposes
    /// </summary>
    public class UniverseSubscription : Subscription
    {
        /// <summary>
        /// Gets the universe for this subscription
        /// </summary>
        public IUniverse Universe
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSubscription"/> class
        /// </summary>
        /// <param name="universe">The universe for this subscription</param>
        /// <param name="security">The security this subscription is for</param>
        /// <param name="enumerator">The subscription's data source</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        public UniverseSubscription(IUniverse universe, Security security, IEnumerator<BaseData> enumerator, DateTime utcStartTime, DateTime utcEndTime)
            : base(security, enumerator, utcStartTime, utcEndTime, false, true)
        {
            Universe = universe;
        }
    }
}