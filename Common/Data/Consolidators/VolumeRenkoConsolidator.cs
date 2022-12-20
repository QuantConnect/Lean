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
    public class VolumeRenkoConsolidator : IDataConsolidator
    {
        private bool _firstTick = true;
        private DataConsolidatedHandler _dataConsolidatedHandler;
        private VolumeRenkoBar _currentBar;
        private IBaseData _consolidated;
        private decimal _volumeLeftOver = 0m;
        
        /// <summary>
        /// Time of consolidated close.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected DateTime CloseOn;

        /// <summary>
        /// Value of consolidated close.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected decimal CloseRate;

        /// <summary>
        /// Value of consolidated high.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected decimal HighRate;

        /// <summary>
        /// Value of consolidated low.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected decimal LowRate;

        /// <summary>
        /// Time of consolidated open.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected DateTime OpenOn;

        /// <summary>
        /// Value of consolidate open.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected decimal OpenRate;

        /// <summary>
        /// Size of the consolidated bar.
        /// </summary>
        /// <remarks>Protected for testing</remarks>
        protected decimal BarSize;

        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type => RenkoType.Wicked;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public IBaseData WorkingData => _currentBar?.Clone();

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => typeof(IBaseData);

        /// <summary>
        /// Gets <see cref="VolumeRenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public Type OutputType => typeof(VolumeRenkoBar);

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated
        {
            get { return _consolidated; }
            private set { _consolidated = value; }
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event EventHandler<VolumeRenkoBar> DataConsolidated;

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        event DataConsolidatedHandler IDataConsolidator.DataConsolidated
        {
            add { _dataConsolidatedHandler += value; }
            remove { _dataConsolidatedHandler -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant volume size of each bar</param>
        public VolumeRenkoConsolidator(decimal barSize)
        {
            BarSize = barSize;
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            var rate = data.Price;
            var volume = _volumeLeftOver;

            if (data.GetType() == typeof(QuoteBar))
            {
                throw new ArgumentException("VolumeRenkoConsolidator() must be used with TradeBar or Tick data.");
            }
            else if (data.GetType() == typeof(Tick))
            {
                volume += ((Tick)data).Quantity;

                if (_firstTick)
                {
                    _firstTick = false;

                    OpenOn = data.Time;
                    CloseOn = data.Time;
                    OpenRate = rate;
                    HighRate = rate;
                    LowRate = rate;
                    CloseRate = rate;
                }
                else
                {
                    CloseOn = data.Time;

                    if (rate > HighRate)
                    {
                        HighRate = rate;
                    }

                    if (rate < LowRate)
                    {
                        LowRate = rate;
                    }

                    CloseRate = rate;
                }
            }
            else if (data.GetType() == typeof(TradeBar))
            {
                volume += ((TradeBar)data).Volume;
                var open = ((TradeBar)data).Open;
                var high = ((TradeBar)data).High;
                var low = ((TradeBar)data).Low;
                var close = ((TradeBar)data).Close;

                if (_firstTick)
                {
                    _firstTick = false;

                    OpenOn = data.Time;
                    CloseOn = data.EndTime;
                    OpenRate = open;
                    HighRate = high;
                    LowRate = low;
                    CloseRate = close;
                }
                else
                {
                    CloseOn = data.EndTime;

                    if (high > HighRate)
                    {
                        HighRate = high;
                    }

                    if (low < LowRate)
                    {
                        LowRate = low;
                    }

                    CloseRate = close;
                }
            }

            _volumeLeftOver = Next(data, volume);
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

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected void OnDataConsolidated(VolumeRenkoBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);
            _currentBar = consolidated;
            _dataConsolidatedHandler?.Invoke(this, consolidated);
            Consolidated = consolidated;
        }

        private decimal Next(IBaseData data, decimal volume)
        {
            while (volume >= BarSize)
            {
                var wicko = new VolumeRenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, OpenRate, HighRate, LowRate, CloseRate, BarSize);
                volume -= BarSize;

                OnDataConsolidated(wicko);

                OpenOn = CloseOn;
                OpenRate = CloseRate;
                HighRate = CloseRate;
                LowRate = CloseRate;
            }

            return volume;
        }
    }

    /// <summary>
    /// Provides a type safe wrapper on the RenkoConsolidator class. This just allows us to define our selector functions with the real type they'll be receiving
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public class VolumeRenkoConsolidator<TInput> : VolumeRenkoConsolidator
        where TInput : IBaseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant volume size of each bar</param>
        public VolumeRenkoConsolidator(decimal barSize)
            : base(barSize)
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
