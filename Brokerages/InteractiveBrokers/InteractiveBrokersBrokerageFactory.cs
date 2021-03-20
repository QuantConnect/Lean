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

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Factory type for the <see cref="InteractiveBrokersBrokerage"/>
    /// </summary>
    public class InteractiveBrokersBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Initializes a new instance of the InteractiveBrokersBrokerageFactory class
        /// </summary>
        public InteractiveBrokersBrokerageFactory()
            : base(typeof(InteractiveBrokersBrokerage))
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the IB brokerage from configuration
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "ib-account", Config.Get("ib-account") },
            { "ib-user-name", Config.Get("ib-user-name") },
            { "ib-password", Config.Get("ib-password") },
            { "ib-trading-mode", Config.Get("ib-trading-mode") },
            { "ib-agent-description", Config.Get("ib-agent-description") }
        };

        /// <summary>
        /// Gets a new instance of the <see cref="InteractiveBrokersBrokerageModel"/>
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new InteractiveBrokersBrokerageModel();

        /// <summary>
        /// Creates a new IBrokerage instance and set ups the environment for the brokerage
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();

            // read values from the brokerage datas
            var port = Config.GetInt("ib-port", 4001);
            var host = Config.Get("ib-host", "127.0.0.1");
            var twsDirectory = Config.Get("ib-tws-dir", "C:\\Jts");
            var ibVersion = Config.Get("ib-version", "974");

            var account = Read<string>(job.BrokerageData, "ib-account", errors);
            var userId = Read<string>(job.BrokerageData, "ib-user-name", errors);
            var password = Read<string>(job.BrokerageData, "ib-password", errors);
            var tradingMode = Read<string>(job.BrokerageData, "ib-trading-mode", errors);
            var agentDescription = Read<string>(job.BrokerageData, "ib-agent-description", errors);

            var loadExistingHoldings = true;
            if (job.BrokerageData.ContainsKey("load-existing-holdings"))
            {
                loadExistingHoldings = Convert.ToBoolean(job.BrokerageData["load-existing-holdings"]);
            }

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(Environment.NewLine, errors));
            }

            if (tradingMode.IsNullOrEmpty())
            {
                throw new Exception("No trading mode selected. Please select either 'paper' or 'live' trading.");
            }

            var ib = new InteractiveBrokersBrokerage(
                algorithm,
                algorithm.Transactions,
                algorithm.Portfolio,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider")),
                account,
                host,
                port,
                twsDirectory,
                ibVersion,
                userId,
                password,
                tradingMode,
                agentDescription,
                loadExistingHoldings);
            Composer.Instance.AddPart<IDataQueueHandler>(ib);

            return ib;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Stops the InteractiveBrokersGatewayRunner
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
        }
    }
}
