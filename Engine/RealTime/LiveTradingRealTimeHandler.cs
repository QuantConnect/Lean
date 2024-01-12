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
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Live trading realtime event processing.
    /// </summary>
    public class LiveTradingRealTimeHandler : BacktestingRealTimeHandler
    {
        private Thread _realTimeThread;
        private CancellationTokenSource _cancellationTokenSource = new();
        protected MarketHoursDatabase MarketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        /// <summary>
        /// Boolean flag indicating thread state.
        /// </summary>
        public override bool IsActive { get; protected set; }

        /// <summary>
        /// Initializes the real time handler for the specified algorithm and job
        /// </summary>
        public override void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api, IIsolatorLimitResultProvider isolatorLimitProvider)
        {
            base.Setup(algorithm, job, resultHandler, api, isolatorLimitProvider);

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
        }

        /// <summary>
        /// Get's the timeout the scheduled task time monitor should use
        /// </summary>
        protected override int GetTimeMonitorTimeout()
        {
            return 500;
        }

        /// <summary>
        /// Execute the live realtime event thread montioring.
        /// It scans every second monitoring for an event trigger.
        /// </summary>
        private void Run()
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
                foreach (var kvp in ScheduledEvents.OrderBySafe(pair => pair.Value))
                {
                    var scheduledEvent = kvp.Key;
                    try
                    {
                        IsolatorLimitProvider.Consume(scheduledEvent, time, TimeMonitor);
                    }
                    catch (Exception exception)
                    {
                        Algorithm.SetRuntimeError(exception, $"Scheduled event: '{scheduledEvent.Name}' at {time}");
                    }
                }
            }

            IsActive = false;
            Log.Trace("LiveTradingRealTimeHandler.Run(): Exiting thread... Exit triggered: " + _cancellationTokenSource.IsCancellationRequested);
        }

        /// <summary>
        /// Refresh the market hours for each security in the given date
        /// </summary>
        /// <remarks>Each time this method is called, the MarketHoursDatabase is reset</remarks>
        private void RefreshMarketHoursToday(DateTime date)
        {
            date = date.Date;
            MarketHoursDatabase.Reset();

            // update market hours for each security
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;

                var marketHours = GetMarketHours(date, security.Symbol);
                security.Exchange.SetMarketHours(marketHours, date.DayOfWeek);
                var localMarketHours = security.Exchange.Hours.MarketHours[date.DayOfWeek];
                Log.Trace($"LiveTradingRealTimeHandler.RefreshMarketHoursToday({security.Type}): Market hours set: Symbol: {security.Symbol} {localMarketHours} ({security.Exchange.Hours.TimeZone})");
            }
        }

        /// <summary>
        /// Set the current time. If the date changes re-start the realtime event setup routines.
        /// </summary>
        /// <param name="time"></param>
        public override void SetTime(DateTime time)
        {
            if (Algorithm.IsWarmingUp)
            {
                base.SetTime(time);
            }
            else if (_realTimeThread == null)
            {
                // in live mode we use current time for our time keeping
                // this method is used by backtesting to set time based on the data
                _realTimeThread = new Thread(Run) { IsBackground = true, Name = "RealTime Thread" };
                _realTimeThread.Start(); // RealTime scan time for time based events
            }
        }

        /// <summary>
        /// Scan for past events that didn't fire because there was no data at the scheduled time.
        /// </summary>
        /// <param name="time">Current time.</param>
        public override void ScanPastEvents(DateTime time)
        {
            if (Algorithm.IsWarmingUp)
            {
                base.ScanPastEvents(time);
            }
            // in live mode we use current time for our time keeping
            // this method is used by backtesting to scan for past events based on the data
        }

        /// <summary>
        /// Stop the real time thread
        /// </summary>
        public override void Exit()
        {
            _realTimeThread.StopSafely(TimeSpan.FromMinutes(5), _cancellationTokenSource);
            _cancellationTokenSource.DisposeSafely();
            base.Exit();
        }

        /// <summary>
        /// Get the market hours for the given symbol and date
        /// </summary>
        private IEnumerable<MarketHoursSegment> GetMarketHours(DateTime time, Symbol symbol)
        {
            if (Config.GetBool("force-exchange-always-open"))
            {
                yield return MarketHoursSegment.OpenAllDay();
                yield break;
            }

            var entry = MarketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);
            var securityExchangeHours = new SecurityExchangeHours(
                entry.DataTimeZone,
                entry.ExchangeHours.Holidays,
                entry.ExchangeHours.MarketHours.ToDictionary(),
                entry.ExchangeHours.EarlyCloses,
                entry.ExchangeHours.LateOpens);
            var hours = securityExchangeHours.GetMarketHours(time);

            foreach (var segment in hours.Segments)
            {
                yield return segment;
            }
        }
    }
}
