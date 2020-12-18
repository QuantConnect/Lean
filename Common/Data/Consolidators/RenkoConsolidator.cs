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
    public class ClassicRenkoConsolidator : BaseRenkoConsolidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        /// <param name="evenBars">When true bar open/close will be a multiple of the barSize</param>
        public ClassicRenkoConsolidator(decimal barSize, bool evenBars = true)
            : base(barSize, evenBars)
        {
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
            decimal barSize, Func<IBaseData, decimal> selector, Func<IBaseData, decimal> volumeSelector = null,
            bool evenBars = true
            )
            : base(barSize, selector, volumeSelector, evenBars)
        {
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
            decimal barSize, PyObject selector, PyObject volumeSelector = null, bool evenBars = true
            )
            : base(barSize, selector, volumeSelector, evenBars)
        {
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
           
            var currentValue = Selector(data);
            var volume = VolumeSelector(data);

            decimal? close = null;

            // if we're already in a bar then update it
            if (CurrentBar != null)
            {
                CurrentBar.Update(data.Time, currentValue, volume);

                // if the update caused this bar to close, fire the event and reset the bar
                if (CurrentBar.IsClosed)
                {
                    close = CurrentBar.Close;
                    OnDataConsolidated(CurrentBar);
                    CurrentBar = null;
                }
            }

            if (CurrentBar == null)
            {
                var open = close ?? currentValue;
                if (EvenBars && !close.HasValue)
                {
                    open = Math.Ceiling(open / BarSize) * BarSize;
                }

                CurrentBar = new RenkoBar(data.Symbol, data.Time, BarSize, open, volume);
            }            
        }


    }

    /// <summary>
    /// Provides a type safe wrapper on the RenkoConsolidator class. This just allows us to define our selector functions with the real type they'll be receiving
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public class RenkoConsolidator<TInput> : ClassicRenkoConsolidator
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
        public RenkoConsolidator(
            decimal barSize, Func<TInput, decimal> selector, Func<TInput, decimal> volumeSelector = null,
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
        public RenkoConsolidator(decimal barSize, bool evenBars = true)
            : base(barSize, evenBars)
        {
        }

        /// <summary>
        /// Updates this consolidator with the specified data.
        /// </summary>
        /// <remarks>
        /// Type safe shim method.
        /// </remarks>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            base.Update(data);
        }

    }
}
