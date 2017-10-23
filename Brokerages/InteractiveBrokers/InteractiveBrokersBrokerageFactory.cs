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
using QuantConnect.Logging;
using Newtonsoft.Json;

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
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                var data = new Dictionary<string, string>();
                data.Add("ib-account", Config.Get("ib-account"));
                data.Add("ib-user-name", Config.Get("ib-user-name"));
                data.Add("ib-password", Config.Get("ib-password"));
                data.Add("ib-trading-mode", Config.Get("ib-trading-mode"));
                data.Add("ib-agent-description", Config.Get("ib-agent-description"));
                return data;
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="InteractiveBrokersBrokerageModel"/>
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new InteractiveBrokersBrokerageModel(); }
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

            // read values from the brokerage datas
            var useTws = Config.GetBool("ib-use-tws");
            var port = Config.GetInt("ib-port", 4001);
            var host = Config.Get("ib-host", "127.0.0.1");
            var twsDirectory = Config.Get("ib-tws-dir", "C:\\Jts");
            var ibControllerDirectory = Config.Get("ib-controller-dir", "C:\\IBController");

            var account = Read<string>(job.BrokerageData, "ib-account", errors);
            var userId = Read<string>(job.BrokerageData, "ib-user-name", errors);
            var password = Read<string>(job.BrokerageData, "ib-password", errors);
            var tradingMode = Read<string>(job.BrokerageData, "ib-trading-mode", errors);
            var agentDescription = Read<string>(job.BrokerageData, "ib-agent-description", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(Environment.NewLine, errors));
            }

            if (tradingMode.IsNullOrEmpty())
            {
                throw new Exception("No trading mode selected. Please select either 'paper' or 'live' trading.");
            }

            // launch the IB gateway
            InteractiveBrokersGatewayRunner.Start(ibControllerDirectory, twsDirectory, userId, password, tradingMode, useTws);

            var ib = new InteractiveBrokersBrokerage(algorithm, algorithm.Transactions, algorithm.Portfolio, account, host, port, agentDescription);
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
            InteractiveBrokersGatewayRunner.Stop();
        }
    }
}
