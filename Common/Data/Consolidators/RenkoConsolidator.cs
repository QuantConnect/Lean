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
    /// This consolidator can transform a stream of <see cref="BaseData"/> instances into a stream of <see cref="RenkoBar"/>
    /// </summary>
    public class RenkoConsolidator : IDataConsolidator
    {
        private RenkoBar _currentBar;
        private DataConsolidatedHandler _dataConsolidatedHandler;
        private decimal _barSize;
        private bool _evenBars;
        private Func<IBaseData, decimal> _selector;
        private Func<IBaseData, decimal> _volumeSelector;

        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type => RenkoType.Classic;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public IBaseData WorkingData => _currentBar?.Clone();

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => typeof(IBaseData);

        /// <summary>
        /// Gets <see cref="RenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public Type OutputType => typeof(RenkoBar);

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated { get; private set; }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event EventHandler<RenkoBar> DataConsolidated;

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        event DataConsolidatedHandler IDataConsolidator.DataConsolidated
        {
            add { _dataConsolidatedHandler += value; }
            remove { _dataConsolidatedHandler -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public RenkoConsolidator(decimal barSize, bool evenBars = true)
        {
            EpsilonCheck(barSize);
            _barSize = barSize;
            _selector = x => x.Value;
            _evenBars = evenBars;
            _volumeSelector = x => 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public RenkoConsolidator(
            decimal barSize,
            Func<IBaseData, decimal> selector,
            Func<IBaseData, decimal> volumeSelector = null,
            bool evenBars = true
            )
        {
            EpsilonCheck(barSize);
            _barSize = barSize;
            _selector = selector ?? (x => x.Value);
            _evenBars = evenBars;
            _volumeSelector = volumeSelector ?? (x => 0);
        }

        /// <summary>
        ///Initializes a new instance of the <see cref="RenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="type">The RenkoType of the bar</param>
        [Obsolete("Please use the WickedRenkoConsolidator if RenkoType is not Classic")]
        public RenkoConsolidator(decimal barSize, RenkoType type)
        : this(barSize, true)
        {
            if (type != RenkoType.Classic)
            {
                throw new ArgumentException("Please use the WickedRenkoConsolidator type if RenkoType is not Classic");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public RenkoConsolidator(decimal barSize,
            PyObject selector,
            PyObject volumeSelector = null,
            bool evenBars = true)
            : this(barSize, evenBars)
        {
            if (selector != null)
            {
                if (!selector.TryConvertToDelegate(out _selector))
                {
                    throw new ArgumentException(
                        "Unable to convert parameter 'selector' to delegate type Func<IBaseData, decimal>");
                }
            }
            else
            {
                _selector = x => x.Value;
            }

            if (volumeSelector != null)
            {
                if (!volumeSelector.TryConvertToDelegate(out _volumeSelector))
                {
                    throw new ArgumentException(
                        "Unable to convert parameter 'volumeSelector' to delegate type Func<IBaseData, decimal>");
                }
            }
            else
            {
                _volumeSelector = x => 0;
            }
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            var currentValue = _selector(data);
            var volume = _volumeSelector(data);

            decimal? close = null;

            // if we're already in a bar then update it
            if (_currentBar != null)
            {
                _currentBar.Update(data.Time, currentValue, volume);

                // if the update caused this bar to close, fire the event and reset the bar
                if (_currentBar.IsClosed)
                {
                    close = _currentBar.Close;
                    OnDataConsolidated(_currentBar);
                    _currentBar = null;
                }
            }

            if (_currentBar == null)
            {
                var open = close ?? currentValue;
                if (_evenBars && !close.HasValue)
                {
                    open = Math.Ceiling(open / _barSize) * _barSize;
                }

                _currentBar = new RenkoBar(data.Symbol, data.Time, _barSize, open, volume);
            }
        }

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

        private static void EpsilonCheck(decimal barSize)
        {
            if (barSize < Extensions.GetDecimalEpsilon())
            {
                throw new ArgumentOutOfRangeException(nameof(barSize),
                    "RenkoConsolidator bar size must be positve and greater than 1e-28");
            }
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        private void OnDataConsolidated(RenkoBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);

            _dataConsolidatedHandler?.Invoke(this, consolidated);

            Consolidated = consolidated;
        }
    }

    /// <summary>
    /// Provides a type safe wrapper on the RenkoConsolidator class. This just allows us to define our selector functions with the real type they'll be receiving
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public class RenkoConsolidator<TInput> : RenkoConsolidator
        where TInput : IBaseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public RenkoConsolidator(
            decimal barSize,
            Func<TInput, decimal> selector,
            Func<TInput, decimal> volumeSelector = null,
            bool evenBars = true
            )
            : base(barSize, x => selector((TInput) x),
                volumeSelector == null ? (Func<IBaseData, decimal>) null : x => volumeSelector((TInput) x), evenBars)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public RenkoConsolidator(decimal barSize, bool evenBars = true)
            : this(barSize, x => x.Value, x => 0, evenBars)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="type">The RenkoType of the bar</param>
        [Obsolete("Please use the WickedRenkoConsolidator if RenkoType is not Classic")]
        public RenkoConsolidator(decimal barSize, RenkoType type)
            : base(barSize, type)
        {
        }

        /// <summary>
        /// Updates this consolidator with the specified data.
        /// </summary>
        /// <remarks>
        /// Type safe shim method.
        /// </remarks>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(TInput data)
        {
            base.Update(data);
        }
    }
}
