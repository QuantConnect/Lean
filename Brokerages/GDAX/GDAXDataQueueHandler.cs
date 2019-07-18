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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using RestSharp;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for GDAX
    /// </summary>
    [BrokerageFactory(typeof(GDAXBrokerageFactory))]
    public class GDAXDataQueueHandler : GDAXBrokerage, IDataQueueHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GDAXDataQueueHandler"/> class
        /// </summary>
        public GDAXDataQueueHandler(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm,
            IPriceProvider priceProvider)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, passPhrase, algorithm, priceProvider)
        {
        }

        /// <summary>
        /// The list of websocket channels to subscribe
        /// </summary>
        protected override string[] ChannelNames { get; } = { "heartbeat", "level2", "matches" };

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (TickLocker)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            Subscribe(symbols);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            Unsubscribe(symbols);
        }
    }
}
