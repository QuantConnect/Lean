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
using Python.Runtime;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// The base class for the RenkoConsolidator class between <see cref="RenkoType"/>s.
    /// </summary>
    public abstract class BaseRenkoConsolidator : IDataConsolidator
    {
        internal bool EvenBars;
        internal Func<IBaseData, decimal> Selector;
        internal Func<IBaseData, decimal> VolumeSelector;
        internal DateTime CloseOn;
        internal decimal CloseRate;
        internal RenkoBar CurrentBar;
        private DataConsolidatedHandler _dataConsolidatedHandler;
        internal bool FirstTick = true;
        internal decimal HighRate;
        internal RenkoBar LastWicko;
        internal decimal LowRate;
        internal DateTime OpenOn;
        internal decimal OpenRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="WickedRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        protected BaseRenkoConsolidator(decimal barSize)    // For use with WickedType
        {
            BarSize = barSize;
            Type = RenkoType.Wicked;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        protected BaseRenkoConsolidator(decimal barSize, bool evenBars = true)
        {
            BarSize = barSize;
            Selector = x => x.Value;
            VolumeSelector = x => 0;
            EvenBars = evenBars;

            Type = RenkoType.Classic;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        protected BaseRenkoConsolidator(
            decimal barSize, Func<IBaseData, decimal> selector, Func<IBaseData, decimal> volumeSelector = null,
            bool evenBars = true
            )
        {
            EpsilonCheck(barSize);

            BarSize = barSize;
            EvenBars = evenBars;
            Selector = selector ?? (x => x.Value);
            VolumeSelector = volumeSelector ?? (x => 0);

            Type = RenkoType.Classic;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public BaseRenkoConsolidator(
            decimal barSize, PyObject selector, PyObject volumeSelector = null, bool evenBars = true
            )
            : this(barSize, evenBars)
        {
            EpsilonCheck(barSize);

            if (selector != null)
            {
                if (!selector.TryConvertToDelegate(out Selector))
                {
                    throw new ArgumentException(
                        "Unable to convert parameter 'selector' to delegate type Func<IBaseData, decimal>");
                }
            }
            else
            {
                Selector = (x => x.Value);
            }

            if (volumeSelector != null)
            {
                if (!volumeSelector.TryConvertToDelegate(out VolumeSelector))
                {
                    throw new ArgumentException(
                        "Unable to convert parameter 'volumeSelector' to delegate type Func<IBaseData, decimal>");
                }
            }
            else
            {
                VolumeSelector = (x => 0);
            }
        }

        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type { get; }

        /// <summary>
        /// Gets the bar size used by this consolidator
        /// </summary>
        protected decimal BarSize { get; }

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated { get; private set; }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public IBaseData WorkingData => CurrentBar?.Clone();

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => typeof(IBaseData);

        /// <summary>
        /// Gets <see cref="RenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public Type OutputType => typeof(RenkoBar);


        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public abstract void Update(IBaseData data);


        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            DataConsolidated = null;
            _dataConsolidatedHandler = null;
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event EventHandler<RenkoBar> DataConsolidated;

        private static void EpsilonCheck(decimal barSize)
        {
            if (barSize < Extensions.GetDecimalEpsilon())
            {
                throw new ArgumentOutOfRangeException(nameof(barSize),
                    "RenkoConsolidator bar size must be positve and greater than 1e-28");
            }
        }

        
        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        event DataConsolidatedHandler IDataConsolidator.DataConsolidated
        {
            add { _dataConsolidatedHandler += value; }
            remove { _dataConsolidatedHandler -= value; }
        }
        // Used for unit tests
        internal RenkoBar OpenRenkoBar =>
            new RenkoBar(null, OpenOn, CloseOn, BarSize, OpenRate, HighRate, LowRate, CloseRate);

        
        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected void OnDataConsolidated(RenkoBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);

            _dataConsolidatedHandler?.Invoke(this, consolidated);

            Consolidated = consolidated;
        }

    }
}
