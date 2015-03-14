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
using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Type capable of creating trade bars from other trade bars. This type is mainly used in consolidators
    /// to maintain consistency in how we aggregate the trade bars and how we decide when to end a trade bar
    /// </summary>
    public abstract class TradeBarCreatorBase<TInputType>
        where TInputType : BaseData
    {
        /// <summary>
        /// Event that fires when a new trade bar has been created
        /// </summary>
        public event EventHandler<TradeBar> TradeBarCreated; 

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
        /// Initializes a new instance of the TradeBarCreatorBase class to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        protected TradeBarCreatorBase(TimeSpan period)
        {
            _period = period;
        }

        /// <summary>
        /// Initializes a new instance of the TradeBarCreatorBase class to produce a new 'TradeBar' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        protected TradeBarCreatorBase(int maxCount)
        {
            _maxCount = maxCount;
        }

        /// <summary>
        /// Initializes a new instance of the TradeBarCreatorBase class to produce a new 'TradeBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        protected TradeBarCreatorBase(int maxCount, TimeSpan period)
        {
            _maxCount = maxCount;
            _period = period;
        }

        /// <summary>
        /// Updates this trade bar creator with the specified data. This method is
        /// responsible for raising the TradeBarCreated event
        /// </summary>
        /// <param name="data">The new data for the creator</param>
        public void Update(TInputType data)
        {
            AggregateBar(ref _workingBar, data);

            //Decide to fire the event
            var fireDataConsolidated = false;
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

            }

            //Fire the event
            if (fireDataConsolidated)
            {
                if (_period.HasValue)
                {
                    _lastEmit = data.Time;
                }
                OnTradeBarCreated(_workingBar);
                _workingBar = null;
            }
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected abstract void AggregateBar(ref TradeBar workingBar, TInputType data);

        /// <summary>
        /// Event invocator for the TradeBarCreated event. This fires when a new trade bar has been created from input data
        /// </summary>
        /// <param name="newTradeBar">The new trade bar that was just created</param>
        protected virtual void OnTradeBarCreated(TradeBar newTradeBar)
        {
            var handler = TradeBarCreated;
            if (handler != null) handler(this, newTradeBar);
        }
    }
}