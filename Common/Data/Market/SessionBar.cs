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
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace Common.Data.Market
{
    /// <summary>
    /// Contains OHLCV data for a single session
    /// </summary>
    public class SessionBar : BaseData, IBaseDataBar
    {
        private IBaseDataBar _bar;
        private decimal _volume;

        /// <summary>
        /// Open Interest:
        /// </summary>
        public decimal OpenInterest { get; set; }

        /// <summary>
        /// Volume:
        /// </summary>
        public decimal Volume
        {
            get
            {
                if (_bar is TradeBar tradeBar)
                {
                    return tradeBar.Volume;
                }
                return _volume;
            }
            set => _volume = value;
        }

        /// <summary>
        /// Opening Price:
        /// </summary>
        public decimal Open => _bar?.Open ?? 0m;

        /// <summary>
        /// High Price:
        /// </summary>
        public decimal High => _bar?.High ?? 0m;

        /// <summary>
        /// Low Price:
        /// </summary>
        public decimal Low => _bar?.Low ?? 0m;

        /// <summary>
        /// Closing Price:
        /// </summary>
        public decimal Close => _bar?.Close ?? 0m;

        /// <summary>
        /// The period of this session bar
        /// </summary>
        public TimeSpan Period { get; } = TimeSpan.FromDays(1);

        /// <summary>
        /// The closing time of this bar, computed via the Time and Period
        /// </summary>
        public override DateTime EndTime
        {
            get { return _bar.Time.Date + Period; }
            set { _bar.Time = value.Date - Period; }
        }

        /// <summary>
        /// Initializes a new instance of SessionBar with default values
        /// </summary>
        public SessionBar() { }

        /// <summary>
        /// Initializes this SessionBar by referencing the underlying bar
        /// </summary>
        internal void Initialize(IBaseDataBar bar)
        {
            _bar = bar;
            Time = _bar?.Time ?? DateTime.MinValue;
        }

        /// <summary>
        /// Returns a string representation of the session bar with OHLCV and OpenInterest values formatted.
        /// Example: "O: 101.00 H: 112.00 L: 95.00 C: 110.00 V: 1005.00 OI: 12"
        /// </summary>
        public override string ToString()
        {
            return $"O: {Open.SmartRounding()} " +
                   $"H: {High.SmartRounding()} " +
                   $"L: {Low.SmartRounding()} " +
                   $"C: {Close.SmartRounding()} " +
                   $"V: {Volume.SmartRounding()} " +
                   $"OI: {OpenInterest}";
        }
    }
}