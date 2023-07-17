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
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// This consolidator can transform a stream of <see cref="IBaseData"/> instances into a stream of <see cref="RangeBar"/>
    /// </summary>
    public class RangeConsolidator : BaseTimelessConsolidator
    {
        private RangeBar _currentBar;
        private bool _firstTick;
        private readonly bool _allowPhantomBars;

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
                _currentBar = (RangeBar)value;
            }
        }

        /// <summary>
        /// Range for each RangeBar, this is, the difference between the High and Low for each
        /// RangeBar
        /// </summary>
        public decimal RangeSize { get; private set; }

        /// <summary>
        /// Minimum Bar's Symbol price variation
        /// </summary>
        public decimal MinimumPriceVariation { get; private set; }

        /// <summary>
        /// Number of MinimumPriceVariation units
        /// </summary>
        public decimal Range { get; private set; }

        /// <summary>
        /// Gets <see cref="RangeBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType => typeof(RangeBar);

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => _currentBar?.Clone();

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new event EventHandler<RangeBar> DataConsolidated;

        /// <summary>
        ///Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The constant value size of each bar, where the unit is the minimum price variation of the RangeBar's symbol</param>
        /// /// <param name="allowPhantomBars">If set to true, allows the indicator to create phantom/intermediate bars if needed</param>
        public RangeConsolidator(decimal range, bool allowPhantomBars = false)
            : this(range, x => x.Value, allowPhantomBars: allowPhantomBars)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The constant value size of each bar, where the unit is the minimum price variation of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="allowPhantomBars">If set to true, allows the indicator to create phantom/intermediate bars if needed</param>
        public RangeConsolidator(
            decimal range,
            Func<IBaseData, decimal> selector,
            Func<IBaseData, decimal> volumeSelector = null,
            bool allowPhantomBars = false)
            : base(selector ?? (x => x.Value), volumeSelector ?? (x => 0))
        {
            Range = range;
            _allowPhantomBars = allowPhantomBars;
            _firstTick = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The constant value size of each bar, where the unit is the minimum price variation of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        /// <param name="allowPhantomBars">If set to true, allows the indicator to create phantom/intermediate bars if needed</param>
        public RangeConsolidator(decimal range,
            PyObject selector,
            PyObject volumeSelector = null,
            bool allowPhantomBars = false)
            : base(selector, volumeSelector)
        {
            Range = range;
            _allowPhantomBars = allowPhantomBars;
            _firstTick = true;
        }

        /// <summary>
        /// Update the current bar being created with the given data. If allowPhantomBars is
        /// set to true, it also creates intermediate/phantom bars if needed
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected override void UpdateBar(DateTime time, decimal currentValue, decimal volume)
        {
            if (_allowPhantomBars)
            {
                if (currentValue > _currentBar.Low + 2 * RangeSize)
                {
                    Rising(time, currentValue, volume);
                }
                else if (currentValue < _currentBar.High - (2 * RangeSize))
                {
                    Falling(time, currentValue, volume);
                }
                else
                {
                    _currentBar.Update(time, currentValue, volume);
                }
            }
            else
            {
                _currentBar.Update(time, currentValue, volume);
            }
        }

        /// <summary>
        /// Checks if the current bar being created has closed. If that is the case, it consolidates
        /// the bar, resets the current bar and stores the needed information from the last bar to
        /// create the next bar. If the current bar is null it means phantom bars were created on
        /// <see cref="UpdateBar(DateTime, decimal, decimal)"/>
        /// </summary>
        protected override void CheckIfBarIsClosed()
        {
            if (_currentBar != null && _currentBar.IsClosed)
            {
                OnDataConsolidated(_currentBar);
                _currentBar = null;
            }
        }

        /// <summary>
        /// Creates a new bar with the given data
        /// </summary>
        /// <param name="data">The new data for the bar</param>
        protected override void CreateNewBar(IBaseData data)
        {
            var currentValue = Selector(data);
            var volume = VolumeSelector(data);
            var open = currentValue;

            if (_firstTick)
            {
                MinimumPriceVariation = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(data.Symbol.ID.Market, data.Symbol, data.Symbol.ID.SecurityType, "USD").MinimumPriceVariation;
                RangeSize = MinimumPriceVariation * Range;
                open = Math.Ceiling(open / RangeSize) * RangeSize;
                _firstTick = false;
            }

            _currentBar = new RangeBar(data.Symbol, data.Time, RangeSize, open, volume);
        }

        /// <summary>
        /// Creates intermediate/phantom RangeBar's when the price is rising
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        private void Rising(DateTime time, decimal currentValue, decimal volume)
        {
            while (currentValue - _currentBar.Low > RangeSize)
            {
                _currentBar.Update(time, currentValue, volume);
                OnDataConsolidated(_currentBar);
                _currentBar = new RangeBar(_currentBar.Symbol, _currentBar.EndTime, RangeSize, _currentBar.Close + MinimumPriceVariation, 0);
            }
            _currentBar = null;
        }

        /// <summary>
        /// Creates intermediate/phantom RangeBar's when the price is falling
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        private void Falling(DateTime time, decimal currentValue, decimal volume)
        {
            while (_currentBar.High - currentValue > RangeSize)
            {
                _currentBar.Update(time, currentValue, volume);
                OnDataConsolidated(_currentBar);
                _currentBar = new RangeBar(_currentBar.Symbol, _currentBar.EndTime, RangeSize, _currentBar.Close - MinimumPriceVariation, 0);
            }
            _currentBar = null;
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        private void OnDataConsolidated(RangeBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);

            DataConsolidatedHandler?.Invoke(this, consolidated);

            Consolidated = consolidated;
        }
    }
}
