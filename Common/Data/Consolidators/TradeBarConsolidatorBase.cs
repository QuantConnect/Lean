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
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// A data consolidator that can make bigger bars from any base data by using a TradeBarCreatorBase instance
    /// 
    /// This type acts as the base for other consolidators that produce bars on a given time step or for a count of data.
    /// </summary>
    /// <typeparam name="T">The input type into the consolidator's Update method</typeparam>
    public abstract class TradeBarConsolidatorBase<T> : DataConsolidator<T>
        where T : BaseData
    {

        //The minimum timespan between creating new bars.
        private readonly TimeSpan? _period;
        //The number of data updates between creating new bars.
        private readonly int? _maxCount;
        //The number of pieces of data we've accumulated since our last emit
        private int _currentCount;
        //The working bar used for aggregating the data
        private TradeBar _workingBar;
        //The last time we emitted a consolidated bar
        private DateTime? _lastEmit;

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        protected TradeBarConsolidatorBase(TimeSpan period)
        {
            _period = period;
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        protected TradeBarConsolidatorBase(int maxCount)
        {
            _maxCount = maxCount;
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        protected TradeBarConsolidatorBase(int maxCount, TimeSpan period)
        {
            _maxCount = maxCount;
            _period = period;
        }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType
        {
            get { return typeof(TradeBar); }
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced. We define this as a 'new'
        /// event so we can expose it as a TradeBar instead of a BaseData instance
        /// </summary>
        public new event EventHandler<TradeBar> DataConsolidated;

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(T data)
        {
            //Decide to fire the event
            var fireDataConsolidated = false;

            // decide to aggregate data before or after firing OnDataConsolidated event
            // always aggregate before firing if in counting mode
            bool aggregateBeforeFire = _maxCount.HasValue; 

            if (_maxCount.HasValue)
            {
                // we're in count mode
                _currentCount++;
                if (_currentCount >= _maxCount.Value)
                {
                    _currentCount = 0;
                    fireDataConsolidated = true;
                }
            }

            if (_period.HasValue)
            {
                if (!_lastEmit.HasValue)
                {
                    // we're in time span mode and not initialized
                    _lastEmit = data.Time;
                }

                // we're in time span mode and initialized
                if (data.Time - _lastEmit.Value >= _period.Value)
                {
                    fireDataConsolidated = true;
                }

                // special case: always fire before event trigger when TimeSpan is zero
                if (_period.Value == TimeSpan.Zero)
                {
                    aggregateBeforeFire = true;
                }
            }

            if (aggregateBeforeFire)
            {
                AggregateBar(ref _workingBar, data);
            }

            //Fire the event
            if (fireDataConsolidated)
            {
                if (_period.HasValue)
                {
                    _lastEmit = data.Time;
                }
                OnDataConsolidated(_workingBar);
                _workingBar = null;
            }

            if (!aggregateBeforeFire)
            {
                AggregateBar(ref _workingBar, data);
            }
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected virtual void OnDataConsolidated(TradeBar consolidated)
        {
            var handler = DataConsolidated;
            if (handler != null) handler(this, consolidated);

            base.OnDataConsolidated(consolidated);
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected abstract void AggregateBar(ref TradeBar workingBar, T data);
    }
}