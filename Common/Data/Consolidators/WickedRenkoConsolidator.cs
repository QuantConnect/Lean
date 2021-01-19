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
    /// with Renko type <see cref="RenkoType.Wicked"/>.
    /// </summary>
    public class WickedRenkoConsolidator : IDataConsolidator
    {
        private bool _firstTick = true;
        private RenkoBar _lastWicko;
        private DataConsolidatedHandler _dataConsolidatedHandler;
        private RenkoBar _currentBar;
        private IBaseData _consolidated;
        
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
        /// Gets <see cref="RenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public Type OutputType => typeof(RenkoBar);

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
        /// Initializes a new instance of the <see cref="WickedRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        public WickedRenkoConsolidator(decimal barSize)
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

                if (CloseRate > OpenRate)
                {
                    if (_lastWicko == null || _lastWicko.Direction == BarDirection.Rising)
                    {
                        Rising(data);
                        return;
                    }

                    var limit = _lastWicko.Open + BarSize;

                    if (CloseRate > limit)
                    {
                        var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, _lastWicko.Open, limit,
                            LowRate, limit);

                        _lastWicko = wicko;

                        OnDataConsolidated(wicko);

                        OpenOn = CloseOn;
                        OpenRate = limit;
                        LowRate = limit;

                        Rising(data);
                    }
                }
                else if (CloseRate < OpenRate)
                {
                    if (_lastWicko == null || _lastWicko.Direction == BarDirection.Falling)
                    {
                        Falling(data);
                        return;
                    }

                    var limit = _lastWicko.Open - BarSize;

                    if (CloseRate < limit)
                    {
                        var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, _lastWicko.Open, HighRate,
                            limit, limit);

                        _lastWicko = wicko;

                        OnDataConsolidated(wicko);

                        OpenOn = CloseOn;
                        OpenRate = limit;
                        HighRate = limit;

                        Falling(data);
                    }
                }
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

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected void OnDataConsolidated(RenkoBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);
            _currentBar = consolidated;
            _dataConsolidatedHandler?.Invoke(this, consolidated);
            Consolidated = consolidated;
        }

        private void Rising(IBaseData data)
        {
            decimal limit;

            while (CloseRate > (limit = OpenRate + BarSize))
            {
                var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, OpenRate, limit, LowRate, limit);

                _lastWicko = wicko;

                OnDataConsolidated(wicko);

                OpenOn = CloseOn;
                OpenRate = limit;
                LowRate = limit;
            }
        }

        private void Falling(IBaseData data)
        {
            decimal limit;

            while (CloseRate < (limit = OpenRate - BarSize))
            {
                var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, OpenRate, HighRate, limit, limit);

                _lastWicko = wicko;

                OnDataConsolidated(wicko);

                OpenOn = CloseOn;
                OpenRate = limit;
                HighRate = limit;
            }
        }
    }

    /// <summary>
    /// Provides a type safe wrapper on the WickedRenkoConsolidator class. This just allows us to define our selector functions with the real type they'll be receiving
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public class WickedRenkoConsolidator<TInput> : WickedRenkoConsolidator
        where TInput : IBaseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WickedRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        public WickedRenkoConsolidator(decimal barSize)
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
