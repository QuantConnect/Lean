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
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Live trading realtime event processing.
    /// </summary>
    public class LiveTradingRealTimeHandler : BaseRealTimeHandler, IRealTimeHandler
    {
        private static MarketHoursDatabase _marketHoursDatabase;

        private IIsolatorLimitResultProvider _isolatorLimitProvider;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Boolean flag indicating thread state.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes the real time handler for the specified algorithm and job
        /// </summary>
        public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api, IIsolatorLimitResultProvider isolatorLimitProvider)
        {
            //Initialize:
            Algorithm = algorithm;
            ResultHandler = resultHandler;
            _isolatorLimitProvider = isolatorLimitProvider;
            _cancellationTokenSource = new CancellationTokenSource();
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            var todayInAlgorithmTimeZone = DateTime.UtcNow.ConvertFromUtc(Algorithm.TimeZone).Date;

            // refresh the market hours for today explicitly, and then set up an event to refresh them each day at midnight
            RefreshMarketHoursToday(todayInAlgorithmTimeZone);

            // every day at midnight from tomorrow until the end of time
            var times =
                from date in Time.EachDay(todayInAlgorithmTimeZone.AddDays(1), Time.EndOfTime)
                select date.ConvertToUtc(Algorithm.TimeZone);

            Add(new ScheduledEvent("RefreshMarketHours", times, (name, triggerTime) =>
            {
                // refresh market hours from api every day
                RefreshMarketHoursToday(triggerTime.ConvertFromUtc(Algorithm.TimeZone).Date);
            }));

            base.Setup(todayInAlgorithmTimeZone, Time.EndOfTime, job.Language, DateTime.UtcNow);

            foreach (var scheduledEvent in ScheduledEvents)
            {
                // zoom past old events
                scheduledEvent.Key.SkipEventsUntil(algorithm.UtcTime);
                // set logging accordingly
                scheduledEvent.Key.IsLoggingEnabled = Log.DebuggingEnabled;
            }
        }

        /// <summary>
        /// Execute the live realtime event thread montioring.
        /// It scans every second monitoring for an event trigger.
        /// </summary>
        public void Run()
        {
            IsActive = true;

            // continue thread until cancellation is requested
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var time = DateTime.UtcNow;

                // pause until the next second
                var nextSecond = time.RoundUp(TimeSpan.FromSeconds(1));
                var delay = Convert.ToInt32((nextSecond - time).TotalMilliseconds);
                Thread.Sleep(delay < 0 ? 1 : delay);

                // poke each event to see if it should fire, we order by unique id to be deterministic
                foreach (var kvp in ScheduledEvents.OrderBy(pair => pair.Value))
                {
                    var scheduledEvent = kvp.Key;
                    try
                    {
                        _isolatorLimitProvider.Consume(scheduledEvent, time);
                    }
                    catch (ScheduledEventException scheduledEventException)
                    {
                        var errorMessage = "LiveTradingRealTimeHandler.Run(): There was an error in a scheduled " +
                                           $"event {scheduledEvent.Name}. The error was {scheduledEventException.Message}";

                        Log.Error(scheduledEventException, errorMessage);

                        ResultHandler.RuntimeError(errorMessage);

                        // Errors in scheduled event should be treated as runtime error
                        // Runtime errors should end Lean execution
                        Algorithm.RunTimeError = new Exception(errorMessage);
                    }
                }
            }

            IsActive = false;
            Log.Trace("LiveTradingRealTimeHandler.Run(): Exiting thread... Exit triggered: " + _cancellationTokenSource.IsCancellationRequested);
        }

        /// <summary>
        /// Refresh the Today variable holding the market hours information
        /// </summary>
        private void RefreshMarketHoursToday(DateTime date)
        {
            date = date.Date;

            // update market hours for each security
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;

                var marketHours = MarketToday(date, security.Symbol);
                security.Exchange.SetMarketHours(marketHours, date.DayOfWeek);
                var localMarketHours = security.Exchange.Hours.MarketHours[date.DayOfWeek];
                Log.Trace($"LiveTradingRealTimeHandler.RefreshMarketHoursToday({security.Type}): Market hours set: Symbol: {security.Symbol} {localMarketHours} ({security.Exchange.Hours.TimeZone})");
            }
        }

        /// <summary>
        /// Adds the specified event to the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be scheduled, including the date/times the event fires and the callback</param>
        public override void Add(ScheduledEvent scheduledEvent)
        {
            if (Algorithm != null)
            {
                scheduledEvent.SkipEventsUntil(Algorithm.UtcTime);
            }

            ScheduledEvents.AddOrUpdate(scheduledEvent, GetScheduledEventUniqueId());
        }

        /// <summary>
        /// Removes the specified event from the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be removed</param>
        public override void Remove(ScheduledEvent scheduledEvent)
        {
            int id;
            ScheduledEvents.TryRemove(scheduledEvent, out id);
        }

        /// <summary>
        /// Set the current time. If the date changes re-start the realtime event setup routines.
        /// </summary>
        /// <param name="time"></param>
        public void SetTime(DateTime time)
        {
            // in live mode we use current time for our time keeping
            // this method is used by backtesting to set time based on the data
        }

        /// <summary>
        /// Scan for past events that didn't fire because there was no data at the scheduled time.
        /// </summary>
        /// <param name="time">Current time.</param>
        public void ScanPastEvents(DateTime time)
        {
            // in live mode we use current time for our time keeping
            // this method is used by backtesting to scan for past events based on the data
        }

        /// <summary>
        /// Stop the real time thread
        /// </summary>
        public void Exit()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Get the calendar open hours for the date.
        /// </summary>
        private IEnumerable<MarketHoursSegment> MarketToday(DateTime time, Symbol symbol)
        {
            if (Config.GetBool("force-exchange-always-open"))
            {
                yield return MarketHoursSegment.OpenAllDay();
                yield break;
            }

            var hours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.ID.SecurityType);
            foreach (var segment in hours.MarketHours[time.DayOfWeek].Segments)
            {
                yield return segment;
            }
        }
    }
}