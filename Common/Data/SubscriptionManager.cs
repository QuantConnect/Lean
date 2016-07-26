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
using NodaTime;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Data
{
    /// <summary>
    /// Enumerable Subscription Management Class
    /// </summary>
    public class SubscriptionManager
    {
        private readonly TimeKeeper _timeKeeper;

        /// Generic Market Data Requested and Object[] Arguements to Get it:
        public List<SubscriptionDataConfig> Subscriptions;

        /// <summary>
        /// Initialise the Generic Data Manager Class
        /// </summary>
        /// <param name="timeKeeper">The algoritm's time keeper</param>
        public SubscriptionManager(TimeKeeper timeKeeper)
        {
            _timeKeeper = timeKeeper;
            //Generic Type Data Holder:
            Subscriptions = new List<SubscriptionDataConfig>();
        }

        /// <summary>
        /// Get the count of assets:
        /// </summary>
        public int Count 
        {
            get 
            { 
                return Subscriptions.Count; 
            }
        }

        /// <summary>
        /// Add Market Data Required (Overloaded method for backwards compatibility).
        /// </summary>
        /// <param name="symbol">Symbol of the asset we're like</param>
        /// <param name="resolution">Resolution of Asset Required</param>
        /// <param name="timeZone">The time zone the subscription's data is time stamped in</param>
        /// <param name="exchangeTimeZone">Specifies the time zone of the exchange for the security this subscription is for. This
        /// is this output time zone, that is, the time zone that will be used on BaseData instances</param>
        /// <param name="isCustomData">True if this is custom user supplied data, false for normal QC data</param>
        /// <param name="fillDataForward">when there is no data pass the last tradebar forward</param>
        /// <param name="extendedMarketHours">Request premarket data as well when true </param>
        /// <returns>The newly created <see cref="SubscriptionDataConfig"/></returns>
        public SubscriptionDataConfig Add(Symbol symbol, Resolution resolution, DateTimeZone timeZone, DateTimeZone exchangeTimeZone, bool isCustomData = false, bool fillDataForward = true, bool extendedMarketHours = false)
        {
            //Set the type: market data only comes in two forms -- ticks(trade by trade) or tradebar(time summaries)
            var dataType = typeof(TradeBar);
            if (resolution == Resolution.Tick) 
            {
                dataType = typeof(Tick);
            }
            return Add(dataType, symbol, resolution, timeZone, exchangeTimeZone, isCustomData, fillDataForward, extendedMarketHours, false);
        }

        /// <summary>
        /// Add Market Data Required - generic data typing support as long as Type implements BaseData.
        /// </summary>
        /// <param name="dataType">Set the type of the data we're subscribing to.</param>
        /// <param name="symbol">Symbol of the asset we're like</param>
        /// <param name="resolution">Resolution of Asset Required</param>
        /// <param name="dataTimeZone">The time zone the subscription's data is time stamped in</param>
        /// <param name="exchangeTimeZone">Specifies the time zone of the exchange for the security this subscription is for. This
        /// is this output time zone, that is, the time zone that will be used on BaseData instances</param>
        /// <param name="isCustomData">True if this is custom user supplied data, false for normal QC data</param>
        /// <param name="fillDataForward">when there is no data pass the last tradebar forward</param>
        /// <param name="extendedMarketHours">Request premarket data as well when true </param>
        /// <param name="isInternalFeed">Set to true to prevent data from this subscription from being sent into the algorithm's OnData events</param>
        /// <param name="isFilteredSubscription">True if this subscription should have filters applied to it (market hours/user filters from security), false otherwise</param>
        /// <returns>The newly created <see cref="SubscriptionDataConfig"/></returns>
        public SubscriptionDataConfig Add(Type dataType, Symbol symbol, Resolution resolution, DateTimeZone dataTimeZone, DateTimeZone exchangeTimeZone, bool isCustomData, bool fillDataForward = true, bool extendedMarketHours = false, bool isInternalFeed = false, bool isFilteredSubscription = true)
        {
            if (dataTimeZone == null)
            {
                throw new ArgumentNullException("dataTimeZone", "DataTimeZone is a required parameter for new subscriptions.  Set to the time zone the raw data is time stamped in.");
            }
            if (exchangeTimeZone == null)
            {
                throw new ArgumentNullException("exchangeTimeZone", "ExchangeTimeZone is a required parameter for new subscriptions.  Set to the time zone the security exchange resides in.");
            }
            
            //Create:
            var newConfig = new SubscriptionDataConfig(dataType, symbol, resolution, dataTimeZone, exchangeTimeZone, fillDataForward, extendedMarketHours, isInternalFeed, isCustomData, isFilteredSubscription: isFilteredSubscription);

            //Add to subscription list: make sure we don't have his symbol:
            Subscriptions.Add(newConfig);

            // add the time zone to our time keeper
            _timeKeeper.AddTimeZone(exchangeTimeZone);

            return newConfig;
        }

        /// <summary>
        /// Add a consolidator for the symbol
        /// </summary>
        /// <param name="symbol">Symbol of the asset to consolidate</param>
        /// <param name="consolidator">The consolidator</param>
        public void AddConsolidator(Symbol symbol, IDataConsolidator consolidator)
        {
            //Find the right subscription and add the consolidator to it
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                if (Subscriptions[i].Symbol == symbol)
                {
                    // we need to be able to pipe data directly from the data feed into the consolidator
                    if (!consolidator.InputType.IsAssignableFrom(Subscriptions[i].Type))
                    {
                        throw new ArgumentException(string.Format("Type mismatch found between consolidator and symbol. " +
                            "Symbol: {0} expects type {1} but tried to register consolidator with input type {2}", 
                            symbol, Subscriptions[i].Type.Name, consolidator.InputType.Name)
                            );
                    }
                    Subscriptions[i].Consolidators.Add(consolidator);
                    return;
                }
            }

            //If we made it here it is because we never found the symbol in the subscription list
            throw new ArgumentException("Please subscribe to this symbol before adding a consolidator for it. Symbol: " + symbol.ToString());
        }

    } // End Algorithm MetaData Manager Class

} // End QC Namespace
