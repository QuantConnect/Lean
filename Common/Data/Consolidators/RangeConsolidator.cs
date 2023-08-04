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
    public class RangeConsolidator : BaseTimelessConsolidator<RangeBar>
    {
        private bool _firstTick;
        private decimal _minimumPriceVariation;

        /// <summary>
        /// Symbol properties database to use to get the minimum price variation of certain symbol
        /// </summary>
        private static SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        /// <summary>
        /// Bar being created
        /// </summary>
        protected override RangeBar CurrentBar { get; set; }

        /// <summary>
        /// Range for each RangeBar, this is, the difference between the High and Low for each
        /// RangeBar
        /// </summary>
        public decimal RangeSize { get; private set; }

        /// <summary>
        /// Number of MinimumPriceVariation units
        /// </summary>
        public int Range { get; private set; }

        /// <summary>
        /// Gets <see cref="RangeBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType => typeof(RangeBar);

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => CurrentBar?.Clone();

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The Range interval sets the range in which the price moves, which in turn initiates the formation of a new bar.
        /// One range equals to one minimum price change, where this last value is defined depending of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar, except if the input is a TradeBar.</param>
        public RangeConsolidator(
            int range,
            Func<IBaseData, decimal> selector = null,
            Func<IBaseData, decimal> volumeSelector = null)
            : base(selector, volumeSelector)
        {
            Range = range;
            _firstTick = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The Range interval sets the range in which the price moves, which in turn initiates the formation of a new bar.
        /// One range equals to one minimum price change, where this last value is defined depending of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        public RangeConsolidator(int range,
            PyObject selector,
            PyObject volumeSelector = null)
            : base(selector, volumeSelector)
        {
            Range = range;
            _firstTick = true;
        }

        /// <summary>
        /// Updates the current RangeBar being created with the given data.
        /// Additionally, if it's the case, it consolidates the current RangeBar
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected override void UpdateBar(DateTime time, decimal currentValue, decimal volume)
        {
            bool isRising = default;
            if (currentValue > CurrentBar.High)
            {
                isRising = true;
            }
            else if (currentValue < CurrentBar.Low)
            {
                isRising = false;
            }

            CurrentBar.Update(time, currentValue, volume);
            while (CurrentBar.IsClosed)
            {
                OnDataConsolidated(CurrentBar);
                CurrentBar = new RangeBar(CurrentBar.Symbol, CurrentBar.EndTime, RangeSize, isRising ? CurrentBar.High + _minimumPriceVariation : CurrentBar.Low - _minimumPriceVariation);
                CurrentBar.Update(time, currentValue, Math.Abs(CurrentBar.Low - currentValue) > RangeSize ? 0 : volume); // Intermediate/phantom RangeBar's have zero volume
            }
        }

        /// <summary>
        /// Creates a new bar with the given data
        /// </summary>
        /// <param name="data">The new data for the bar</param>
        /// <param name="currentValue">The new value for the bar</param>
        /// <param name="volume">The new volume for the bar</param>
        protected override void CreateNewBar(IBaseData data, decimal currentValue, decimal volume)
        {
            var open = currentValue;

            if (_firstTick)
            {
                _minimumPriceVariation = _symbolPropertiesDatabase.GetSymbolProperties(data.Symbol.ID.Market, data.Symbol, data.Symbol.ID.SecurityType, "USD").MinimumPriceVariation;
                RangeSize = _minimumPriceVariation * Range;
                open = Math.Ceiling(open / RangeSize) * RangeSize;
                _firstTick = false;
            }

            CurrentBar = new RangeBar(data.Symbol, data.Time, RangeSize, open, volume: volume);
        }
    }
}
