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
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Provides an implementations of <see cref="IBrokerageFactory"/> that produces a <see cref="OandaBrokerage"/>
    /// </summary>
    public class OandaBrokerageFactory: BrokerageFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OandaBrokerageFactory"/> class.
        /// </summary>
        public OandaBrokerageFactory()
            : base(typeof(OandaBrokerage))
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
                    { "oanda-environment", Config.Get("oanda-environment") },
                    { "oanda-access-token", Config.Get("oanda-access-token") },
                    { "oanda-account-id", Config.Get("oanda-account-id") },
                    { "oanda-agent", Config.Get("oanda-agent", OandaRestApiBase.OandaAgentDefaultValue) }
                };
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="OandaBrokerageModel"/>
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new OandaBrokerageModel();

        /// <summary>
        /// Creates a new <see cref="IBrokerage"/> instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();

            // read values from the brokerage data
            var environment = Read<Environment>(job.BrokerageData, "oanda-environment", errors);
            var accessToken = Read<string>(job.BrokerageData, "oanda-access-token", errors);
            var accountId = Read<string>(job.BrokerageData, "oanda-account-id", errors);
            var agent = Read<string>(job.BrokerageData, "oanda-agent", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(System.Environment.NewLine, errors));
            }

            var brokerage = new OandaBrokerage(
                algorithm.Transactions, 
                algorithm.Portfolio,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                environment, 
                accessToken, 
                accountId, 
                agent);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }

    }
}
