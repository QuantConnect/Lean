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
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Ccxt
{
    /// <summary>
    /// Factory type for the <see cref="CcxtBrokerage"/>
    /// </summary>
    public class CcxtBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CcxtBrokerage"/> class
        /// </summary>
        public CcxtBrokerageFactory()
            : base(typeof(CcxtBrokerage))
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                var exchangeName = Config.Get("ccxt-exchange-name");

                return new Dictionary<string, string>
                {
                    { "ccxt-exchange-name", exchangeName },
                    { $"ccxt-{exchangeName}-api-key", Config.Get($"ccxt-{exchangeName}-api-key") },
                    { $"ccxt-{exchangeName}-secret", Config.Get($"ccxt-{exchangeName}-secret") },
                    { $"ccxt-{exchangeName}-password", Config.Get($"ccxt-{exchangeName}-password") }
                };
            }
        }


        /// <summary>
        /// Gets a new instance of the <see cref="DefaultBrokerageModel"/>
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider)
        {
            var exchangeName = Config.Get("ccxt-exchange-name");

            return new CcxtBrokerageModel(exchangeName);
        }

        /// <summary>
        /// Creates a new IBrokerage instance and set ups the environment for the brokerage
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();

            // read values from the brokerage data
            var exchangeName = Read<string>(job.BrokerageData, "ccxt-exchange-name", errors);
            var apiKey = Read<string>(job.BrokerageData, $"ccxt-{exchangeName}-api-key", errors);
            var secret = Read<string>(job.BrokerageData, $"ccxt-{exchangeName}-secret", errors);
            var password = Read<string>(job.BrokerageData, $"ccxt-{exchangeName}-password", errors);

            var brokerage = new CcxtBrokerage(
                algorithm.Transactions,
                exchangeName,
                apiKey,
                secret,
                password,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")));

            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Stops the InteractiveBrokersGatewayRunner
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
