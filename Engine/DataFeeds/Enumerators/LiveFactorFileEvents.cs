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
using QuantConnect.Data;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Finds the factor file events a live event provider missed because their row wasn't available on its own date
    /// </summary>
    internal static class LiveFactorFileEvents
    {
        /// <summary>
        /// Gets the dates, before the given tradable date, of the factor file rows with an event the provider hasn't
        /// seen yet and that takes effect on the next trading day at the earliest
        /// </summary>
        /// <param name="config">The associated configuration</param>
        /// <param name="tradableDate">The new tradable date</param>
        /// <param name="previousFactorFile">The factor file of the previous tradable date, null on the first one</param>
        /// <param name="factorFile">The refreshed factor file</param>
        /// <param name="getEventReferencePrice">Gets the reference price of a factor file's event on the trading day after a date,
        /// null when it has none</param>
        /// <returns>The dates of the rows of the missed events</returns>
        public static IEnumerable<DateTime> GetMissedEventDates(SubscriptionDataConfig config, DateTime tradableDate,
            CorporateFactorProvider previousFactorFile, CorporateFactorProvider factorFile,
            Func<CorporateFactorProvider, DateTime, decimal?> getEventReferencePrice)
        {
            if (factorFile == null)
            {
                yield break;
            }

            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(config);
            // the first tradable date comes when the algorithm starts, which can be after the open of a trading date
            if (previousFactorFile == null && exchangeHours.IsDateOpen(tradableDate))
            {
                yield break;
            }

            // the events of rows before the previous trading day took effect before the tradable date
            for (var date = exchangeHours.GetPreviousTradingDay(tradableDate); date < tradableDate; date = date.AddDays(1))
            {
                // without a reference price the event can't be emitted, so it's skipped as before instead of failing
                if (getEventReferencePrice(factorFile, date) > 0
                    && (previousFactorFile == null || getEventReferencePrice(previousFactorFile, date) == null))
                {
                    yield return date;
                }
            }
        }
    }
}
