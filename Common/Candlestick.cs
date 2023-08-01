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
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Single candlestick for a candlestick chart
    /// </summary>
    [JsonConverter(typeof(CandlestickJsonConverter))]
    public class Candlestick : ISeriesPoint
    {
        /// <summary>
        /// The candlestick time
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The candlestick time in seconds since Unix Epoch
        /// </summary>
        public long LongTime
        {
            get
            {
                return (long)QuantConnect.Time.DateTimeToUnixTimeStamp(Time);
            }
        }

        /// <summary>
        /// List of values for this candlestick.
        /// A candlestick is represented as a list of length 4, with the values being (open, high, low, close).
        /// </summary>
        public List<decimal> Values { get; }

        /// <summary>
        /// The candlestick open price
        /// </summary>
        public decimal Open { get { return Values[0]; } }

        /// <summary>
        /// The candlestick high price
        /// </summary>
        public decimal High { get { return Values[1]; } }

        /// <summary>
        /// The candlestick low price
        /// </summary>
        public decimal Low { get { return Values[2]; } }

        /// <summary>
        /// The candlestick close price
        /// </summary>
        public decimal Close { get { return Values[3]; } }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Candlestick()
        {
            Values = new List<decimal>() { 0, 0, 0, 0 };
        }

        /// <summary>
        /// Constructor taking the candlestick values
        /// </summary>
        /// <param name="time">Candlestick time in seconds since Unix Epoch</param>
        /// <param name="open">Candlestick open price</param>
        /// <param name="high">Candlestick high price</param>
        /// <param name="low">Candlestick low price</param>
        /// <param name="close">Candlestick close price</param>
        public Candlestick(long time, decimal open, decimal high, decimal low, decimal close)
        {
            Time = QuantConnect.Time.UnixTimeStampToDateTime(time);
            Values = new List<decimal>() { open, high, low, close };
        }

        /// <summary>
        /// Constructor taking candlestick values and time in DateTime format
        /// </summary>
        /// <param name="time">Candlestick time in seconds</param>
        /// <param name="open">Candlestick open price</param>
        /// <param name="high">Candlestick high price</param>
        /// <param name="low">Candlestick low price</param>
        /// <param name="close">Candlestick close price</param>
        public Candlestick(DateTime time, decimal open, decimal high, decimal low, decimal close)
        {
            Time = time.ToUniversalTime();
            Values = new List<decimal>() { open, high, low, close };
        }

        /// <summary>
        /// Constructor taking candlestick values and time in DateTime format
        /// </summary>
        /// <param name="time">Candlestick time in seconds</param>
        /// <param name="open">Candlestick open price</param>
        /// <param name="high">Candlestick high price</param>
        /// <param name="low">Candlestick low price</param>
        /// <param name="close">Candlestick close price</param>
        public Candlestick(TradeBar bar)
            : this(bar.EndTime.ToUniversalTime(), bar.Open, bar.High, bar.Low, bar.Close)
        {
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="candlestick">Candlestick to copy from</param>
        public Candlestick(Candlestick candlestick)
            : this(candlestick.Time, candlestick.Open, candlestick.High, candlestick.Low, candlestick.Close)
        {
        }

        /// <summary>
        /// Provides a readable string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return Messages.Candlestick.ToString(this);
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public ISeriesPoint Clone()
        {
            return new Candlestick(this);
        }
    }
}