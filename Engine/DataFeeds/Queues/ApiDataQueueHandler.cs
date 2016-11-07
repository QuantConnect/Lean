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

using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// Bind to a live data websocket connection
    /// </summary>
    public class ApiDataQueueHandler : IDataQueueHandler
    {
        private readonly Api.Api _api;

        /// <summary>
        /// Constructor that initializes Api Connection
        /// </summary>
        public ApiDataQueueHandler()
        {
            _api = new Api.Api();
            _api.Initialize(Config.GetInt("job-user-id", 0), Config.Get("api-access-token", ""), Config.Get("data-folder"));
        }

        /// <summary>
        /// Get next ticks if they have arrived from the server.
        /// </summary>
        /// <returns>Array of <see cref="BaseData"/></returns>
        public virtual IEnumerable<BaseData> GetNextTicks()
        {
            return _api.GetLiveData();
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public virtual void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            _api.LiveSubscribe(symbols);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job that's being processed processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public virtual void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            _api.LiveUnsubscribe(symbols);
        }
    }
}
