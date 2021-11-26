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
using QuantConnect.Securities;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines the parameters required to add a subscription to a data feed.
    /// </summary>
    public class SubscriptionRequest : BaseDataRequest
    {
        /// <summary>
        /// Gets true if the subscription is a universe
        /// </summary>
        public bool IsUniverseSubscription { get; }

        /// <summary>
        /// Gets the universe this subscription resides in
        /// </summary>
        public Universe Universe { get; }

        /// <summary>
        /// Gets the security. This is the destination of data for non-internal subscriptions.
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the subscription configuration. This defines how/where to read the data.
        /// </summary>
        public SubscriptionDataConfig Configuration { get; }

        /// <summary>
        /// Gets the tradable days specified by this request, in the security's data time zone
        /// </summary>
        public IEnumerable<DateTime> TradableDays
        {
            get
            {
                return Time.EachTradeableDayInTimeZone(Security.Exchange.Hours,
                    StartTimeLocal,
                    EndTimeLocal,
                    Configuration.DataTimeZone,
                    Configuration.ExtendedMarketHours
                    );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRequest"/> class
        /// </summary>
        public SubscriptionRequest(bool isUniverseSubscription,
            Universe universe,
            Security security,
            SubscriptionDataConfig configuration,
            DateTime startTimeUtc,
            DateTime endTimeUtc)
            : base(startTimeUtc, endTimeUtc, security.Exchange.Hours, configuration.TickType)
        {
            IsUniverseSubscription = isUniverseSubscription;
            Universe = universe;
            Security = security;
            Configuration = configuration;

            // open interest data comes in once a day before market open,
            // make the subscription start from midnight and use always open exchange
            if (Configuration.TickType == TickType.OpenInterest)
            {
                StartTimeUtc = StartTimeUtc.ConvertFromUtc(ExchangeHours.TimeZone).Date.ConvertToUtc(ExchangeHours.TimeZone);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRequest"/> class
        /// </summary>
        public SubscriptionRequest(SubscriptionRequest template,
            bool? isUniverseSubscription = null,
            Universe universe = null,
            Security security = null,
            SubscriptionDataConfig configuration = null,
            DateTime? startTimeUtc = null,
            DateTime? endTimeUtc = null
            )
            : this(isUniverseSubscription ?? template.IsUniverseSubscription,
                  universe ?? template.Universe,
                  security ?? template.Security,
                  configuration ?? template.Configuration,
                  startTimeUtc ?? template.StartTimeUtc,
                  endTimeUtc ?? template.EndTimeUtc
                  )
        {
        }
    }
}