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
        public Type BrokerageType
        {
            get { return typeof (InteractiveBrokersBrokerage); }
        }

        public IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            // launch the IB gateway
            InteractiveBrokersGatewayRunner.Start(job.AccountId);

            // this needs to be fixed, LiveNodePacket.AccountId must be a string
            var orderMapping = algorithm.Transactions;
            return new InteractiveBrokersBrokerage(orderMapping, job.AccountId);
        }
    }
}
