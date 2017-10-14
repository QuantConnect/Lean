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
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
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
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "gdax-url" , Config.Get("gdax-url", "wss://ws-feed.gdax.com")},
                    { "gdax-api-secret", Config.Get("gdax-api-secret")},
                    { "gdax-api-key", Config.Get("gdax-api-key")},
                    { "gdax-passphrase", Config.Get("gdax-passphrase")}
                };
            }
        }

        /// <summary>
        /// The brokerage model
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new GDAXBrokerageModel(); }
        }

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override Interfaces.IBrokerage CreateBrokerage(Packets.LiveNodePacket job, Interfaces.IAlgorithm algorithm)
        {
            var required = new[] { "gdax-url", "gdax-api-secret", "gdax-api-key", "gdax-passphrase" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception(string.Format("GDAXBrokerageFactory.CreateBrokerage: Missing {0} in config.json", item));
            }

            var restClient = new RestClient("https://api.gdax.com");
            var webSocketClient = new WebSocketWrapper();

            var brokerage = new GDAXBrokerage(job.BrokerageData["gdax-url"], webSocketClient, restClient, job.BrokerageData["gdax-api-key"], job.BrokerageData["gdax-api-secret"],
                job.BrokerageData["gdax-passphrase"], algorithm);

            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
