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
    public class TradeBarConsolidatorBase<T> : DataConsolidator<T>
        where T : BaseData
    {
        private readonly TradeBarCreatorBase<T> _tradeBarCreator;

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="tradeBarCreator">The trade bar creator responsible for aggregate the T data into trade bars</param>
        public TradeBarConsolidatorBase(TradeBarCreatorBase<T> tradeBarCreator)
        {
            _tradeBarCreator = tradeBarCreator;
            InitializeTradeBarCreatorEventHandler();
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
            _tradeBarCreator.Update(data);
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
        /// Wires up the event handler on _tradeBarCreator to call OnDataConsolidated
        /// </summary>
        private void InitializeTradeBarCreatorEventHandler()
        {
            _tradeBarCreator.TradeBarCreated += (sender, bar) => OnDataConsolidated(bar);
        }
    }
}