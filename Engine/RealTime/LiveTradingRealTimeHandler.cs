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
        private readonly bool _forceExchangeAlwaysOpen = Config.GetBool("force-exchange-always-open");

        /// <summary>
        /// Gets the current market hours database instance
        /// </summary>
        protected MarketHoursDatabase MarketHoursDatabase { get; set; } = MarketHoursDatabase.FromDataFolder();

        /// <summary>
        /// Gets the current symbol properties database instance
        /// </summary>
        protected SymbolPropertiesDatabase SymbolPropertiesDatabase { get; set; } = SymbolPropertiesDatabase.FromDataFolder();

        /// <summary>
        /// Gets the time provider
        /// </summary>
        /// <remarks>
        /// This should be fixed to RealTimeHandler, but made a protected property for testing purposes
        /// </remarks>
        protected virtual ITimeProvider TimeProvider { get; } = RealTimeProvider.Instance;

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

            var utcNow = TimeProvider.GetUtcNow();
            var todayInAlgorithmTimeZone = utcNow.ConvertFromUtc(Algorithm.TimeZone).Date;

            // refresh the market hours and symbol properties for today explicitly
            RefreshMarketHours(todayInAlgorithmTimeZone);
            RefreshSymbolProperties();

            // set up an scheduled event to refresh market hours and symbol properties every certain period of time
            var times = Time.DateTimeRange(utcNow.Date, Time.EndOfTime, Algorithm.Settings.DatabasesRefreshPeriod).Where(date => date > utcNow);

            Add(new ScheduledEvent("RefreshMarketHoursAndSymbolProperties", times, (name, triggerTime) =>
            {
                RefreshMarketHours(triggerTime.ConvertFromUtc(Algorithm.TimeZone).Date);
                RefreshSymbolProperties();
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
                var time = TimeProvider.GetUtcNow();

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
        protected virtual void RefreshMarketHours(DateTime date)
        {
            date = date.Date;
            ResetMarketHoursDatabase();

            // update market hours for each security
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;
                UpdateMarketHours(security);

                var localMarketHours = security.Exchange.Hours.GetMarketHours(date);
                Log.Trace($"LiveTradingRealTimeHandler.RefreshMarketHoursToday({security.Type}): Market hours set: Symbol: {security.Symbol} {localMarketHours} ({security.Exchange.Hours.TimeZone})");
            }
        }

        /// <summary>
        /// Refresh the symbol properties for each security
        /// </summary>
        /// <remarks>
        /// - Each time this method is called, the SymbolPropertiesDatabase is reset
        /// - Made protected virtual for testing purposes
        /// </remarks>
        protected virtual void RefreshSymbolProperties()
        {
            ResetSymbolPropertiesDatabase();

            // update market hours for each security
            foreach (var kvp in Algorithm.Securities)
            {
                var security = kvp.Value;
                UpdateSymbolProperties(security);

                Log.Trace($"LiveTradingRealTimeHandler.RefreshSymbolPropertiesToday(): Symbol properties set: " +
                    $"Symbol: {security.Symbol} {security.SymbolProperties}");
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
        /// Updates the market hours for the specified security.
        /// </summary>
        /// <remarks>
        /// - This is done after a MHDB refresh
        /// - Made protected virtual for testing purposes
        /// </remarks>
        protected virtual void UpdateMarketHours(Security security)
        {
            var hours = _forceExchangeAlwaysOpen
                ? SecurityExchangeHours.AlwaysOpen(security.Exchange.TimeZone)
                : MarketHoursDatabase.GetExchangeHours(security.Symbol.ID.Market, security.Symbol, security.Symbol.ID.SecurityType);

            // Use Update method to avoid replacing the reference
            security.Exchange.Hours.Update(hours);
        }

        /// <summary>
        /// Updates the symbol properties for the specified security.
        /// </summary>
        /// <remarks>
        /// - This is done after a SPDB refresh
        /// - Made protected virtual for testing purposes
        /// </remarks>
        protected virtual void UpdateSymbolProperties(Security security)
        {
            var symbolProperties = SymbolPropertiesDatabase.GetSymbolProperties(security.Symbol.ID.Market, security.Symbol,
                security.Symbol.ID.SecurityType, security.QuoteCurrency.Symbol);
            security.SymbolProperties = symbolProperties;
        }

        /// <summary>
        /// Resets the market hours database, forcing a reload when reused.
        /// Called in tests where multiple algorithms are run sequentially,
        /// and we need to guarantee that every test starts with the same environment.
        /// </summary>
        protected virtual void ResetMarketHoursDatabase()
        {
            MarketHoursDatabase.ReloadEntries();
        }

        /// <summary>
        /// Resets the symbol properties database, forcing a reload when reused.
        /// </summary>
        private void ResetSymbolPropertiesDatabase()
        {
            SymbolPropertiesDatabase.ReloadEntries();
        }
    }
}
