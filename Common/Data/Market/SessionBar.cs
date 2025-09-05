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
        /// <summary>
        /// Volume:
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Open Interest:
        /// </summary>
        public decimal OpenInterest { get; set; }

        /// <summary>
        /// Opening Price:
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High Price:
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low Price:
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Closing Price:
        /// </summary>
        public decimal Close { get; set; }

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

            switch (data)
            {
                case TradeBar tradeBar:
                    EndTime = tradeBar.EndTime;
                    Open = tradeBar.Open;
                    High = tradeBar.High;
                    Low = tradeBar.Low;
                    Close = tradeBar.Close;
                    Volume = tradeBar.Volume;
                    break;

                case QuoteBar quoteBar:
                    EndTime = quoteBar.EndTime;
                    Open = quoteBar.Open;
                    High = quoteBar.High;
                    Low = quoteBar.Low;
                    Close = quoteBar.Close;
                    break;
            }
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