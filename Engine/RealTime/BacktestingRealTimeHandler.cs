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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Psuedo realtime event processing for backtesting to simulate realtime events in fast forward.
    /// </summary>
    public class BacktestingRealTimeHandler : IRealTimeHandler
    {
        //Threading
        private DateTime _time;
        private bool _exitTriggered;
        private bool _isActive = true;
        private AlgorithmNodePacket _job;

        //Events:
        private List<RealTimeEvent> _events;

        //Algorithm and Handlers:
        private IAlgorithm _algorithm;
        private Dictionary<SecurityType, MarketToday> _today;
        private IResultHandler _resultHandler;

        /// <summary>
        /// Realtime Moment.
        /// </summary>
        public DateTime Time
        {
            get
            {
                return _time;
            }
        }

        /// <summary>
        /// Events array we scan to trigger realtime events.
        /// </summary>
        public List<RealTimeEvent> Events
        {
            get 
            {
                return _events;
            }
        }

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
        }

        /// <summary>
        /// Market hours for today for each security type in the algorithm
        /// </summary>
        public Dictionary<SecurityType, MarketToday> MarketToday
        {
            get
            {
                throw new NotImplementedException("MarketToday is not currently needed in backtesting mode");
                return _today;
            }
        }

        /// <summary>
        /// Intializes the real time handler for the specified algorithm and job
        /// </summary>
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api) 
        {
            //Initialize:
            _algorithm = algorithm;
            _events = new List<RealTimeEvent>();
            _job = job;
            _today = new Dictionary<SecurityType, MarketToday>();
            _resultHandler =  resultHandler;
        }

        /// <summary>
        /// Setup the events for this date.
        /// </summary>
        /// <param name="date">Date for event</param>
        public void SetupEvents(DateTime date)
        {
            //Clear any existing events:
            ClearEvents();

            //Set up the events:
            //1. Default End of Day Times:
            foreach (var security in _algorithm.Securities.Values)
            {
                //Register Events:
                Log.Debug("BacktestingRealTimeHandler.SetupEvents(): Adding End of Day: " + security.Exchange.MarketClose.Add(TimeSpan.FromMinutes(-10)));

                //1. Setup End of Day Events:
                var closingToday = date.Date + security.Exchange.MarketClose.Add(TimeSpan.FromMinutes(-10));
                var symbol = security.Symbol;
                AddEvent(new RealTimeEvent( closingToday, () =>
                {
                    try
                    {
                        _algorithm.OnEndOfDay(symbol);
                    }
                    catch (Exception err)
                    {
                        _resultHandler.RuntimeError("Runtime error in OnEndOfDay event: " + err.Message, err.StackTrace);
                        Log.Error("BacktestingRealTimeHandler.SetupEvents(): EOD: " + err.Message);
                    }
                }));
            }

            // fire just before the day rolls over, 11:58pm
            AddEvent(new RealTimeEvent(date.AddHours(23.967), () =>
            {
                try
                {
                    _algorithm.OnEndOfDay();
                    Log.Debug(string.Format("BacktestingRealTimeHandler: Fired On End of Day Event() for Day({0})", _time.ToShortDateString()));
                }
                catch (Exception err)
                {
                    _resultHandler.RuntimeError("Runtime error in OnEndOfDay event: " + err.Message, err.StackTrace);
                    Log.Error("BacktestingRealTimeHandler.SetupEvents.Trigger OnEndOfDay(): " + err.Message);
                }
            }));
        }
        
        /// <summary>
        /// Normally this would run the realtime event monitoring. Backtesting is in fastforward so the realtime is linked to the backtest clock.
        /// This thread does nothing. Wait until the job is over.
        /// </summary>
        public void Run()
        {
            _isActive = false;
        }


        /// <summary>
        /// Add a new event to our list of events to scan.
        /// </summary>
        /// <param name="newEvent">Event object to montitor daily.</param>
        public void AddEvent(RealTimeEvent newEvent)
        {
            _events.Add(newEvent);
        }

        /// <summary>
        /// Scan the event list with the current market time and see if we need to trigger the callback.
        /// </summary>
        public void ScanEvents()
        {
            for (var i = 0; i < _events.Count; i++)
            {
                _events[i].Scan(_time);
            }
        }

        /// <summary>
        /// Clear any outstanding events.
        /// </summary>
        public void ClearEvents()
        {
            _events.Clear();
        }

        /// <summary>
        /// Reset the events for a new day.
        /// </summary>
        public void ResetEvents()
        {
            for (var i = 0; i < _events.Count; i++)
            {
                _events[i].Reset();
            }
        }

        /// <summary>
        /// Set the time for the realtime event handler.
        /// </summary>
        /// <param name="time">Current time.</param>
        public void SetTime(DateTime time)
        {
            var isDayChange = _time.Date != time.Date;
            //Set the time:
            _time = time;

            // Backtest Mode Only: 
            // > Scan the event every time we set the time. This allows "fast-forwarding" of the realtime events into sync with backtest.
            ScanEvents();

            //Check for day reset:
            if (isDayChange)
            {
                //Reset all the daily events with today's date:
                SetupEvents(time.Date);
            }
        }

        /// <summary>
        /// Stop the real time thread
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
        }

    } // End Result Handler Thread:

} // End Namespace
