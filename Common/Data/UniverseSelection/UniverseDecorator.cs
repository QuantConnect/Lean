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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides an implementation of <see cref="UniverseSelection.Universe"/> that redirects all calls to a
    /// wrapped (or decorated) universe. This provides scaffolding for other decorators who
    /// only need to override one or two methods.
    /// </summary>
    /// <remarks> Requires special handling due to `this != this.Universe` 
    /// <see cref="GetSubscriptionRequests(Security, DateTime, DateTime, ISubscriptionDataConfigService)"/></remarks>
    public abstract class UniverseDecorator : Universe
    {
        /// <summary>
        /// The decorated universe instance
        /// </summary>
        protected readonly Universe Universe;

        /// <summary>
        /// Gets the settings used for subscriptions added for this universe
        /// </summary>
        public override UniverseSettings UniverseSettings
        {
            get
            {
                return Universe.UniverseSettings;
            }
            set
            {
                Universe.UniverseSettings = value;
            }
        }

        /// <summary>
        /// Gets the internal security collection used to define membership in this universe
        /// </summary>
        public override ConcurrentDictionary<Symbol, Member> Securities
        {
            get { return Universe.Securities; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseDecorator"/> class
        /// </summary>
        /// <param name="universe">The decorated universe. All overridable methods delegate to this instance.</param>
        protected UniverseDecorator(Universe universe)
            : base(universe.Configuration)
        {
            Universe = universe;
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <returns>All subscriptions required by this security</returns>
        [Obsolete("This overload is obsolete and will not be called. It was not capable of creating new SubscriptionDataConfig due to lack of information")]
        public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security, DateTime currentTimeUtc, DateTime maximumEndTimeUtc)
        {
            return Universe.GetSubscriptionRequests(security, currentTimeUtc, maximumEndTimeUtc);
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <param name="subscriptionService">Instance which implements <see cref="ISubscriptionDataConfigService"/> interface</param>
        /// <returns>All subscriptions required by this security</returns>
        public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security,
            DateTime currentTimeUtc,
            DateTime maximumEndTimeUtc,
            ISubscriptionDataConfigService subscriptionService)
        {
            var result = Universe.GetSubscriptionRequests(
                security,
                currentTimeUtc,
                maximumEndTimeUtc,
                subscriptionService).ToList();

            for (var i = 0; i < result.Count; i++)
            {
                // This is required because the system tracks which universe
                // is requesting to add or remove each SubscriptionRequest.
                // UniverseDecorator is a special case because
                // `this != UniverseDecorator.Universe`
                result[i] =
                    new SubscriptionRequest(result[i], universe: this);
            }

            return result;
        }

        /// <summary>
        /// Determines whether or not the specified security can be removed from
        /// this universe. This is useful to prevent securities from being taken
        /// out of a universe before the algorithm has had enough time to make
        /// decisions on the security
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="security">The security to check if its ok to remove</param>
        /// <returns>True if we can remove the security, false otherwise</returns>
        public override bool CanRemoveMember(DateTime utcTime, Security security)
        {
            return Universe.CanRemoveMember(utcTime, security);
        }

        /// <summary>
        /// Creates and configures a security for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol of the security to be created</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="marketHoursDatabase">The market hours database</param>
        /// <param name="symbolPropertiesDatabase">The symbol properties database</param>
        /// <returns>The newly initialized security object</returns>
        [Obsolete("CreateSecurity is obsolete and will not be called. The system will create the required Securities based on selected symbols")]
        public override Security CreateSecurity(Symbol symbol, IAlgorithm algorithm, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase)
        {
            return Universe.CreateSecurity(symbol, algorithm, marketHoursDatabase, symbolPropertiesDatabase);
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return Universe.SelectSymbols(utcTime, data);
        }
    }
}
