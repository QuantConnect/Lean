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

using NodaTime;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines a user that is fired based on a specified <see cref="IDateRule"/> and <see cref="ITimeRule"/>
    /// </summary>
    public class ScheduledUniverse : Universe, ITimeTriggeredUniverse
    {
        private readonly IDateRule _dateRule;
        private readonly ITimeRule _timeRule;
        private readonly Func<DateTime, IEnumerable<Symbol>> _selector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverse"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the date/time rules are in</param>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        public ScheduledUniverse(DateTimeZone timeZone, IDateRule dateRule, ITimeRule timeRule, Func<DateTime, IEnumerable<Symbol>> selector, UniverseSettings settings = null)
            : base(CreateConfiguration(timeZone, dateRule, timeRule))
        {
            _dateRule = dateRule;
            _timeRule = timeRule;
            _selector = selector;
            UniverseSettings = settings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverse"/> class
        /// </summary>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        public ScheduledUniverse(IDateRule dateRule, ITimeRule timeRule, Func<DateTime, IEnumerable<Symbol>> selector, UniverseSettings settings = null)
            : this(TimeZones.Utc, dateRule, timeRule, selector, settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverse"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the date/time rules are in</param>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        public ScheduledUniverse(DateTimeZone timeZone, IDateRule dateRule, ITimeRule timeRule, PyObject selector, UniverseSettings settings = null)
            : base(CreateConfiguration(timeZone, dateRule, timeRule))
        {
            Func<DateTime, object> func;
            selector.TryConvertToDelegate(out func);
            _dateRule = dateRule;
            _timeRule = timeRule;
            _selector = func.ConvertSelectionSymbolDelegate();
            UniverseSettings = settings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverse"/> class
        /// </summary>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        public ScheduledUniverse(IDateRule dateRule, ITimeRule timeRule, PyObject selector, UniverseSettings settings = null)
            : this(TimeZones.Utc, dateRule, timeRule, selector, settings)
        {
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return _selector(utcTime);
        }

        /// <summary>
        /// Get an enumerator of UTC DateTimes that defines when this universe will be invoked
        /// </summary>
        /// <param name="startTimeUtc">The start time of the range in UTC</param>
        /// <param name="endTimeUtc">The end time of the range in UTC</param>
        /// <returns>An enumerator of UTC DateTimes that defines when this universe will be invoked</returns>
        public IEnumerable<DateTime> GetTriggerTimes(DateTime startTimeUtc, DateTime endTimeUtc, MarketHoursDatabase marketHoursDatabase)
        {
            var startTimeLocal = startTimeUtc.ConvertFromUtc(Configuration.ExchangeTimeZone);
            var endTimeLocal = endTimeUtc.ConvertFromUtc(Configuration.ExchangeTimeZone);

            // define date/time rule enumerable
            var dates = _dateRule.GetDates(startTimeLocal, endTimeLocal);
            var times = _timeRule.CreateUtcEventTimes(dates).GetEnumerator();

            // Make sure and filter out any times before our start time
            // GH #5440
            do
            {
                if (!times.MoveNext())
                {
                    times.Dispose();
                    yield break;
                }
            }
            while (times.Current < startTimeUtc);

            // Start yielding times
            do
            {
                yield return times.Current;
            }
            while (times.MoveNext());
            times.Dispose();
        }

        private static SubscriptionDataConfig CreateConfiguration(DateTimeZone timeZone, IDateRule dateRule, ITimeRule timeRule)
        {
            // remove forbidden characters
            var ticker = $"{dateRule.Name}_{timeRule.Name}";
            foreach (var c in SecurityIdentifier.InvalidSymbolCharacters)
            {
                ticker = ticker.Replace(c.ToStringInvariant(), "_");
            }

            var symbol = Symbol.Create(ticker, SecurityType.Base, QuantConnect.Market.USA);
            var config = new SubscriptionDataConfig(typeof(Tick),
                symbol: symbol,
                resolution: Resolution.Daily,
                dataTimeZone: timeZone,
                exchangeTimeZone: timeZone,
                fillForward: false,
                extendedHours: false,
                isInternalFeed: true,
                isCustom: false,
                tickType: null,
                isFilteredSubscription: false
            );

            // force always open hours so we don't inadvertently mess with the scheduled firing times
            MarketHoursDatabase.FromDataFolder()
                .SetEntryAlwaysOpen(config.Market, config.Symbol.Value, config.SecurityType, config.ExchangeTimeZone);

            return config;
        }
    }
}
