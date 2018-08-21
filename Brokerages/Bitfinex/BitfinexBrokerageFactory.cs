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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Factory method to create Bitfinex Websockets brokerage
    /// </summary>
    public class BitfinexBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public BitfinexBrokerageFactory() : base(typeof(BitfinexBrokerage))
        {
        }

        /// <summary>
        /// Not required
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// provides brokerage connection data
        /// </summary>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "bitfinex-rest" , Config.Get("bitfinex-rest", "https://api.bitfinex.com")},
            { "bitfinex-url" , Config.Get("bitfinex-url", "wss://api.bitfinex.com/ws")},
            { "bitfinex-api-key", Config.Get("bitfinex-api-key")},
            { "bitfinex-api-secret", Config.Get("bitfinex-api-secret")}
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        public override IBrokerageModel BrokerageModel => new BitfinexBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] { "bitfinex-rest", "bitfinex-url", "bitfinex-api-secret", "bitfinex-api-key" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"BitfinexBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var priceProvider = new ApiPriceProvider(job.UserId, job.UserToken);

            var brokerage = new BitfinexBrokerage(
                job.BrokerageData["bitfinex-url"],
                job.BrokerageData["bitfinex-rest"],
                job.BrokerageData["bitfinex-api-key"],
                job.BrokerageData["bitfinex-api-secret"],
                algorithm,
                priceProvider);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
