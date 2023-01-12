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
    /// This consolidator can transform a stream of <see cref="BaseData"/> instances into a stream of <see cref="RenkoBar"/>
    /// with a constant volume for each bar.
    /// </summary>
    public class VolumeRenkoConsolidator : DataConsolidator<BaseData>
    {
        private bool _firstTick = true;
        private VolumeRenkoBar _currentBar;
        private decimal _volumeLeftOver = 0m;
        private decimal _barSize;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => _currentBar;

        /// <summary>
        /// Gets <see cref="VolumeRenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType => typeof(VolumeRenkoBar);

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated
        {
            get; private set;
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event EventHandler<VolumeRenkoBar> DataConsolidated;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant volume size of each bar</param>
        public VolumeRenkoConsolidator(decimal barSize)
        {
            _barSize = barSize;
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(BaseData data)
        {
            var rate = data.Price;
            var dataType = data.GetType();

            decimal volume;
            decimal open;
            decimal high;
            decimal low;
            decimal close;

            if (dataType == typeof(TradeBar))
            {
                volume = ((TradeBar)data).Volume;
                open = ((TradeBar)data).Open;
                high = ((TradeBar)data).High;
                low = ((TradeBar)data).Low;
                close = rate;
            }
            else if (dataType == typeof(Tick))
            {
                // Only include actual trade information
                if (((Tick)data).TickType != TickType.Trade)
                {
                    return;
                }

                volume = ((Tick)data).Quantity;
                open = rate;
                high = rate;
                low = rate;
                close = rate;
            }
            else
            {
                throw new ArgumentException("VolumeRenkoConsolidator() must be used with TradeBar or Tick data.");
            }

            if (_firstTick)
            {
                _firstTick = false;
                _currentBar = new VolumeRenkoBar(data.Symbol, data.Time, data.EndTime, _barSize, open, high, low, close, volume);
            }
            else
            {
                _volumeLeftOver = _currentBar.Update(data.EndTime, high, low, close, volume);
                while (_volumeLeftOver >= 0)
                {
                    OnDataConsolidated(_currentBar);
                    _currentBar = new VolumeRenkoBar(data.Symbol, data.EndTime, data.EndTime, _barSize, close, high, low, close, 0);
                    _volumeLeftOver = _currentBar.Update(data.EndTime, high, low, close, _volumeLeftOver);
                }
            }
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public override void Scan(DateTime currentLocalTime)
        {
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected void OnDataConsolidated(VolumeRenkoBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);
            Consolidated = consolidated;
        }
    }
}
