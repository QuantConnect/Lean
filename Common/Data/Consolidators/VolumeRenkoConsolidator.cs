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
        private VolumeRenkoBar _currentBar;
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
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new event EventHandler<VolumeRenkoBar> DataConsolidated;

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
            var close = data.Price;
            var dataType = data.GetType();

            decimal volume;
            decimal open;
            decimal high;
            decimal low;

            if (dataType == typeof(TradeBar))
            {
                var tradeBar = (TradeBar)data;
                volume = tradeBar.Volume;
                open = tradeBar.Open;
                high = tradeBar.High;
                low = tradeBar.Low;
            }
            else if (dataType == typeof(Tick))
            {
                var tick = (Tick)data;
                // Only include actual trade information
                if (tick.TickType != TickType.Trade)
                {
                    return;
                }

                volume = tick.Quantity;
                open = close;
                high = close;
                low = close;
            }
            else
            {
                throw new ArgumentException($"{GetType().Name} must be used with TradeBar or Tick data.");
            }

            var adjustedVolume = AdjustVolume(volume, close);

            if (_currentBar == null)
            {
                _currentBar = new VolumeRenkoBar(data.Symbol, data.Time, data.EndTime, _barSize, open, high, low, close, 0);
            }
            var volumeLeftOver = _currentBar.Update(data.EndTime, high, low, close, adjustedVolume);
            while (volumeLeftOver >= 0)
            {
                OnDataConsolidated(_currentBar);
                _currentBar = _currentBar.Rollover();
                volumeLeftOver = _currentBar.Update(data.EndTime, high, low, close, volumeLeftOver);
            }
        }

        /// <summary>
        /// Returns the raw volume without any adjustment.
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <param name="price">The price</param>
        /// <returns>The unmodified volume</returns>
        protected virtual decimal AdjustVolume(decimal volume, decimal price)
        {
            return volume;
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public override void Scan(DateTime currentLocalTime)
        {
        }

        /// <summary>
        /// Resets the consolidator
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _currentBar = null;
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected void OnDataConsolidated(VolumeRenkoBar consolidated)
        {
            base.OnDataConsolidated(consolidated);
            DataConsolidated?.Invoke(this, consolidated);
        }
    }
}