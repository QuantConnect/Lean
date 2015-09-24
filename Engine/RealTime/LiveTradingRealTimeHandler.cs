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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Scheduling;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Live trading realtime event processing.
    /// </summary>
    public class LiveTradingRealTimeHandler : IRealTimeHandler
    {
        private bool _isActive = true;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        // initialize this immediately since the Initialzie method gets called after IAlgorithm.Initialize,
        // so we want to be ready to accept events as soon as possible
        private readonly ConcurrentDictionary<string, ScheduledEvent> _scheduledEvents = new ConcurrentDictionary<string, ScheduledEvent>();

        //Algorithm and Handlers:
        private IApi _api;
        private IAlgorithm _algorithm;
        private IResultHandler _resultHandler;

        /// <summary>
        /// Boolean flag indicating thread state.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
        }

        /// <summary>
        /// Intializes the real time handler for the specified algorithm and job
        /// </summary>
        public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api)
        {
            //Initialize:
            _api = api;
            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _cancellationTokenSource = new CancellationTokenSource();

            var todayInAlgorithmTimeZone = DateTime.UtcNow.ConvertFromUtc(_algorithm.TimeZone).Date;

            // refresh the market hours for today explicitly, and then set up an event to refresh them each day at midnight
            RefreshMarketHoursToday(todayInAlgorithmTimeZone);

            // every day at midnight from tomorrow until the end of time
            var times =
                from date in Time.EachDay(todayInAlgorithmTimeZone.AddDays(1), Time.EndOfTime)
                select date.ConvertToUtc(_algorithm.TimeZone);

            Add(new ScheduledEvent("RefreshMarketHours", times, (name, triggerTime) =>
            {
                // refresh market hours from api every day
                RefreshMarketHoursToday(triggerTime.ConvertFromUtc(_algorithm.TimeZone).Date);
            }));

            // add end of day events for each tradeable day
            Add(ScheduledEventFactory.EveryAlgorithmEndOfDay(_algorithm, _resultHandler, todayInAlgorithmTimeZone, Time.EndOfTime, ScheduledEvent.AlgorithmEndOfDayDelta, DateTime.UtcNow));

            // add end of trading day events for each security
            foreach (var security in _algorithm.Securities.Values.Where(x => !x.SubscriptionDataConfig.IsInternalFeed))
            {
                // assumes security.Exchange has been updated with today's hours via RefreshMarketHoursToday
                Add(ScheduledEventFactory.EverySecurityEndOfDay(_algorithm, _resultHandler, security, todayInAlgorithmTimeZone, Time.EndOfTime, ScheduledEvent.SecurityEndOfDayDelta, DateTime.UtcNow));
            }

            foreach (var scheduledEvent in _scheduledEvents)
            {
                // zoom past old events
                scheduledEvent.Value.SkipEventsUntil(algorithm.UtcTime);
                // set logging accordingly
                scheduledEvent.Value.IsLoggingEnabled = Log.DebuggingEnabled;
            }
        }

        /// <summary>
        /// Execute the live realtime event thread montioring. 
        /// It scans every second monitoring for an event trigger.
        /// </summary>
        public void Run()
        {
            _isActive = true;

            // continue thread until cancellation is requested
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var time = DateTime.UtcNow;

                    // pause until the next second
                    var nextSecond = time.RoundUp(TimeSpan.FromSeconds(1));
                    var delay = Convert.ToInt32((nextSecond - time).TotalMilliseconds);
                    Thread.Sleep(delay < 0 ? 1 : delay);

                    // poke each event to see if it should fire
                    foreach (var scheduledEvent in _scheduledEvents)
                    {
                        scheduledEvent.Value.Scan(time);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            }

            _isActive = false;
            Log.Trace("LiveTradingRealTimeHandler.Run(): Exiting thread... Exit triggered: " + _cancellationTokenSource.IsCancellationRequested);
        }

        /// <summary>
        /// Refresh the Today variable holding the market hours information
        /// </summary>
        private void RefreshMarketHoursToday(DateTime date)
        {
            date = date.Date;

            // group securities by security type to make calls to api one security type at a time
            foreach (var securityTypeGrouping in _algorithm.Securities.GroupBy(x => x.Value.Type))
            {
                var securityType = securityTypeGrouping.Key;
                var marketToday = _api.MarketToday(date, securityType);

                Log.Trace(string.Format("LiveTradingRealTimeHandler.SetupEvents(): Daily Market Hours Setup for Security Type: {0} Start: {1} Stop: {2}",
                    securityType, marketToday.Open.Start, marketToday.Open.End
                    ));

                // foreach security in this security type grouping
                foreach (var kvp in securityTypeGrouping)
                {
                    var symbol = kvp.Key;
                    var security = kvp.Value;

                    // if the market is not open today, set it as closed all day
                    if (marketToday.Status != "open")
                    {
                        security.Exchange.SetMarketHours(TimeSpan.Zero, TimeSpan.Zero, date.DayOfWeek);
                    }
                    else
                    {
                        // set the market hours using data returned from the api
                        var extendedMarketOpen = marketToday.PreMarket.Start.TimeOfDay;
                        var marketOpen = marketToday.Open.Start.TimeOfDay;
                        var marketClose = marketToday.Open.End.TimeOfDay;
                        var extendedMarketClose = marketToday.PostMarket.End.TimeOfDay;
                        security.Exchange.SetMarketHours(extendedMarketOpen, marketOpen, marketClose, extendedMarketClose, date.DayOfWeek);
                        Log.Trace(string.Format("LiveTradingRealTimeHandler.SetupEvents({0}): Market hours set: Symbol: {1} Extended Start: {2} Start: {3} End: {4} Extended End: {5}",
                                securityType, symbol, extendedMarketOpen, marketOpen, marketClose, extendedMarketClose));
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified event to the schedule
        /// </summary>
        /// <param name="scheduledEvent">The event to be scheduled, including the date/times the event fires and the callback</param>
        public void Add(ScheduledEvent scheduledEvent)
        {
            if (_algorithm != null)
            {
                scheduledEvent.SkipEventsUntil(_algorithm.UtcTime);
            }

            _scheduledEvents.AddOrUpdate(scheduledEvent.Name, scheduledEvent);
        }

        /// <summary>
        /// Removes the specified event from the schedule
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name)
        {
            ScheduledEvent scheduledEvent;
            _scheduledEvents.TryRemove(name, out scheduledEvent);
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
        /// Stop the real time thread
        /// </summary>
        public void Exit()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}