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
        private bool _firstTick;
        private decimal _minimumPriceVariation;
        protected RangeBar CurrentRangeBar;

        /// <summary>
        /// Bar being created
        /// </summary>
        protected override TradeBar CurrentBar
        {
            get
            {
                return CurrentRangeBar;
            }
            set
            {
                CurrentRangeBar = (RangeBar)value;
            }
        }

        /// <summary>
        /// Range for each RangeBar, this is, the difference between the High and Low for each
        /// RangeBar
        /// </summary>
        public decimal RangeSize { get; private set; }

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
        public override IBaseData WorkingData => CurrentRangeBar?.Clone();

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new event EventHandler<RangeBar> DataConsolidated;

        /// <summary>
        ///Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The constant value size of each bar, where the unit is the minimum price variation of the RangeBar's symbol</param>
        public RangeConsolidator(decimal range)
            : this(range, x => x.Value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The constant value size of each bar, where the unit is the minimum price variation of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar, except if the input is a TradeBar.</param>
        public RangeConsolidator(
            decimal range,
            Func<IBaseData, decimal> selector,
            Func<IBaseData, decimal> volumeSelector = null)
            : base(selector ?? (x => x.Value), volumeSelector ?? (x => x is TradeBar bar ? bar.Volume : 0))
        {
            Range = range;
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
        public RangeConsolidator(decimal range,
            PyObject selector,
            PyObject volumeSelector = null)
            : base(selector, volumeSelector)
        {
            Range = range;
            _firstTick = true;
        }

        /// <summary>
        /// Update the current RangeBar being created with the given data.
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected override void UpdateBar(DateTime time, decimal currentValue, decimal volume)
        {
            if (currentValue > CurrentRangeBar.Low + 2 * RangeSize)
            {
                Rising(time, currentValue, volume);
            }
            else if (currentValue < CurrentRangeBar.High - (2 * RangeSize))
            {
                Falling(time, currentValue, volume);
            }
            else
            {
                CurrentRangeBar.Update(time, currentValue, volume);
            }
        }

        /// <summary>
        /// Checks if the current RangeBar being created has closed. If that is the case, it consolidates
        /// the bar, resets the current RangeBar and stores the needed information from the last bar to
        /// create the next bar
        /// <see cref="UpdateBar(DateTime, decimal, decimal)"/>
        /// </summary>
        protected override void CheckIfBarIsClosed()
        {
            if (CurrentRangeBar != null && CurrentRangeBar.IsClosed)
            {
                OnDataConsolidated(CurrentRangeBar);
                CurrentRangeBar = null;
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
                _minimumPriceVariation = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(data.Symbol.ID.Market, data.Symbol, data.Symbol.ID.SecurityType, "USD").MinimumPriceVariation;
                RangeSize = _minimumPriceVariation * Range;
                open = Math.Ceiling(open / RangeSize) * RangeSize;
                _firstTick = false;
            }

            CurrentRangeBar = new RangeBar(data.Symbol, data.Time, RangeSize, open, volume);
        }

        /// <summary>
        /// Creates intermediate/phantom RangeBar's when the price is rising
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        private void Rising(DateTime time, decimal currentValue, decimal volume)
        {
            while (currentValue - CurrentRangeBar.Low > RangeSize)
            {
                CurrentRangeBar.Update(time, currentValue, volume);
                OnDataConsolidated(CurrentRangeBar);
                CurrentRangeBar = new RangeBar(CurrentRangeBar.Symbol, CurrentRangeBar.EndTime, RangeSize, CurrentRangeBar.Close + _minimumPriceVariation, 0);
            }
            CurrentRangeBar = null;
        }

        /// <summary>
        /// Creates intermediate/phantom RangeBar's when the price is falling
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        private void Falling(DateTime time, decimal currentValue, decimal volume)
        {
            while (CurrentRangeBar.High - currentValue > RangeSize)
            {
                CurrentRangeBar.Update(time, currentValue, volume);
                OnDataConsolidated(CurrentRangeBar);
                CurrentRangeBar = new RangeBar(CurrentRangeBar.Symbol, CurrentRangeBar.EndTime, RangeSize, CurrentRangeBar.Close - _minimumPriceVariation, 0);
            }
            CurrentRangeBar = null;
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
