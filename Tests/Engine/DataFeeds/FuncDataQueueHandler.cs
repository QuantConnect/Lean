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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataQueueHandler"/> that can be specified
    /// via a function
    /// </summary>
    public class FuncDataQueueHandler : IDataQueueHandler
    {
        private readonly object _lock = new object();
        private readonly HashSet<SymbolSecurityType> _subscriptions = new HashSet<SymbolSecurityType>();
        private readonly Func<FuncDataQueueHandler, IEnumerable<BaseData>> _getNextTicksFunction;

        /// <summary>
        /// Gets the subscriptions currently being managed by the queue handler
        /// </summary>
        public List<SymbolSecurityType> Subscriptions
        {
            get { lock (_lock) return _subscriptions.ToList(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataQueueHandler"/> class
        /// </summary>
        /// <param name="getNextTicksFunction">The functional implementation for the <see cref="GetNextTicks"/> function</param>
        public FuncDataQueueHandler(Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction)
        {
            _getNextTicksFunction = getNextTicksFunction;
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            return _getNextTicksFunction(this);
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var kvp in symbols)
            {
                foreach (var item in kvp.Value)
                {
                    lock (_lock) _subscriptions.Add(new SymbolSecurityType(item, kvp.Key));
                }
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var kvp in symbols)
            {
                foreach (var item in kvp.Value)
                {
                    lock (_lock) _subscriptions.Remove(new SymbolSecurityType(item, kvp.Key));
                }
            }
        }
    }
}