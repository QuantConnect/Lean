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
using System.Collections.Concurrent;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Securities;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides a base class for all universes to derive from.
    /// </summary>
    public abstract class Universe
    {
        private readonly ConcurrentDictionary<Symbol, Security> _securities;

        /// <summary>
        /// Gets the security type of this universe
        /// </summary>
        public SecurityType SecurityType
        {
            get { return Configuration.SecurityType; }
        }

        /// <summary>
        /// Gets the market of this universe
        /// </summary>
        public string Market
        {
            get { return Configuration.Market; }
        }

        /// <summary>
        /// Gets the settings used for subscriptons added for this universe
        /// </summary>
        public abstract SubscriptionSettings SubscriptionSettings
        {
            get;
        }

        /// <summary>
        /// Gets the configuration used to get universe data
        /// </summary>
        public SubscriptionDataConfig Configuration
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current listing of members in this universe. Modifications
        /// to this dictionary do not change universe membership.
        /// </summary>
        public Dictionary<Symbol, Security> Members
        {
            get { return new Dictionary<Symbol, Security>(_securities); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Universe"/> class
        /// </summary>
        /// <param name="config">The configuration used to source data for this universe</param>
        protected Universe(SubscriptionDataConfig config)
        {
            _securities = new ConcurrentDictionary<Symbol, Security>();

            if (config.FillDataForward) throw new ArgumentException("Universe data can not be fill forward.");
            if (!config.IsInternalFeed) throw new ArgumentException("Universe data must be marked as internal feed.");

            Configuration = config;
        }

        /// <summary>
        /// Determines whether or not the specified security can be removed from
        /// this universe. This is useful to prevent securities from being taken
        /// out of a universe before the algorithm has had enough time to make
        /// decisions on the security
        /// </summary>
        /// <param name="security">The security to check if its ok to remove</param>
        /// <returns>True if we can remove the security, false otherwise</returns>
        public virtual bool CanRemoveMember(Security security)
        {
            return true;
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public abstract IEnumerable<Symbol> SelectSymbols(IEnumerable<BaseData> data);

        /// <summary>
        /// Determines whether or not the specified
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        internal bool ContainsMember(Security security)
        {
            return _securities.ContainsKey(security.Symbol);
        }

        /// <summary>
        /// Adds the specified security to this universe
        /// </summary>
        /// <param name="security">The security to be added</param>
        /// <returns>True if the security was successfully added,
        /// false if the security was already in the universe</returns>
        internal bool AddMember(Security security)
        {
            if (_securities.ContainsKey(security.Symbol))
            {
                return false;
            }
            return _securities.TryAdd(security.Symbol, security);
        }

        /// <summary>
        /// Tries to remove the specified security from the universe. This
        /// will first check to verify that we can remove the security by
        /// calling the <see cref="CanRemoveMember"/> function.
        /// </summary>
        /// <param name="security">The security to be removed</param>
        /// <returns>True if the security was successfully removed, false if
        /// we're not allowed to remove or if the security didn't exist</returns>
        internal bool RemoveMember(Security security)
        {
            if (CanRemoveMember(security))
            {
                return _securities.TryRemove(security.Symbol, out security);
            }
            return false;
        }
    }
}