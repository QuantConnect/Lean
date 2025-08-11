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
using ProtoBuf;
using QuantConnect.Indicators;
using System.Threading;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a daily trading session with OHLCV data and rolling window functionality.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class Session : IBar
    {
        private readonly RollingWindow<SessionBar> _window;
        private int _initialized;
        private decimal _open;
        private decimal _high;
        private decimal _low;
        private decimal _close;
        private decimal _volume;

        /// <summary>
        /// Rolling window of session bars (default size: 2)
        /// </summary>
        [ProtoMember(101)]
        public RollingWindow<SessionBar> Window => _window;

        /// <summary>
        /// Opening price of the session
        /// </summary>
        [ProtoMember(102)]
        public decimal Open
        {
            get { return _open; }
            private set
            {
                Initialize(value);
                _open = value;
            }
        }

        /// <summary>
        /// High price of the session
        /// </summary>
        [ProtoMember(103)]
        public decimal High
        {
            get { return _high; }
            private set
            {
                Initialize(value);
                _high = value;
            }
        }

        /// <summary>
        /// Low price of the session
        /// </summary>
        [ProtoMember(104)]
        public decimal Low
        {
            get { return _low; }
            private set
            {
                Initialize(value);
                _low = value;
            }
        }

        /// <summary>
        /// Closing price of the session
        /// </summary>
        [ProtoMember(105)]
        public decimal Close
        {
            get { return _close; }
            private set
            {
                Initialize(value);
                _close = value;
            }
        }

        /// <summary>
        /// Volume traded during the session
        /// </summary>
        [ProtoMember(106)]
        public decimal Volume
        {
            get { return _volume; }
            private set
            {
                Initialize(value);
                _volume = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="windowSize">Size of the rolling window (default: 2)</param>
        public Session(int windowSize = 2)
        {
            _window = new RollingWindow<SessionBar>(windowSize);
        }

        /// <summary>
        /// Updates the session with new price data
        /// </summary>
        public void Update(DateTime time, decimal price, decimal volume)
        {
            if (_window.Count == 0 || IsNewSession(time))
            {
                StartNewSession(time, price);
            }

            // Update current values
            High = Math.Max(High, price);
            Low = Math.Min(Low, price);
            Close = price;
            Volume += volume;

            // Update rolling window
            _window[0] = new SessionBar(time, Open, High, Low, Close, Volume);
        }

        private bool IsNewSession(DateTime time)
        {
            return _window.Count > 0 && time.Date != _window[0].Time.Date;
        }

        private void StartNewSession(DateTime time, decimal openPrice)
        {
            var newBar = new SessionBar(time, openPrice, openPrice, openPrice, openPrice, 0);
            _window.Add(newBar);

            // Reset values
            Open = High = Low = Close = openPrice;
            Volume = 0;
        }

        private void Initialize(decimal value)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                _open = _high = _low = _close = value;
            }
        }
    }

    /// <summary>
    /// Contains OHLCV data for a single session
    /// </summary>
    [ProtoContract]
    public class SessionBar : IBar
    {
        /// <summary>
        /// Current time marker.
        /// </summary>
        [ProtoMember(1)]
        public DateTime Time { get; private set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        [ProtoMember(2)]
        public decimal Open { get; private set; }

        /// <summary>
        /// High price of the bar during the time period.
        /// </summary>
        [ProtoMember(3)]
        public decimal High { get; private set; }

        /// <summary>
        /// Low price of the bar during the time period.
        /// </summary>
        [ProtoMember(4)]
        public decimal Low { get; private set; }

        /// <summary>
        /// Closing price of the bar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        [ProtoMember(5)]
        public decimal Close { get; private set; }

        /// <summary>
        /// Volume:
        /// </summary>
        [ProtoMember(6)]
        public decimal Volume { get; private set; }

        public SessionBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
    }
}