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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// Factory method to create GDAX Websockets brokerage
    /// </summary>
    public class GDAXBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public GDAXBrokerageFactory() : base(typeof(GDAXBrokerage))
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
            { "gdax-url" , Config.Get("gdax-url", "wss://ws-feed.pro.coinbase.com")},
            { "gdax-api-secret", Config.Get("gdax-api-secret")},
            { "gdax-api-key", Config.Get("gdax-api-key")},
            { "gdax-passphrase", Config.Get("gdax-passphrase")}
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new GDAXBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] { "gdax-url", "gdax-api-secret", "gdax-api-key", "gdax-passphrase" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"GDAXBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var restClient = new RestClient("https://api.pro.coinbase.com");
            var webSocketClient = new WebSocketClientWrapper();
            var priceProvider = new ApiPriceProvider(job.UserId, job.UserToken);
            var aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));

            IBrokerage brokerage;
            if (job.DataQueueHandler.EndsWith("GDAXDataQueueHandler"))
            {
                var dataQueueHandler = new GDAXDataQueueHandler(job.BrokerageData["gdax-url"], webSocketClient,
                    restClient, job.BrokerageData["gdax-api-key"], job.BrokerageData["gdax-api-secret"],
                    job.BrokerageData["gdax-passphrase"], algorithm, priceProvider, aggregator);

                Composer.Instance.AddPart<IDataQueueHandler>(dataQueueHandler);

                brokerage = dataQueueHandler;
            }
            else
            {
                brokerage = new GDAXBrokerage(job.BrokerageData["gdax-url"], webSocketClient,
                    restClient, job.BrokerageData["gdax-api-key"], job.BrokerageData["gdax-api-secret"],
                    job.BrokerageData["gdax-passphrase"], algorithm, priceProvider, aggregator);
            }

            return brokerage;
        }
    }
}
