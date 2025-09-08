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
        private IBaseData _bar;
        private decimal _open;
        private decimal _high;
        private decimal _low;
        private decimal _close;
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
            get => _bar switch
            {
                TradeBar t => t.Volume,
                _ => _volume
            };
            set => _volume = value;
        }

        /// <summary>
        /// Opening Price:
        /// </summary>
        public decimal Open
        {
            get => _bar switch
            {
                TradeBar t => t.Open,
                QuoteBar q => q.Open,
                _ => _open
            };
            set => _open = value;
        }

        /// <summary>
        /// High Price:
        /// </summary>
        public decimal High
        {
            get => _bar switch
            {
                TradeBar t => t.High,
                QuoteBar q => q.High,
                _ => _high
            };
            set => _high = value;
        }

        /// <summary>
        /// Low Price:
        /// </summary>
        public decimal Low
        {
            get => _bar switch
            {
                TradeBar t => t.Low,
                QuoteBar q => q.Low,
                _ => _low
            };
            set => _low = value;
        }

        /// <summary>
        /// Closing Price:
        /// </summary>
        public decimal Close
        {
            get => _bar switch
            {
                TradeBar t => t.Close,
                QuoteBar q => q.Close,
                _ => _close
            };
            set => _close = value;
        }

        /// <summary>
        /// The period of this session bar
        /// </summary>
        public TimeSpan Period { get; } = TimeSpan.FromDays(1);

        /// <summary>
        /// The closing time of this bar, computed via the Time and Period
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Time = value.Date - Period; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionBar"/> class
        /// </summary>
        public SessionBar(DateTime endTime, Symbol symbol, decimal open, decimal high, decimal low, decimal close, decimal volume, decimal openInterest)
        {
            EndTime = endTime;
            Symbol = symbol;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            OpenInterest = openInterest;
        }

        /// <summary>
        /// Initializes a new instance of SessionBar with default values
        /// </summary>
        public SessionBar() { }

        /// <summary>
        /// Updates the session bar
        /// </summary>
        public void Update(IBaseData data)
        {
            if (data == null)
            {
                return;
            }
            _bar = data;
            EndTime = data.EndTime;
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