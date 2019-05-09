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
 *
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Interface to setup the algorithm. Pass in a raw algorithm, return one with portfolio, cash, etc already preset.
    /// </summary>
    [InheritedExport(typeof(ISetupHandler))]
    public interface ISetupHandler : IDisposable
    {
        /// <summary>
        /// The worker thread instance the setup handler should use
        /// </summary>
        WorkerThread WorkerThread
        {
            set;
        }

        /// <summary>
        /// Any errors from the initialization stored here:
        /// </summary>
        List<Exception> Errors
        {
            get;
            set;
        }

        /// <summary>
        /// Get the maximum runtime for this algorithm job.
        /// </summary>
        TimeSpan MaximumRuntime
        {
            get;
        }

        /// <summary>
        /// Algorithm starting capital for statistics calculations
        /// </summary>
        decimal StartingPortfolioValue
        {
            get;
        }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        DateTime StartingDate
        {
            get;
        }

        /// <summary>
        /// Maximum number of orders for the algorithm run -- applicable for backtests only.
        /// </summary>
        int MaxOrders
        {
            get;
        }

        /// <summary>
        /// Create a new instance of an algorithm from a physical dll path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly's location</param>
        /// <param name="algorithmNodePacket">Details of the task required</param>
        /// <returns>A new instance of IAlgorithm, or throws an exception if there was an error</returns>
        IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath);

        /// <summary>
        /// Creates the brokerage as specified by the job packet
        /// </summary>
        /// <param name="algorithmNodePacket">Job packet</param>
        /// <param name="uninitializedAlgorithm">The algorithm instance before Initialize has been called</param>
        /// <param name="factory">The brokerage factory</param>
        /// <returns>The brokerage instance, or throws if error creating instance</returns>
        IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm, out IBrokerageFactory factory);

        /// <summary>
        /// Primary entry point to setup a new algorithm
        /// </summary>
        /// <param name="parameters">The parameters object to use</param>
        /// <returns>True on successfully setting up the algorithm state, or false on error.</returns>
        bool Setup(SetupHandlerParameters parameters);
    }
}
