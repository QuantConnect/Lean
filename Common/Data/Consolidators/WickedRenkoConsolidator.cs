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
    public class WickedRenkoConsolidator : BaseRenkoConsolidator
    {
        private DateTime _closeOn;
        private decimal _closeRate;
        private bool _firstTick = true;
        private decimal _highRate;
        private RenkoBar _lastWicko;
        private decimal _lowRate;
        private DateTime _openOn;
        private decimal _openRate;


        /// <summary>
        /// Initializes a new instance of the <see cref="WickedRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        public WickedRenkoConsolidator(decimal barSize)
            : base(barSize)
        {
        }

        // Used for unit tests
        internal RenkoBar OpenRenkoBar =>
            new RenkoBar(null, _openOn, _closeOn, BarSize, _openRate, _highRate, _lowRate, _closeRate);

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            var rate = data.Price;

            if (_firstTick)
            {
                _firstTick = false;

                _openOn = data.Time;
                _closeOn = data.Time;
                _openRate = rate;
                _highRate = rate;
                _lowRate = rate;
                _closeRate = rate;
            }
            else
            {
                _closeOn = data.Time;

                if (rate > _highRate) _highRate = rate;

                if (rate < _lowRate) _lowRate = rate;

                _closeRate = rate;

                if (_closeRate > _openRate)
                {
                    if (_lastWicko == null || _lastWicko.Direction == BarDirection.Rising)
                    {
                        Rising(data);
                        return;
                    }

                    var limit = _lastWicko.Open + BarSize;

                    if (_closeRate > limit)
                    {
                        var wicko = new RenkoBar(data.Symbol, _openOn, _closeOn, BarSize, _lastWicko.Open, limit,
                            _lowRate, limit);

                        _lastWicko = wicko;

                        OnDataConsolidated(wicko);

                        _openOn = _closeOn;
                        _openRate = limit;
                        _lowRate = limit;

                        Rising(data);
                    }
                }
                else if (_closeRate < _openRate)
                {
                    if (_lastWicko == null || _lastWicko.Direction == BarDirection.Falling)
                    {
                        Falling(data);
                        return;
                    }

                    var limit = _lastWicko.Open - BarSize;

                    if (_closeRate < limit)
                    {
                        var wicko = new RenkoBar(data.Symbol, _openOn, _closeOn, BarSize, _lastWicko.Open, _highRate,
                            limit, limit);

                        _lastWicko = wicko;

                        OnDataConsolidated(wicko);

                        _openOn = _closeOn;
                        _openRate = limit;
                        _highRate = limit;

                        Falling(data);
                    }
                }
            }
        }

        private void Rising(IBaseData data)
        {
            decimal limit;

            while (_closeRate > (limit = _openRate + BarSize))
            {
                var wicko = new RenkoBar(data.Symbol, _openOn, _closeOn, BarSize, _openRate, limit, _lowRate, limit);

                _lastWicko = wicko;

                OnDataConsolidated(wicko);

                _openOn = _closeOn;
                _openRate = limit;
                _lowRate = limit;
            }
        }

        private void Falling(IBaseData data)
        {
            decimal limit;

            while (_closeRate < (limit = _openRate - BarSize))
            {
                var wicko = new RenkoBar(data.Symbol, _openOn, _closeOn, BarSize, _openRate, _highRate, limit, limit);

                _lastWicko = wicko;

                OnDataConsolidated(wicko);

                _openOn = _closeOn;
                _openRate = limit;
                _highRate = limit;
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
    }
}
