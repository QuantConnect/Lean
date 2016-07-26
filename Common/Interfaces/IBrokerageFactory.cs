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
using QuantConnect.Brokerages;
using QuantConnect.Packets;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines factory types for brokerages. Every IBrokerage is expected to also implement an IBrokerageFactory.
    /// </summary>
    [InheritedExport(typeof(IBrokerageFactory))]
    public interface IBrokerageFactory : IDisposable
    {
        /// <summary>
        /// Gets the type of brokerage produced by this factory
        /// </summary>
        Type BrokerageType { get; }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        Dictionary<string, string> BrokerageData { get; }

        /// <summary>
        /// Gets a brokerage model that can be used to model this brokerage's unique
        /// behaviors
        /// </summary>
        IBrokerageModel BrokerageModel { get; }

        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm);

        /// <summary>
        /// Gets a brokerage message handler
        /// </summary>
        IBrokerageMessageHandler CreateBrokerageMessageHandler(IAlgorithm algorithm, AlgorithmNodePacket job, IApi api);
    }
}