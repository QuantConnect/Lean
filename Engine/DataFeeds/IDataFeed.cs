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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Datafeed interface for creating custom datafeed sources.
    /// </summary>
    [InheritedExport]
    public interface IDataFeed
    {
        /// <summary>
        /// List of the subscription the algorithm has requested. Subscriptions contain the type, sourcing information and manage the enumeration of data.
        /// </summary>
        List<SubscriptionDataConfig> Subscriptions
        {
            get;
        }

        /// <summary>
        /// Prices of the datafeed this instant for dynamically updating security values (and calculation of the total portfolio value in realtime).
        /// </summary>
        /// <remarks>Indexed in order of the subscriptions</remarks>
        List<decimal> RealtimePrices
        {
            get;
        }

        /// <summary>
        /// Cross-threading queues so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        ConcurrentQueue<List<BaseData>>[] Bridge
        {
            get;
            set;
        }

        /// <summary>
        /// Boolean flag indicating there is no more data in any of our subscriptions.
        /// </summary>
        bool EndOfBridges
        {
            get;
        }

        /// <summary>
        /// Array of boolean flags indicating the data status for each queue/subscription we're tracking.
        /// </summary>
        bool[] EndOfBridge
        {
            get;
        }

        /// <summary>
        /// Set the source of the data we're requesting for the type-readers to know where to get data from.
        /// </summary>
        /// <remarks>Live or Backtesting Datafeed</remarks>
        DataFeedEndpoint DataFeed
        {
            get;
            set;
        }

        /// <summary>
        /// Public flag indicator that the thread is still busy.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// The most advanced moment in time for which the data feed has completed loading data
        /// </summary>
        DateTime LoadedDataFrontier { get; }

        /// <summary>
        /// Data has completely loaded and we don't expect any more.
        /// </summary>
        bool LoadingComplete
        {
            get;
        }

        /// <summary>
        /// Initializes the data feed for the specified job and algorithm
        /// </summary>
        void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job);

        /// <summary>
        /// Primary entry point.
        /// </summary>
        void Run();

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        void Exit();

        /// <summary>
        /// Purge all remaining data in the thread.
        /// </summary>
        void PurgeData();
    }
}
