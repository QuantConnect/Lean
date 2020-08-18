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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Specifies data channel settings
    /// </summary>
    public class DataChannelProvider : IDataChannelProvider
    {
        /// <summary>
        /// True if this subscription request should be streamed
        /// </summary>
        public virtual bool ShouldStreamSubscription(LiveNodePacket job, SubscriptionDataConfig config)
        {
            return IsStreamingType(config) || !config.IsCustomData && config.Type != typeof(CoarseFundamental);
        }

        /// <summary>
        /// Returns true if the data type for the given subscription configuration supports streaming
        /// </summary>
        protected static bool IsStreamingType(SubscriptionDataConfig configuration)
        {
            var dataTypeInstance = configuration.Type.GetBaseDataInstance();
            return dataTypeInstance.GetSource(configuration, DateTime.UtcNow, true)
                       .TransportMedium == SubscriptionTransportMedium.Streaming;
        }
    }
}
