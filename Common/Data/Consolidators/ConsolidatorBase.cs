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
using QuantConnect.Indicators;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Provides a base implementation for consolidators, including a built-in rolling window
    /// that stores the history of consolidated bars.
    /// </summary>
    public abstract class ConsolidatorBase : WindowBase<IBaseData>, IDataConsolidator
    {
        private IBaseData _consolidated;

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated
        {
            get
            {
                return _consolidated;
            }
            protected set
            {
                _consolidated = value;
            }
        }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public abstract IBaseData WorkingData { get; }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public abstract Type InputType { get; }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public abstract Type OutputType { get; }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public abstract void Update(IBaseData data);

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public abstract void Scan(DateTime currentLocalTime);

        /// <summary>
        /// Event handler that fires when a new piece of data is produced. This is the single subscription
        /// point, shared by the <see cref="IDataConsolidator"/> interface and by derived consolidators whose
        /// output is a base data bar, so subscribing and unsubscribing always target the same handler list.
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Event invocator for the DataConsolidated event. Populates the rolling window, raises the
        /// strongly typed and interface events, and finally updates the <see cref="Consolidated"/> property.
        /// </summary>
        protected virtual void OnDataConsolidated(IBaseData consolidated)
        {
            // populate the rolling window before firing any event so that, inside a DataConsolidated
            // handler, consolidator[0] is the bar that was just produced. Skip null bars, an out of order
            // data point can produce a null bar in count mode, so we never push null nor wipe the history
            if (consolidated != null)
            {
                Current = consolidated;
            }

            // let derived consolidators raise their strongly typed DataConsolidated event
            FireDataConsolidated(consolidated);

            DataConsolidated?.Invoke(this, consolidated);

            // assign the Consolidated property after the event handlers are fired,
            // this allows the event handlers to look at the new consolidated data
            // and the previous consolidated data at the same time without extra bookkeeping
            Consolidated = consolidated;
        }

        /// <summary>
        /// Raises the strongly typed DataConsolidated event exposed by derived consolidators that produce a
        /// more specific bar type. Invoked after the rolling window is populated and before the shared event
        /// so every handler sees the same window. Consolidators whose output is a base data bar do not need
        /// to override this, the shared <see cref="DataConsolidated"/> event already carries their bar.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected virtual void FireDataConsolidated(IBaseData consolidated)
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            DataConsolidated = null;
        }

        /// <summary>
        /// Resets this consolidator, clearing consolidated data and the rolling window.
        /// </summary>
        public virtual void Reset()
        {
            Consolidated = null;
            ResetWindow();
        }
    }
}
