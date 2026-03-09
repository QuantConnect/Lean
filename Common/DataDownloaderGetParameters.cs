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

namespace QuantConnect
{
    /// <summary>
    /// Model class for passing in parameters for historical data
    /// </summary>
    public class DataDownloaderGetParameters
    {
        /// <summary>
        /// Symbol for the data we're looking for.
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Resolution of the data request
        /// </summary>
        public Resolution Resolution { get; set; }

        /// <summary>
        /// Start time of the data in UTC
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// End time of the data in UTC
        /// </summary>
        public DateTime EndUtc { get; set; }

        /// <summary>
        /// The type of tick to get
        /// </summary>
        public TickType TickType { get; set; }

        /// <summary>
        /// Initialize model class for passing in parameters for historical data
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <param name="tickType">[Optional] The type of tick to get. Defaults to <see cref="QuantConnect.TickType.Trade"/></param>
        public DataDownloaderGetParameters(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc, TickType? tickType = null)
        {
            Symbol = symbol;
            Resolution = resolution;
            StartUtc = startUtc;
            EndUtc = endUtc;
            TickType = tickType ?? TickType.Trade;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="DataDownloaderGetParameters"/> object.
        /// </summary>
        /// <returns>A string representing the object's properties.</returns>
        public override string ToString() => $"Symbol: {Symbol}, Resolution: {Resolution}, StartUtc: {StartUtc}, EndUtc: {EndUtc}, TickType: {TickType}";
    }
}
