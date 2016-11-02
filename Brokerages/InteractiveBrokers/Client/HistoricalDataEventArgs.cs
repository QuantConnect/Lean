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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.HistoricalData"/> event
    /// </summary>
    public class HistoricalDataEventArgs : EventArgs
    {
        /// <summary>
        /// The request's identifier.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// The date-time stamp of the start of the bar. 
        /// The format is determined by the reqHistoricalData() formatDate parameter 
        /// (either as a yyyymmss hh:mm:ss formatted string or as system time according to the request).
        /// </summary>
        public string Date { get; private set; }

        /// <summary>
        /// The bar opening price.
        /// </summary>
        public double Open { get; private set; }

        /// <summary>
        /// The high price during the time covered by the bar.
        /// </summary>
        public double High { get; private set; }

        /// <summary>
        /// The low price during the time covered by the bar.
        /// </summary>
        public double Low { get; private set; }

        /// <summary>
        /// The bar closing price.
        /// </summary>
        public double Close { get; private set; }

        /// <summary>
        /// The volume during the time covered by the bar.
        /// </summary>
        public int Volume { get; private set; }

        /// <summary>
        /// When TRADES historical data is returned, represents the number of trades that occurred during the time period the bar covers.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The weighted average price during the time covered by the bar.
        /// </summary>
        public double Wap { get; private set; }

        /// <summary>
        /// Whether or not there are gaps in the data.
        /// </summary>
        public bool HasGaps { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoricalDataEventArgs"/> class
        /// </summary>
        public HistoricalDataEventArgs(int requestId, string date, double open, double high, double low, double close, int volume, int count, double wap, bool hasGaps)
        {
            RequestId = requestId;
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Count = count;
            Wap = wap;
            HasGaps = hasGaps;
        }
    }
}