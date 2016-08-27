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
    /// This consolidator can transform a stream of <see cref="BaseData"/> instances into a stream of <see cref="WickoBar"/>
    /// </summary>
    public class WickoConsolidator : DataConsolidator<IBaseData>
    {
        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new EventHandler<WickoBar> DataConsolidated;

        private WickoBar _currentBar;

        private readonly decimal _barSize;
        private readonly Func<IBaseData, decimal> _selector;

        private bool firstTick = true;
        private WickoBar lastWicko = null;

        private DateTime openOn;
        private DateTime closeOn;
        private decimal openRate;
        private decimal highRate;
        private decimal lowRate;
        private decimal closeRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="WickoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public WickoConsolidator(decimal barSize, bool evenBars = true)
        {
            _barSize = barSize;
            _selector = x => x.Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WickoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="WickoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        public WickoConsolidator(decimal barSize, Func<IBaseData, decimal> selector)
        {
            if (barSize < Extensions.GetDecimalEpsilon())
            {
                throw new ArgumentOutOfRangeException("barSize", "WickoConsolidator bar size must be positve and greater than 1e-28");
            }

            _barSize = barSize;
            _selector = selector ?? (x => x.Value);
        }

        /// <summary>
        /// Gets the bar size used by this consolidator
        /// </summary>
        public decimal BarSize
        {
            get { return _barSize; }
        }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override BaseData WorkingData
        {
            get { return _currentBar == null ? null : _currentBar.Clone(); }
        }

        /// <summary>
        /// Gets <see cref="WickoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType
        {
            get { return typeof(WickoBar); }
        }

        internal WickoBar OpenWickoBar
        {
            get
            {
                return new WickoBar(null, openOn, closeOn,
                    BarSize, openRate, highRate, lowRate, closeRate);
            }
        }

        private void Rising(IBaseData data)
        {
            decimal limit;

            while (closeRate > (limit = (openRate + BarSize)))
            {
                var wicko = new WickoBar(data.Symbol, openOn, closeOn,
                    BarSize, openRate, limit, lowRate, limit);

                lastWicko = wicko;

                OnDataConsolidated(wicko);

                openOn = closeOn;
                openRate = limit;
                lowRate = limit;
            }
        }

        private void Falling(IBaseData data)
        {
            decimal limit;

            while (closeRate < (limit = (openRate - BarSize)))
            {
                var wicko = new WickoBar(data.Symbol, openOn, closeOn,
                    BarSize, openRate, highRate, limit, limit);

                lastWicko = wicko;

                OnDataConsolidated(wicko);

                openOn = closeOn;
                openRate = limit;
                highRate = limit;
            }
        }

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            var rate = data.Price;

            if (firstTick)
            {
                firstTick = false;

                openOn = data.Time;
                closeOn = data.Time;
                openRate = rate;
                highRate = rate;
                lowRate = rate;
                closeRate = rate;
            }
            else
            {
                closeOn = data.Time;

                if (rate > highRate)
                    highRate = rate;

                if (rate < lowRate)
                    lowRate = rate;

                closeRate = rate;

                if (closeRate > openRate)
                {
                    if (lastWicko == null ||
                        (lastWicko.Trend == WickoBarTrend.Rising))
                    {
                        Rising(data);

                        return;
                    }

                    var limit = (lastWicko.Open + BarSize);

                    if (closeRate > limit)
                    {
                        var wicko = new WickoBar(data.Symbol, openOn, closeOn,
                            BarSize, lastWicko.Open, limit, lowRate, limit);

                        lastWicko = wicko;

                        OnDataConsolidated(wicko);

                        openOn = closeOn;
                        openRate = limit;
                        lowRate = limit;

                        Rising(data);
                    }
                }
                else if (closeRate < openRate)
                {
                    if (lastWicko == null ||
                        (lastWicko.Trend == WickoBarTrend.Falling))
                    {
                        Falling(data);

                        return;
                    }

                    var limit = (lastWicko.Open - BarSize);

                    if (closeRate < limit)
                    {
                        var wicko = new WickoBar(data.Symbol, openOn, closeOn,
                            BarSize, lastWicko.Open, highRate, limit, limit);

                        lastWicko = wicko;

                        OnDataConsolidated(wicko);

                        openOn = closeOn;
                        openRate = limit;
                        highRate = limit;

                        Falling(data);
                    }
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
        protected virtual void OnDataConsolidated(WickoBar consolidated)
        {
            var handler = DataConsolidated;
            if (handler != null) handler(this, consolidated);

            base.OnDataConsolidated(consolidated);
        }
    }

    /// <summary>
    /// Provides a type safe wrapper on the WickoConsolidator class. This just allows us to define our selector functions with the real type they'll be receiving
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public class WickoConsolidator<TInput> : WickoConsolidator
        where TInput : IBaseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WickoConsolidator" /> class.
        /// </summary>
        /// <param name="barSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="WickoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        public WickoConsolidator(decimal barSize, Func<TInput, decimal> selector)
            : base(barSize, x => selector((TInput)x))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WickoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        public WickoConsolidator(decimal barSize)
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
