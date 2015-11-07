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

using System.Threading;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Base Bar Class: Open, High, Low, Close and Period.
    /// </summary>
    public class Bar : IBar
    {
        private int _initialized;
        private decimal _open;
        private decimal _high;
        private decimal _low;
        private decimal _close;

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open
        {
            get { return _open; }
            set
            {
                Initialize(value);
                _open = value;
            }
        }

        /// <summary>
        /// High price of the bar during the time period.
        /// </summary>
        public decimal High
        {
            get { return _high; }
            set
            {
                Initialize(value); 
                _high = value;
            }
        }

        /// <summary>
        /// Low price of the bar during the time period.
        /// </summary>
        public decimal Low
        {
            get { return _low; }
            set
            {
                Initialize(value); 
                _low = value;
            }
        }

        /// <summary>
        /// Closing price of the bar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close
        {
            get { return _close; }
            set
            {
                Initialize(value);
                _close = value;
            }
        }

        /// <summary>
        /// Default initializer to setup an empty bar.
        /// </summary>
        public Bar()
        {
        }

        /// <summary>
        /// Initializer to setup a bar with a given information.
        /// </summary>
        /// <param name="open">Decimal Opening Price</param>
        /// <param name="high">Decimal High Price of this bar</param>
        /// <param name="low">Decimal Low Price of this bar</param>
        /// <param name="close">Decimal Close price of this bar</param>
        public Bar(decimal open, decimal high, decimal low, decimal close)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
            _initialized = 1;
        }

        /// <summary>
        /// Updates the bar with a new value. This will aggregate the OHLC bar
        /// </summary>
        /// <param name="value">The new value</param>
        public void Update(decimal value)
        {
            Initialize(value);
            if (value > High) High = value;
            if (value < Low) Low = value;
            Close = value;
        }

        /// <summary>
        /// Returns a clone of this bar
        /// </summary>
        public Bar Clone()
        {
            return new Bar(Open, High, Low, Close);
        }

        /// <summary>
        /// Initializes this bar with a first data point
        /// </summary>
        /// <param name="value">The seed value for this bar</param>
        private void Initialize(decimal value)
        {
            // require that the first initialization point must be non-zero
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0 && value != 0)
            {
                _open = _high = _low = _close = value;
            }
        }
    }
}