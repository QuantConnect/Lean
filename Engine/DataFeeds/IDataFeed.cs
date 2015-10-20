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
using System.ComponentModel.Composition;
using System.Threading;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Delegate type for the <see cref="IDataFeed.UniverseSelection"/> event
    /// </summary>
    /// <param name="args">The event arguments</param>
    /// <returns>Changes requested via universe selection</returns>
    public delegate SecurityChanges UniverseSelectionHandler(IDataFeed sender, UniverseSelectionEventArgs args);

    /// <summary>
    /// Datafeed interface for creating custom datafeed sources.
    /// </summary>
    [InheritedExport(typeof(IDataFeed))]
    public interface IDataFeed
    {
        /// <summary>
        /// Event fired when the data feed encounters new fundamental data.
        /// This event must be fired when there is nothing in the <see cref="Bridge"/>,
        /// this can be accomplished using <see cref="BusyBlockingCollection{T}.Wait(int,CancellationToken)"/>
        /// </summary>
        event UniverseSelectionHandler UniverseSelection;
        
        /// <summary>
        /// Gets all of the current subscriptions this data feed is processing
        /// </summary>
        IEnumerable<Subscription> Subscriptions
        {
            get;
        }

        /// <summary>
        /// Cross-threading queue so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        BusyBlockingCollection<TimeSlice> Bridge
        {
            get;
        }

        /// <summary>
        /// Public flag indicator that the thread is still busy.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Initializes the data feed for the specified job and algorithm
        /// </summary>
        void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler);

        /// <summary>
        /// Adds a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="universe">The universe the subscription is to be added to</param>
        /// <param name="security">The security to add a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        /// <returns>True if the subscription was created and added successfully, false otherwise</returns>
        bool AddSubscription(Universe universe, Security security, DateTime utcStartTime, DateTime utcEndTime);

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to be removed</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        bool RemoveSubscription(Subscription subscription);

        /// <summary>
        /// Primary entry point.
        /// </summary>
        void Run();

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        void Exit();
    }
}
