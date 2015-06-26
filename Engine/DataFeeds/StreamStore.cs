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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// StreamStore manages the creation of data objects for live streams; including managing 
    /// a fillforward data stream request.
    /// </summary>
    /// <remarks>
    /// Streams data is pushed into update where it is appended to the data object to be consolidated. Once required time has lapsed for the bar the 
    /// data is piped into a queue.
    /// </remarks>
    public class StreamStore
    {
        //Internal lock object
        private readonly object _lock = new object();

        private BaseData _data;
        private BaseData _previousData;
        private readonly Type _type;
        private readonly SubscriptionDataConfig _config;
        private readonly Security _security;
        private readonly TimeSpan _increment;
        private readonly ConcurrentQueue<BaseData> _queue;

        /// <summary>
        /// Public access to the data object we're dynamically generating.
        /// </summary>
        public BaseData Data
        {
            get
            {
                return _data;
            }
        }

        /// <summary>
        /// Timespan increment for this resolution data:
        /// </summary>
        public TimeSpan Increment
        {
            get
            {
                return _increment;
            }
        }

        /// <summary>
        /// Queue for temporary storage for generated data.
        /// </summary>
        public ConcurrentQueue<BaseData> Queue
        {
            get
            {
                return _queue;
            }
        }

        /// <summary>
        /// Symbol for the given stream.
        /// </summary>
        public string Symbol
        {
            get
            {
                return _config.Symbol;
            }
        }

        /// <summary>
        /// Create a new self updating, thread safe data updater.
        /// </summary>
        /// <param name="config">Configuration for subscription</param>
        /// <param name="security">Security for the subscription</param>
        public StreamStore(SubscriptionDataConfig config, Security security)
        {
            _type = config.Type;
            _data = null;
            _config = config;
            _security = security;
            _increment = config.Increment;
            _queue = new ConcurrentQueue<BaseData>();
            if (config.Resolution == Resolution.Tick)
            {
                throw new ArgumentException("StreamStores are only for non-tick subscriptions");
            }
        }

        /// <summary>
        /// For custom data streams just manually set the data, it doesn't need to be compiled over time into a bar.
        /// </summary>
        /// <param name="data">New data</param>
        public void Update(BaseData data)
        {
            // if we're not within the configured market hours don't process the data
            if (!_security.Exchange.IsOpenDuringBar(data.Time, data.EndTime, _config.ExtendedMarketHours))
            {
                return;
            }

            try
            {
                //If the second has ticked over, and we have data not processed yet, wait for it to be stored:
                // we're waiting for the trigger archive to enqueue and set _data to null
                while (_data != null && _data.Time < ComputeBarStartTime())
                { Thread.Sleep(1); }
            }
            catch (NullReferenceException)
            {
                // we were waiting for _data to go null, it just so happened to go null
                // between the null check and the comparison
            }

            lock (_lock)
            {
                _data = data;
            }
        }

        /// <summary>
        /// Trade causing an update to the current tradebar object.
        /// </summary>
        /// <param name="tick"></param>
        /// <remarks>We build bars from the trade data, or if its a tick stream just pass the trade through as a tick.</remarks>
        public void Update(Tick tick)
        {
            // if we're not within the configured market hours don't process the data
            if (!_security.Exchange.IsOpenDuringBar(tick.Time, tick.EndTime, _config.ExtendedMarketHours))
            {
                return;
            }

            var barStartTime = ComputeBarStartTime();
            try
            {
                //If the second has ticked over, and we have data not processed yet, wait for it to be stored:
                // we're waiting for the trigger archive to enqueue and set _data to null
                while (_data != null && _data.Time < barStartTime)
                { Thread.Sleep(1); }
            }
            catch (NullReferenceException)
            {
                // we were waiting for _data to go null, it just so happened to go null
                // between the null check and the comparison
            }
            
            lock (_lock)
            {
                if (_data == null)
                {
                    _data = new TradeBar(barStartTime, _config.Symbol, tick.LastPrice, tick.LastPrice, tick.LastPrice, tick.LastPrice, tick.Quantity, _config.Resolution.ToTimeSpan());
                }
                else
                {
                    //Update the bar:
                    _data.Update(tick.LastPrice, tick.Quantity, tick.BidPrice, tick.AskPrice);
                }
            }
        }

        /// <summary>
        /// A time period has lapsed, trigger a save/queue of the current value of data.
        /// </summary>
        /// <param name="triggerTime">The time we're triggering this archive for</param>
        /// <param name="fillForward">Data stream is a fillforward type</param>
        public void TriggerArchive(DateTime triggerTime, bool fillForward)
        {
            lock (_lock)
            {
                try
                {
                    //When there's nothing to do:
                    if (_data == null && !fillForward)
                    {
                        Log.Debug("StreamStore.TriggerArchive(): No data to store, and not fill forward: " + Symbol);
                    } 
                    
                    if (_data != null)
                    {
                        //Create clone and reset original
                        Log.Debug("StreamStore.TriggerArchive(): Enqueued new data: S:" + _data.Symbol + " V:" + _data.Value);
                        _previousData = _data.Clone();
                        _queue.Enqueue(_data.Clone());
                        _data = null;
                    }
                    else if (fillForward && _data == null && _previousData != null)
                    {
                        // the time is actually the end time of a bar, check to see if the start time
                        // is within market hours, which is really just checking the _previousData's EndTime
                        if (!_security.Exchange.IsOpenDuringBar(triggerTime - _increment, triggerTime, _config.ExtendedMarketHours))
                        {
                            Log.Debug("StreamStore.TriggerArchive(): Exchange is closed: " + Symbol);
                            return;
                        }

                        //There was no other data in this timer period, and this is a fillforward subscription:
                        Log.Debug("StreamStore.TriggerArchive(): Fillforward, Previous Enqueued: S:" + _previousData.Symbol + " V:" + _previousData.Value);
                        var cloneForward = _previousData.Clone(true);
                        cloneForward.Time = _previousData.Time.Add(_increment);
                        _queue.Enqueue(cloneForward);

                        _previousData = cloneForward.Clone();
                    }
                }
                catch (Exception err)
                {
                    Log.Error("StreamStore.TriggerAchive(fillforward): Failed to archive: " + err.Message);
                }
            }
        }

        /// <summary>
        /// Computes the start time of the bar this data belongs in
        /// </summary>
        private DateTime ComputeBarStartTime()
        {
            // for live data feeds compute a bar start time base on wall clock time, this prevents splitting of data into the algorithm
            return DateTime.Now.RoundDown(_increment);
        }
    }
}
