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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using TradingApi.Bitfinex;

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
        public BitfinexBrokerageFactory()
            : base(typeof(BitfinexBrokerage))
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
                    {"apiSecret" ,Config.Get("bitfinex-api-secret")},
                    {"apiKey" ,Config.Get("bitfinex-api-key")},
                    {"wallet" ,Config.Get("bitfinex-wallet")},
                    {"url" , Config.Get("bitfinex-wss", "wss://api2.bitfinex.com:3000/ws")},
                    {"scaleFactor", Config.Get("bitfinex-scale-factor", "1")}
                };
            }
        }

        /// <summary>
        /// The brokerage model
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new BitfinexBrokerageModel(); }
        }

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override Interfaces.IBrokerage CreateBrokerage(Packets.LiveNodePacket job, Interfaces.IAlgorithm algorithm)
        {

            //it's desirable to throw an exception here when failing parse
            decimal scaleFactor = decimal.Parse(job.BrokerageData["scaleFactor"]);

            if (scaleFactor == 0m)
                throw new Exception("Invalid bitfinex-scale-factor in config.json");

            if (string.IsNullOrEmpty(job.BrokerageData["apiSecret"]))
                throw new Exception("Missing bitfinex-api-secret in config.json");

            if (string.IsNullOrEmpty(job.BrokerageData["apiKey"]))
                throw new Exception("Missing bitfinex-api-key in config.json");

            if (string.IsNullOrEmpty(job.BrokerageData["wallet"]))
                throw new Exception("Missing bitfinex-wallet in config.json");

            var restClient = new BitfinexApi(job.BrokerageData["apiSecret"], job.BrokerageData["apiKey"]);

            var webSocketClient = new WebSocketWrapper();

            var brokerage = new BitfinexWebsocketsBrokerage(job.BrokerageData["url"], webSocketClient, job.BrokerageData["apiKey"], job.BrokerageData["apiSecret"], 
                job.BrokerageData["wallet"], restClient, scaleFactor, algorithm.Portfolio);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
