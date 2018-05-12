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
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Kraken {
    /// <summary>
    /// Provides an implementations of <see cref="IBrokerageFactory"/> that produces a <see cref="KrakenBrokerage"/>
    /// </summary>
    public class KrakenBrokerageFactory: BrokerageFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KrakenBrokerageFactory"/> class.
        /// </summary>
        public KrakenBrokerageFactory() 
            : base(typeof(KrakenBrokerage))
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "kraken-environment", Config.Get("kraken-environment") },
                    { "kraken-access-token", Config.Get("kraken-access-token") },
                    { "kraken-account-id", Config.Get("kraken-account-id") },
                    { "kraken-agent", Config.Get("kraken-agent", "kraken-default-agent") }
                };
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="OandaBrokerageModel"/>
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new OandaBrokerageModel(); }
        }

        /// <summary>
        /// Creates a new <see cref="IBrokerage"/> instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm) {
            
            var required = new[] { "kraken-access-token", "kraken-account-id" };

            foreach (var item in required) {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"KrakenBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var errors = new List<string>();

            var accessToken = Read<string>(job.BrokerageData, "kraken-access-token", errors);
            var accountId   = Read<string>(job.BrokerageData, "kraken-account-id", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(System.Environment.NewLine, errors));
            }

            //var brokerage = new KrakenBrokerage(algorithm.Transactions, algorithm.Portfolio, environment, accessToken, accountId, agent);
            var brokerage = new KrakenBrokerage(accessToken, accountId);

            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }

    }
}
