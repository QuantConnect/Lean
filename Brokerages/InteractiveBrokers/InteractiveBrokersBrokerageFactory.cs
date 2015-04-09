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

            return new InteractiveBrokersBrokerage(algorithm.Transactions, job.AccountId);
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
    }
}
