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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.RealTime
{
    /// <summary>
    /// Live trading realtime event processing.
    /// </summary>
    public class LiveTradingRealTimeHandler : IRealTimeHandler
    {
        /******************************************************** 
        * PRIVATE VARIABLES
        *********************************************************/
        private DateTime _time = new DateTime();
        private bool _exitTriggered = false;
        private bool _isActive = true;
        private List<RealTimeEvent> _events;
        private Dictionary<SecurityType, MarketToday> _today;
        private IDataFeed _feed;
        private IResultHandler _results;
        private TimeSpan _endOfDayDelta = TimeSpan.FromMinutes(10);

        //Algorithm and Handlers:
        private IAlgorithm _algorithm;

        /******************************************************** 
        * PUBLIC PROPERTIES
        *********************************************************/
        /// <summary>
        /// Current time.
        /// </summary>
        public DateTime Time
        {
            get
            {
                return _time;
            }
        }

        /// <summary>
        /// Boolean flag indicating thread state.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
        }

        /// <summary>
        /// List of the events to trigger.
        /// </summary>
        public List<RealTimeEvent> Events
        {
            get
            {
                return _events;
            }
        }

        /******************************************************** 
        * PUBLIC CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Initialize the realtime event handler with all information required for triggering daily events.
        /// </summary>
        public LiveTradingRealTimeHandler(IAlgorithm algorithm, IDataFeed feed, IResultHandler results, IBrokerage brokerage, AlgorithmNodePacket job) 
        {
            //Initialize:
            _algorithm = algorithm;
            _events = new List<RealTimeEvent>();
            _today = new Dictionary<SecurityType, MarketToday>();
            _feed = feed;
            _results = results;
        }

        /******************************************************** 
        * PUBLIC METHODS
        *********************************************************/
        /// <summary>
        /// Execute the live realtime event thread montioring. 
        /// It scans every second monitoring for an event trigger.
        /// </summary>
        public void Run()
        {
            _isActive = true;
            _time = DateTime.Now;

            //Set up the realtime event:
            SetupEvents(DateTime.Now.Date);

            //Continue looping until exit triggered:
            while (!_exitTriggered)
            {
                Thread.Sleep(1000);

                SetTime(DateTime.Now);

                //Refresh event processing:
                ScanEvents();
            }

            _isActive = false;
        }


        /// <summary>
        /// Set up the realtime event handlers for today.
        /// </summary>
        /// <remarks>
        ///     We setup events for:to 
        ///     - Refreshing of the brokerage session tokens.
        ///     - Getting the new daily market open close times.
        ///     - Setting up the "OnEndOfDay" events which close -10M before closing.
        /// </remarks>
        /// <param name="date">Datetime today</param>
        public void SetupEvents(DateTime date)
        {
            try
            {
                //Clear the previous days events to reset with today:
                ClearEvents();

                //MARKET CLOSE UPDATER REAL TIME EVENT:
                // Every day at 3am, update the market status for today:
                AddEvent(new RealTimeEvent(TimeSpan.FromHours(3), () =>
                {
                    Log.Trace("LiveTradingRealTimeHandler: Fired Update Market Status Event: 3.00am");
                    _today[SecurityType.Equity] = Engine.Controls.MarketToday(SecurityType.Equity);
                }));

                //MARKET CLOSE UPDATER REAL TIME EVENT:
                // Every day at 3.30am and 9.30pm, update the access token for tradier: 
                AddEvent(new RealTimeEvent(TimeSpan.FromHours(3.5), () =>
                {
                    Log.Trace("LiveTradingRealTimeHandler: Fired Update Access Token Event: 3.30am");
                    Engine.Brokerage.RefreshSession();
                }));

                // END OF DAY REAL TIME EVENT:
                //Load Today variables based on security type:
                foreach (var security in _algorithm.Securities.Values)
                {
                    var endOfDayEventTime = new TimeSpan();
                    if (security.IsQuantConnectData)
                    {
                        //If QC --> get the close time from API:
                        if (!_today.ContainsKey(security.Type)) _today.Add(security.Type, Engine.Controls.MarketToday(security.Type));

                        if (_today[security.Type].Status == "open")
                        {
                            endOfDayEventTime = _today[security.Type].Open.End.Subtract(_endOfDayDelta);
                        }
                    }
                    else
                    {
                        //If User Data --> Get close time from security.
                        endOfDayEventTime = security.Exchange.MarketClose.Subtract(_endOfDayDelta);
                    }

                    //2. Set this time as the handler for EOD event:
                    if (endOfDayEventTime != new TimeSpan())
                    {
                        Log.Trace("LiveTradingRealTimeHandler.SetupEvents(): Setup EOD Event for " + endOfDayEventTime.ToString());
                        AddEvent(new RealTimeEvent(endOfDayEventTime, () =>
                        {
                            try
                            {
                                _algorithm.OnEndOfDay();
                                _algorithm.OnEndOfDay(security.Symbol);
                                Log.Trace("LiveTradingRealTimeHandler: Fired On End of Day Event(" + security.Symbol + ") for Day( " + _time.ToShortDateString() + ")");
                            }
                            catch (Exception err)
                            {
                                Log.Error("LiveTradingRealTimeHandler.SetupEvents.Trigger OnEndOfDay(): " + err.Message);
                            }
                        }));
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("LiveTradingRealTimeHandler.SetupEvents(): " + err.Message);
            }
        }

        /// <summary>
        /// Container for all time based events.
        /// </summary>
        public void ScanEvents()
        {
            for (var i = 0; i < _events.Count; i++)
            {
                _events[i].Scan(_time);
            }
        }

        /// <summary>
        /// Add this new event to our list.
        /// </summary>
        /// <param name="newEvent">New event we'd like processed.</param>
        public void AddEvent(RealTimeEvent newEvent)
        {
            _events.Add(newEvent);
        }

        /// <summary>
        /// Reset the events -- 
        /// All real time event handlers are self-resetting, and much auto-trigger a reset when the day changes.
        /// </summary>
        public void ResetEvents()
        {
            for (var i = 0; i < _events.Count; i++)
            {
                _events[i].Reset();
            }
        }

        /// <summary>
        /// Clear any outstanding events fom processing list.
        /// </summary>
        public void ClearEvents()
        {
            _events.Clear();
        }

        /// <summary>
        /// Set the current time. If the date changes re-start the realtime event setup routines.
        /// </summary>
        /// <param name="time"></param>
        public void SetTime(DateTime time)
        {
            //Reset all the daily events
            if (_time.Date != time.Date)
            {
                //Each day needs the events reset (have different closing times).
                SetupEvents(time);
            }

            //Set the time:
            _time = time;
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
