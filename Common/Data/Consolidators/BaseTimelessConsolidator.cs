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

using Python.Runtime;
using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Represents a timeless consolidator which depends on the given values
    /// </summary>
    public abstract class BaseTimelessConsolidator : IDataConsolidator
    {
        protected Func<IBaseData, decimal> Selector;
        protected Func<IBaseData, decimal> VolumeSelector;
        protected DataConsolidatedHandler DataConsolidatedHandler;

        /// <summary>
        /// Bar being created
        /// </summary>
        protected virtual TradeBar CurrentBar {  get; set; }

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated { get; protected set; }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public abstract IBaseData WorkingData { get; }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => typeof(IBaseData);

        /// <summary>
        /// Gets <see cref="TradeBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public virtual Type OutputType => typeof(TradeBar);

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event EventHandler<BaseData> DataConsolidated;

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        event DataConsolidatedHandler IDataConsolidator.DataConsolidated
        {
            add { DataConsolidatedHandler += value; }
            remove { DataConsolidatedHandler -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTimelessConsolidator" /> class.
        /// </summary>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="TradeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        protected BaseTimelessConsolidator(Func<IBaseData, decimal> selector, Func<IBaseData, decimal> volumeSelector = null)
        {
            Selector = selector ?? (x => x.Value);
            VolumeSelector = volumeSelector ?? (x => 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTimelessConsolidator" /> class.
        /// </summary>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="TradeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        protected BaseTimelessConsolidator(PyObject selector, PyObject volumeSelector = null)
        {
            using (Py.GIL())
            {
                if (selector != null && !selector.IsNone())
                {
                    if (!selector.TryConvertToDelegate(out Selector))
                    {
                        throw new ArgumentException(
                            "Unable to convert parameter 'selector' to delegate type Func<IBaseData, decimal>");
                    }
                }
                else
                {
                    Selector = x => x.Value;
                }

                if (volumeSelector != null && !volumeSelector.IsNone())
                {
                    if (!volumeSelector.TryConvertToDelegate(out VolumeSelector))
                    {
                        throw new ArgumentException(
                            "Unable to convert parameter 'volumeSelector' to delegate type Func<IBaseData, decimal>");
                    }
                }
                else
                {
                    VolumeSelector = x => 0;
                }
            }
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            var currentValue = Selector(data);
            var volume = VolumeSelector(data);

            // if we're already in a bar then update it
            if (CurrentBar != null)
            {
                UpdateBar(data.Time, currentValue, volume);
                CheckIfBarIsClosed();
            }

            if (CurrentBar == null)
            {
                CreateNewBar(data);
            }
        }

        /// <summary>
        /// Update the current bar being created with the given data
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected abstract void UpdateBar(DateTime time, decimal currentValue, decimal volume);

        /// <summary>
        /// Checks if the current bar being created has closed. If that is the case, it consolidates
        /// the bar, resets the current bar and stores the needed information from the last bar to
        /// create the next bar
        /// </summary>
        protected abstract void CheckIfBarIsClosed();

        /// <summary>
        /// Creates a new bar with the given data
        /// </summary>
        /// <param name="data">The new data for the bar</param>
        protected abstract void CreateNewBar(IBaseData data);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            DataConsolidated = null;
            DataConsolidatedHandler = null;
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
        }
    }
}
