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
using System.ComponentModel.Composition;
using Krs.Ats.IBNet;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Factory type for the InteractiveBrokersBrokerage
    /// </summary>
    public class InteractiveBrokersBrokerageFactory : IBrokerageFactory
    {
        /// <summary>
        /// Gets the brokerage data required to run the IB brokerage from configuration
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for running
        /// live jobs locally with the ConsoleSetupHandler. The implementation must specify the following
        /// attributes:
        ///    [Export(typeof(Dictionary&lt;string, string&gt;))]
        ///    [ExportMetadata("BrokerageData", "{BrokerageTypeNameHere}")]
        /// </remarks>
        [Export(typeof(Dictionary<string, string>))]
        [ExportMetadata("BrokerageData", "InteractiveBrokersBrokerage")]
        public Dictionary<string, string> BrokerageData
        {
            get
            {
                var data = new Dictionary<string, string>();
                data.Add("ib-port", Config.Get("ib-port"));
                data.Add("ib-host", Config.Get("ib-host"));
                data.Add("ib-account", Config.Get("ib-account"));
                data.Add("ib-user-name", Config.Get("ib-user-name"));
                data.Add("ib-password", Config.Get("ib-password"));
                data.Add("ib-agent-description", Config.Get("ib-agent-description"));
                return data;
            }
        }

        /// <summary>
        /// Gets the type of brokerage produced by this factory
        /// </summary>
        public Type BrokerageType
        {
            get { return typeof (InteractiveBrokersBrokerage); }
        }

        /// <summary>
        /// Creates a new IBrokerage instance and set ups the environment for the brokerage
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            // launch the IB gateway
            InteractiveBrokersGatewayRunner.Start(job.AccountId);

            var errors = new List<string>();

            // read values from the brokerage datas
            var port = Read<int>(job.BrokerageData, "ib-port", errors);
            var host = Read<string>(job.BrokerageData, "ib-host", errors);
            var account = Read<string>(job.BrokerageData, "ib-account", errors);
            var agentDescription = Read<AgentDescription>(job.BrokerageData, "ib-agent-description", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(Environment.NewLine, errors));
            }

            return new InteractiveBrokersBrokerage(algorithm.Transactions, account, host, port, agentDescription);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Stops the InteractiveBrokersGatewayRunner
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            InteractiveBrokersGatewayRunner.Stop();
        }

        /// <summary>
        /// Reads a value from the brokerage data, adding an error if the key is not found
        /// </summary>
        private static T Read<T>(IReadOnlyDictionary<string, string> brokerageData, string key, ICollection<string> errors) 
            where T : IConvertible
        {
            string value;
            if (!brokerageData.TryGetValue(key, out value))
            {
                errors.Add("Missing key: " + key);
                return default(T);
            }

            try
            {
                return value.ConvertTo<T>();
            }
            catch (Exception err)
            {
                errors.Add(string.Format("Error converting {0} with value {1}. {2}", key, value, err.Message));
                return default(T);
            }
        }
    }
}
