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
        /// Initializes a new <see cref="TimeSlice"/> containing the specified data
        /// </summary>
        public TimeSlice(DateTime time,
            int dataPointCount,
            Slice slice,
            List<KeyValuePair<Security, List<BaseData>>> data,
            List<KeyValuePair<Cash, BaseData>> cashBookUpdateData,
            List<KeyValuePair<Security, BaseData>> securitiesUpdateData,
            List<KeyValuePair<SubscriptionDataConfig, List<BaseData>>> consolidatorUpdateData,
            List<KeyValuePair<Security, List<BaseData>>> customData)
        {
            Time = time;
            Data = data;
            DataPointCount = dataPointCount;
            Slice = slice;
            CashBookUpdateData = cashBookUpdateData;
            SecuritiesUpdateData = securitiesUpdateData;
            ConsolidatorUpdateData = consolidatorUpdateData;
            CustomData = customData;
        }

        /// <summary>
        /// Creates a new <see cref="TimeSlice"/> for the specified time using the specified data
        /// </summary>
        /// <param name="algorithm">The algorithm we're creating <see cref="TimeSlice"/> instances for</param>
        /// <param name="utcDateTime">The UTC frontier date time</param>
        /// <param name="data">The data in this <see cref="TimeSlice"/></param>
        /// <returns>A new <see cref="TimeSlice"/> containing the specified data</returns>
        public static TimeSlice Create(IAlgorithm algorithm, DateTime utcDateTime, List<KeyValuePair<Security, List<BaseData>>> data)
        {
            var cash = new List<KeyValuePair<Cash, BaseData>>();

            // build up the cash update dictionary
            foreach (var kvp in algorithm.Portfolio.CashBook)
            {
                var updates = data.FirstOrDefault(x => x.Key.Symbol == kvp.Value.SecuritySymbol).Value;
                if (updates != null)
                {
                    var lastNonAux = updates.LastOrDefault(x => x.DataType != MarketDataType.Auxiliary);
                    if (lastNonAux != null)
                    {
                        cash.Add(new KeyValuePair<Cash, BaseData>(kvp.Value, lastNonAux));
                    }
                }
            }

            int count = 0;
            var security = new List<KeyValuePair<Security, BaseData>>();
            var custom = new List<KeyValuePair<Security, List<BaseData>>>();
            var consolidator = new List<KeyValuePair<SubscriptionDataConfig, List<BaseData>>>();
            foreach (var kvp in data)
            {
                count += kvp.Value.Count;
                consolidator.Add(new KeyValuePair<SubscriptionDataConfig, List<BaseData>>(
                    kvp.Key.SubscriptionDataConfig,
                    kvp.Value.Where(x => x.DataType != MarketDataType.Auxiliary).ToList())
                    );

                var update = kvp.Value.LastOrDefault(x => x.DataType != MarketDataType.Auxiliary);
                if (update != null)
                {
                    security.Add(new KeyValuePair<Security, BaseData>(kvp.Key, update));
                }
                if (kvp.Key.IsDynamicallyLoadedData)
                {
                    custom.Add(new KeyValuePair<Security, List<BaseData>>(kvp.Key, kvp.Value));
                }
            }

            var slice = new Slice(utcDateTime.ConvertTo(TimeZones.Utc, algorithm.TimeZone), data.Where(x => !x.Key.SubscriptionDataConfig.IsInternalFeed).SelectMany(x => x.Value));

            return new TimeSlice(utcDateTime, count, slice, data, cash, security, consolidator, custom);
        }
    }
}