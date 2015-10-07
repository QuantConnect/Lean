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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents a grouping of data emitted at a certain time.
    /// </summary>
    public class TimeSlice
    {
        /// <summary>
        /// Gets the count of data points in this <see cref="TimeSlice"/>
        /// </summary>
        public int DataPointCount { get; private set; }

        /// <summary>
        /// Gets the time this data was emitted
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Gets the data in the time slice
        /// </summary>
        public List<KeyValuePair<Security, List<BaseData>>> Data { get; private set; }

        /// <summary>
        /// Gets the <see cref="Slice"/> that will be used as input for the algorithm
        /// </summary>
        public Slice Slice { get; private set; }

        /// <summary>
        /// Gets the data used to update the cash book
        /// </summary>
        public List<KeyValuePair<Cash, BaseData>> CashBookUpdateData { get; private set; }

        /// <summary>
        /// Gets the data used to update securities
        /// </summary>
        public List<KeyValuePair<Security, BaseData>> SecuritiesUpdateData { get; private set; }

        /// <summary>
        /// Gets the data used to update the consolidators
        /// </summary>
        public List<KeyValuePair<SubscriptionDataConfig, List<BaseData>>> ConsolidatorUpdateData { get; private set; }

        /// <summary>
        /// Gets all the custom data in this <see cref="TimeSlice"/>
        /// </summary>
        public List<KeyValuePair<Security, List<BaseData>>> CustomData { get; private set; }

        /// <summary>
        /// Gets the changes to the data subscriptions as a result of universe selection
        /// </summary>
        public SecurityChanges SecurityChanges { get; set; }

        /// <summary>
        /// Initializes a new <see cref="TimeSlice"/> containing the specified data
        /// </summary>
        public TimeSlice(DateTime time, int dataPointCount, Slice slice, List<KeyValuePair<Security, List<BaseData>>> data, List<KeyValuePair<Cash, BaseData>> cashBookUpdateData, List<KeyValuePair<Security, BaseData>> securitiesUpdateData, List<KeyValuePair<SubscriptionDataConfig, List<BaseData>>> consolidatorUpdateData, List<KeyValuePair<Security, List<BaseData>>> customData, SecurityChanges securityChanges)
        {
            Time = time;
            Data = data;
            Slice = slice;
            CustomData = customData;
            DataPointCount = dataPointCount;
            CashBookUpdateData = cashBookUpdateData;
            SecuritiesUpdateData = securitiesUpdateData;
            ConsolidatorUpdateData = consolidatorUpdateData;
            SecurityChanges = securityChanges;
        }

        /// <summary>
        /// Creates a new <see cref="TimeSlice"/> for the specified time using the specified data
        /// </summary>
        /// <param name="utcDateTime">The UTC frontier date time</param>
        /// <param name="algorithmTimeZone">The algorithm's time zone, required for computing algorithm and slice time</param>
        /// <param name="cashBook">The algorithm's cash book, required for generating cash update pairs</param>
        /// <param name="data">The data in this <see cref="TimeSlice"/></param>
        /// <param name="changes">The new changes that are seen in this time slice as a result of universe selection</param>
        /// <returns>A new <see cref="TimeSlice"/> containing the specified data</returns>
        public static TimeSlice Create(DateTime utcDateTime, DateTimeZone algorithmTimeZone, CashBook cashBook, List<KeyValuePair<Security, List<BaseData>>> data, SecurityChanges changes)
        {
            int count = 0;
            var security = new List<KeyValuePair<Security, BaseData>>();
            var custom = new List<KeyValuePair<Security, List<BaseData>>>();
            var consolidator = new List<KeyValuePair<SubscriptionDataConfig, List<BaseData>>>();
            var allDataForAlgorithm = new List<BaseData>(data.Count);
            var cash = new List<KeyValuePair<Cash, BaseData>>(cashBook.Count);

            var cashSecurities = new HashSet<Symbol>();
            foreach (var cashItem in cashBook.Values)
            {
                cashSecurities.Add(cashItem.SecuritySymbol);
            }

            Split split;
            Dividend dividend;
            Delisting delisting;
            SymbolChangedEvent symbolChange;

            var algorithmTime = utcDateTime.ConvertFromUtc(algorithmTimeZone);
            var tradeBars = new TradeBars(algorithmTime);
            var ticks = new Ticks(algorithmTime);
            var splits = new Splits(algorithmTime);
            var dividends = new Dividends(algorithmTime);
            var delistings = new Delistings(algorithmTime);
            var symbolChanges = new SymbolChangedEvents(algorithmTime);

            foreach (var kvp in data)
            {
                var list = kvp.Value;
                var symbol = kvp.Key.Symbol;
                
                // keep count of all data points
                count += list.Count;

                BaseData update = null;
                var consolidatorUpdate = new List<BaseData>(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var baseData = list[i];
                    if (!kvp.Key.SubscriptionDataConfig.IsInternalFeed)
                    {
                        // this is all the data that goes into the algorithm
                        allDataForAlgorithm.Add(baseData);
                    }
                    if (kvp.Key.SubscriptionDataConfig.IsCustomData)
                    {
                        // this is all the custom data
                        custom.Add(kvp);
                    }
                    // don't add internal feed data to ticks/bars objects
                    if (baseData.DataType != MarketDataType.Auxiliary)
                    {
                        if (!kvp.Key.SubscriptionDataConfig.IsInternalFeed)
                        {
                            // populate ticks and tradebars dictionaries with no aux data
                            if (baseData.DataType == MarketDataType.Tick)
                            {
                                List<Tick> ticksList;
                                if (!ticks.TryGetValue(symbol, out ticksList))
                                {
                                    ticksList = new List<Tick> {(Tick) baseData};
                                    ticks[symbol] = ticksList;
                                }
                                ticksList.Add((Tick) baseData);
                            }
                            else if (baseData.DataType == MarketDataType.TradeBar)
                            {
                                tradeBars[symbol] = (TradeBar) baseData;
                            }

                            // this is data used to update consolidators
                            consolidatorUpdate.Add(baseData);
                        }

                        // this is the data used set market prices
                        update = baseData;
                    }
                    // include checks for various aux types so we don't have to construct the dictionaries in Slice
                    else if ((delisting = baseData as Delisting) != null)
                    {
                        delistings[symbol] = delisting;
                    }
                    else if ((dividend = baseData as Dividend) != null)
                    {
                        dividends[symbol] = dividend;
                    }
                    else if ((split = baseData as Split) != null)
                    {
                        splits[symbol] = split;
                    }
                    else if ((symbolChange = baseData as SymbolChangedEvent) != null)
                    {
                        // symbol changes is keyed by the requested symbol
                        symbolChanges[kvp.Key.SubscriptionDataConfig.Symbol] = symbolChange;
                    }
                }

                // check for 'cash securities' if we found valid update data for this symbol
                // and we need this data to update cash conversion rates, long term we should
                // have Cash hold onto it's security, then he can update himself, or rather, just
                // patch through calls to conversion rate to compue it on the fly using Security.Price
                if (update != null && cashSecurities.Contains(kvp.Key.Symbol))
                {
                    foreach (var cashKvp in cashBook)
                    {
                        if (cashKvp.Value.SecuritySymbol == kvp.Key.Symbol)
                        {
                            cash.Add(new KeyValuePair<Cash, BaseData>(cashKvp.Value, update));
                        }
                    }
                }

                security.Add(new KeyValuePair<Security, BaseData>(kvp.Key, update));
                consolidator.Add(new KeyValuePair<SubscriptionDataConfig, List<BaseData>>(kvp.Key.SubscriptionDataConfig, consolidatorUpdate));
            }

            var slice = new Slice(utcDateTime.ConvertFromUtc(algorithmTimeZone), allDataForAlgorithm, tradeBars, ticks, splits, dividends, delistings, symbolChanges);

            return new TimeSlice(utcDateTime, count, slice, data, cash, security, consolidator, custom, changes);
        }
    }
}