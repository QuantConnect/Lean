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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> that reads
    /// an entire <see cref="SubscriptionDataSource"/> into a single <see cref="BaseDataCollection"/>
    /// to be emitted on the tradable date at midnight
    /// </summary>
    /// <remarks>This enumerator factory is currently only used in backtesting with coarse data</remarks>
    public class BaseDataCollectionSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, IDataProvider dataProvider)
        {
            using (var dataCacheProvider = new SingleEntryDataCacheProvider(dataProvider))
            {
                var configuration = request.Configuration;
                var sourceFactory = (BaseData)Activator.CreateInstance(request.Configuration.Type);

                // we want the first selection to happen on the start time
                // so we need the previous tradable day time, since coarse
                // files are for each tradable date but emitted with next day time
                var previousTradableDay = GetPreviousTradableDay(request.StartTimeLocal, request);

                var tradableDays = new[] { previousTradableDay }.Concat(request.TradableDays);

                // if the selection schedule was defined use it instead
                var schedule = request.Universe.UniverseSettings.Schedule.Get(request.StartTimeLocal);
                if (schedule != null)
                {
                    Log.Trace("BaseDataCollectionSubscriptionEnumeratorFactory.CreateEnumerator(): will use UniverseSettings.Schedule " +
                              "instead of each tradable day.");
                    tradableDays = schedule
                        // we shift time 1 day before since coarse selection is emitted for the following day and will match the request time
                        .Select(time => time.AddDays(-1))
                        .Where(time => time <= request.EndTimeUtc);
                }

                // Behaves in the same way as in live trading
                // (i.e. only emit coarse data on dates following a trading day)
                // The shifting of dates is needed to ensure we never emit coarse data on the same date,
                // because it would enable look-ahead bias.
                foreach (var dateTime in tradableDays)
                {
                    var date = dateTime.Date;

                    if (request.Universe.UniverseSettings.Schedule != null)
                    {
                        // if the schedule returned a non tradable date, lets get the previous tradable date
                        // and use it to fetch the coarse data
                        if (!request.Security.Exchange.Hours.IsDateOpen(date))
                        {
                            date = GetPreviousTradableDay(date, request);
                        }
                    }

                    var source = sourceFactory.GetSource(configuration, date, false);
                    var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, configuration, date, false);
                    var coarseFundamentalForDate = factory.Read(source);

                    //  shift all date of emitting the file forward one day to model emitting coarse midnight the next day.
                    yield return new BaseDataCollection(dateTime.Date.AddDays(1), configuration.Symbol, coarseFundamentalForDate);
                }
            }
        }

        private DateTime GetPreviousTradableDay(DateTime time, SubscriptionRequest request)
        {
            return Time.GetStartTimeForTradeBars(
                request.Security.Exchange.Hours,
                time,
                Time.OneDay,
                1,
                false);
        }
    }
}