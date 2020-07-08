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
using QuantConnect.Data;
using QuantConnect.Packets;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Task requester interface with cloud system
    /// </summary>
    /// <remarks>
    /// This interface is the main entrypoint for external live streams of data
    /// to enter the Lean engine. You will need a class deriving from <see cref="BaseData"/>, and you need to convert your data
    /// into an instance of the class deriving from BaseData.
    /// </remarks>
    [InheritedExport(typeof(IDataQueueHandler))]
    public interface IDataQueueHandler : IDisposable
    {
        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of data since the last update</returns>
        IEnumerable<BaseData> GetNextTicks();

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing</param>
        /// <param name="symbols">The symbols to be added</param>
        void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols);

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing</param>
        /// <param name="symbols">The symbols to be removed</param>
        void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols);

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>True if the data provider is connected</returns>
        bool IsConnected { get; }
    }
}
