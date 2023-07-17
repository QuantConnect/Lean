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
    /// This consolidator can transform a stream of <see cref="IBaseData"/> instances into a stream of <see cref="RenkoBar"/>
    /// </summary>
    public class ClassicRenkoConsolidator : BaseTimelessConsolidator
    {
        private RenkoBar _currentBar;
        private DataConsolidatedHandler _dataConsolidatedHandler;
        private decimal _barSize;
        private bool _evenBars;
        private decimal? _lastCloseValue;

        /// <summary>
        /// Bar being created
        /// </summary>
        protected override TradeBar CurrentBar
        {
            get
            {
                return _currentBar;
            }
            set
            {
                _currentBar = (RenkoBar)value;
            }
        }

        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type => RenkoType.Classic;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => _currentBar?.Clone();

        /// <summary>
        /// Gets <see cref="RenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType => typeof(RenkoBar);

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new event EventHandler<RenkoBar> DataConsolidated;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public ClassicRenkoConsolidator(decimal barSize, bool evenBars = true)
            : base(x => x.Value, x => 0)
        {
            EpsilonCheck(barSize);
            _barSize = barSize;
            _evenBars = evenBars;
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
        public ClassicRenkoConsolidator(
            decimal barSize,
            Func<IBaseData, decimal> selector,
            Func<IBaseData, decimal> volumeSelector = null,
            bool evenBars = true)
            : base(selector ?? (x => x.Value), volumeSelector ?? (x => 0))
        {
            EpsilonCheck(barSize);
            _barSize = barSize;
            _evenBars = evenBars;
        }

        /// <summary>
        ///Initializes a new instance of the <see cref="ClassicRenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="type">The RenkoType of the bar</param>
        [Obsolete("Please use the new RenkoConsolidator if RenkoType is not Classic")]
        public ClassicRenkoConsolidator(decimal barSize, RenkoType type)
        : this(barSize, true)
        {
            if (type != RenkoType.Classic)
            {
                throw new ArgumentException("Please use the new RenkoConsolidator type if RenkoType is not Classic");
            }
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
        public ClassicRenkoConsolidator(decimal barSize,
            PyObject selector,
            PyObject volumeSelector = null,
            bool evenBars = true)
            : base(selector, volumeSelector)
        {
            EpsilonCheck(barSize);
            _barSize = barSize;
            _evenBars = evenBars;
        }

        protected override void UpdateBar(DateTime time, decimal currentValue, decimal volume)
        {
            _currentBar.Update(time, currentValue, volume);
        }

        protected override void CheckIfBarIsClosed()
        {
            if (_currentBar.IsClosed)
            {
                _lastCloseValue = _currentBar.Close;
                OnDataConsolidated(_currentBar);
                _currentBar = null;
            }
        }

        protected override void CreateNewBar(IBaseData data)
        {
            var currentValue = Selector(data);
            var volume = VolumeSelector(data);

            var open = _lastCloseValue ?? currentValue;
            if (_evenBars && !_lastCloseValue.HasValue)
            {
                open = Math.Ceiling(open / _barSize) * _barSize;
            }

            _currentBar = new RenkoBar(data.Symbol, data.Time, _barSize, open, volume);
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
    public class ClassicRenkoConsolidator<TInput> : ClassicRenkoConsolidator
        where TInput : IBaseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public ClassicRenkoConsolidator(
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
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public ClassicRenkoConsolidator(decimal barSize, bool evenBars = true)
            : this(barSize, x => x.Value, x => 0, evenBars)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="type">The RenkoType of the bar</param>
        [Obsolete("Please use the WickedRenkoConsolidator if RenkoType is not Classic")]
        public ClassicRenkoConsolidator(decimal barSize, RenkoType type)
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
