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

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Prices class used by <see cref="IFillModel"/>s
    /// </summary>
    public class Prices
    {
        /// <summary>
        /// End time for these prices
        /// </summary>
        public DateTime EndTime { get; init; }

        /// <summary>
        /// Current price
        /// </summary>
        public decimal Current { get; init; }

        /// <summary>
        /// Open price
        /// </summary>
        public decimal Open { get; init; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal High { get; init; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal Low { get; init; }

        /// <summary>
        /// Closing price
        /// </summary>
        public decimal Close { get; init; }

        /// <summary>
        /// Create an instance of Prices class with a data bar
        /// </summary>
        /// <param name="bar">Data bar to use for prices</param>
        public Prices(IBaseDataBar bar)
            : this(bar.EndTime, bar.Close, bar.Open, bar.High, bar.Low, bar.Close) { }

        /// <summary>
        /// Create an instance of Prices class with a data bar and end time
        /// </summary>
        /// <param name="endTime">The end time for these prices</param>
        /// <param name="bar">Data bar to use for prices</param>
        public Prices(DateTime endTime, IBar bar)
            : this(endTime, bar.Close, bar.Open, bar.High, bar.Low, bar.Close) { }

        /// <summary>
        /// Create a instance of the Prices class with specific values for all prices
        /// </summary>
        /// <param name="endTime">The end time for these prices</param>
        /// <param name="current">Current price</param>
        /// <param name="open">Open price</param>
        /// <param name="high">High price</param>
        /// <param name="low">Low price</param>
        /// <param name="close">Close price</param>
        public Prices(
            DateTime endTime,
            decimal current,
            decimal open,
            decimal high,
            decimal low,
            decimal close
        )
        {
            EndTime = endTime;
            Current = current;
            Open = open == 0 ? current : open;
            High = high == 0 ? current : high;
            Low = low == 0 ? current : low;
            Close = close == 0 ? current : close;
        }
    }
}
