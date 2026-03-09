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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Option;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines a universe for a single option chain
    /// </summary>
    public class OptionChainUniverse : Universe
    {
        private readonly OptionFilterUniverse _optionFilterUniverse;
        // as an array to make it easy to prepend to selected symbols
        private readonly Symbol[] _underlyingSymbol;

        /// <summary>
        /// True if this universe filter can run async in the data stack
        /// </summary>
        public override bool Asynchronous
        {
            get
            {
                if (UniverseSettings.Asynchronous.HasValue)
                {
                    return UniverseSettings.Asynchronous.Value;
                }
                return Option.ContractFilter.Asynchronous;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainUniverse"/> class
        /// </summary>
        /// <param name="option">The canonical option chain security</param>
        /// <param name="universeSettings">The universe settings to be used for new subscriptions</param>
        public OptionChainUniverse(Option option,
            UniverseSettings universeSettings)
            : base(option.SubscriptionDataConfig)
        {
            Option = option;
            UniverseSettings = universeSettings;
            _underlyingSymbol = new[] { Option.Symbol.Underlying };
            _optionFilterUniverse = new OptionFilterUniverse(Option);
        }

        /// <summary>
        /// The canonical option chain security
        /// </summary>
        public Option Option { get; }

        /// <summary>
        /// Gets the settings used for subscriptons added for this universe
        /// </summary>
        public override UniverseSettings UniverseSettings
        {
            set
            {
                if (value != null)
                {
                    // make sure data mode is raw
                    base.UniverseSettings = new UniverseSettings(value) { DataNormalizationMode = DataNormalizationMode.Raw };
                }
            }
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            var localEndTime = utcTime.ConvertFromUtc(Option.Exchange.TimeZone);
            // we will only update unique strikes when there is an exchange date change
            _optionFilterUniverse.Refresh(data.Data.Cast<OptionUniverse>().ToList(), data.Underlying, localEndTime);

            var results = Option.ContractFilter.Filter(_optionFilterUniverse);

            // always prepend the underlying symbol
            return _underlyingSymbol.Concat(results.Select(x => x.Symbol));
        }

        /// <summary>
        /// Adds the specified security to this universe
        /// </summary>
        /// <param name="utcTime">The current utc date time</param>
        /// <param name="security">The security to be added</param>
        /// <param name="isInternal">True if internal member</param>
        /// <returns>True if the security was successfully added,
        /// false if the security was already in the universe</returns>
        internal override bool AddMember(DateTime utcTime, Security security, bool isInternal)
        {
            // never add members to disposed universes
            if (DisposeRequested)
            {
                return false;
            }

            if (Securities.ContainsKey(security.Symbol))
            {
                return false;
            }

            // method take into account the case, when the option has experienced an adjustment
            // we update member reference in this case
            if (Securities.Any(x => x.Value.Security == security))
            {
                Member member;
                Securities.TryRemove(security.Symbol, out member);
            }

            return Securities.TryAdd(security.Symbol, new Member(utcTime, security, isInternal));
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <param name="subscriptionService">Instance which implements <see cref="ISubscriptionDataConfigService"/> interface</param>
        /// <returns>All subscriptions required by this security</returns>
        public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security, DateTime currentTimeUtc, DateTime maximumEndTimeUtc,
                                                                                 ISubscriptionDataConfigService subscriptionService)
        {
            if (Option.Symbol.Underlying == security.Symbol)
            {
                Option.Underlying = security;
                security.SetDataNormalizationMode(DataNormalizationMode.Raw);
            }
            else
            {
                // set the underlying security and pricing model from the canonical security
                var option = (Option)security;
                option.PriceModel = Option.PriceModel;
            }

            return base.GetSubscriptionRequests(security, currentTimeUtc, maximumEndTimeUtc, subscriptionService);
        }
    }
}
