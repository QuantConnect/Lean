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

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Historical Data Details
    /// </summary>
    [Serializable()]
    public class HistoricalDataDetails
    {
        private decimal close;
        private int trades;
        private DateTime date;
        private bool hasGaps;
        private decimal high;
        private decimal low;
        private decimal open;
        private int requestId;
        private int volume;
        private double wap;

        /// <summary>
        /// Full Constructor
        /// </summary>
        /// <param name="requestId">The ticker Id of the request to which this bar is responding.</param>
        /// <param name="date">The date-time stamp of the start of the bar.
        /// The format is determined by the reqHistoricalData() formatDate parameter.</param>
        /// <param name="open">Bar opening price.</param>
        /// <param name="high">High price during the time covered by the bar.</param>
        /// <param name="low">Low price during the time covered by the bar.</param>
        /// <param name="close">Bar closing price.</param>
        /// <param name="volume">Volume during the time covered by the bar.</param>
        /// <param name="trades">When TRADES historical data is returned, represents the number of trades that
        /// occurred during the time period the bar covers.</param>
        /// <param name="wap">Weighted average price during the time covered by the bar.</param>
        /// <param name="hasGaps">Whether or not there are gaps in the data.</param>
        /// <param name="recordNumber">Current Record Number out of Record Total.</param>
        /// <param name="recordTotal">Total Records Returned by Historical Request.</param>
        public HistoricalDataDetails(int requestId, DateTime date, decimal open, decimal high, decimal low, decimal close,
                                       int volume, int trades, double wap, bool hasGaps)
        {
            this.requestId = requestId;
            this.hasGaps = hasGaps;
            this.wap = wap;
            this.trades = trades;
            this.volume = volume;
            this.close = close;
            this.low = low;
            this.high = high;
            this.open = open;
            this.date = date;
        }

        /// <summary>
        /// Uninitialized Constructor for Serialization
        /// </summary>
        public HistoricalDataDetails()
        {

        }

        /// <summary>
        /// The ticker Id of the request to which this bar is responding.
        /// </summary>
        public int RequestId
        {
            get { return requestId; }
            set { requestId = value; }
        }

        /// <summary>
        /// The date-time stamp of the start of the bar.
        /// The format is determined by the reqHistoricalData() formatDate parameter.
        /// </summary>
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        /// <summary>
        /// Bar opening price.
        /// </summary>
        public decimal Open
        {
            get { return open; }
            set { open = value; }
        }

        /// <summary>
        /// High price during the time covered by the bar.
        /// </summary>
        public decimal High
        {
            get { return high; }
            set { high = value; }
        }

        /// <summary>
        /// Low price during the time covered by the bar.
        /// </summary>
        public decimal Low
        {
            get { return low; }
            set { low = value; }
        }

        /// <summary>
        /// Bar closing price.
        /// </summary>
        public decimal Close
        {
            get { return close; }
            set { close = value; }
        }

        /// <summary>
        /// Volume during the time covered by the bar.
        /// </summary>
        public int Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// When TRADES historical data is returned, represents the number of trades that
        /// occurred during the time period the bar covers.
        /// </summary>
        public int Trades
        {
            get { return trades; }
            set { trades = value; }
        }

        /// <summary>
        /// Weighted average price during the time covered by the bar.
        /// </summary>
        public double Wap
        {
            get { return wap; }
            set { wap = value; }
        }

        /// <summary>
        /// Whether or not there are gaps in the data.
        /// </summary>
        public bool HasGaps
        {
            get { return hasGaps; }
            set { hasGaps = value; }
        }
    }
}
