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
    public class HistoricalDataDetails
    {
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
        public HistoricalDataDetails(int requestId, DateTime date, decimal open, decimal high, decimal low, decimal close,
                                       int volume, int trades, double wap, bool hasGaps)
        {
            this.RequestId = requestId;
            this.HasGaps = hasGaps;
            this.Wap = wap;
            this.Trades = trades;
            this.Volume = volume;
            this.Close = close;
            this.Low = low;
            this.High = high;
            this.Open = open;
            this.Date = date;
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
        public int RequestId {get; set;}

        /// <summary>
        /// The date-time stamp of the start of the bar.
        /// The format is determined by the reqHistoricalData() formatDate parameter.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Bar opening price.
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price during the time covered by the bar.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price during the time covered by the bar.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Bar closing price.
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Volume during the time covered by the bar.
        /// </summary>
        public int Volume { get; set; }

        /// <summary>
        /// When TRADES historical data is returned, represents the number of trades that
        /// occurred during the time period the bar covers.
        /// </summary>
        public int Trades { get; set; }

        /// <summary>
        /// Weighted average price during the time covered by the bar.
        /// </summary>
        public double Wap { get; set; }

        /// <summary>
        /// Whether or not there are gaps in the data.
        /// </summary>
        public bool HasGaps { get; set; }
    }
}
