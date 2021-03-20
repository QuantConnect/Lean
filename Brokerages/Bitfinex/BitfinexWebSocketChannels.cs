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

using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Contains the channel mappings for a WebSocket connection
    /// </summary>
    public class BitfinexWebSocketChannels : ConcurrentDictionary<int, Channel>
    {
        /// <summary>
        /// Determines whether the dictionary contains a specific channel.
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <returns>true if the channel was found</returns>
        public bool Contains(Channel channel)
        {
            return Values.Contains(channel);
        }

        /// <summary>
        /// Returns the channel id for the given channel.
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <returns>The channel id</returns>
        public int GetChannelId(Channel channel)
        {
            return this.First(c => c.Value.Equals(channel)).Key;
        }
    }
}
