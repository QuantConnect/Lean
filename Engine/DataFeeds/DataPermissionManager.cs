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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Entity in charge of handling data permissions
    /// </summary>
    public class DataPermissionManager : IDataPermissionManager
    {
        /// <summary>
        /// The data channel provider instance
        /// </summary>
        public IDataChannelProvider DataChannelProvider { get; private set; }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(DataPermissionManager))]
        public DataPermissionManager()
        {
        }

        /// <summary>
        /// Initialize the data permission manager
        /// </summary>
        /// <param name="job">The job packet</param>
        public virtual void Initialize(AlgorithmNodePacket job)
        {
            var liveJob = job as LiveNodePacket;
            if (liveJob != null)
            {
                Log.Trace($"LiveTradingDataFeed.GetDataChannelProvider(): will use {liveJob.DataChannelProvider}");
                DataChannelProvider = Composer.Instance.GetExportedValueByTypeName<IDataChannelProvider>(liveJob.DataChannelProvider);
                DataChannelProvider.Initialize(liveJob);
            }
        }

        /// <summary>
        /// Will assert the requested configuration is valid for the current job
        /// </summary>
        /// <param name="subscriptionRequest">The data subscription configuration to assert</param>
        /// <param name="startTimeLocal">The start time of this request</param>
        /// <param name="endTimeLocal">The end time of this request</param>
        public virtual void AssertConfiguration(SubscriptionDataConfig subscriptionRequest, DateTime startTimeLocal, DateTime endTimeLocal)
        {
        }
    }
}
