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

namespace QuantConnect.ToolBox.CryptoiqDownloader
{
    /// <summary>
    /// Defines the JSON response structure from the Cryptoiq API
    /// </summary>
    public class CryptoiqBitcoin
    {
        /// <summary>
        /// The time of the tick
        /// </summary>
        public DateTime Time;
        /// <summary>
        /// The ask price
        /// </summary>
        public decimal Ask;
        /// <summary>
        /// The bid price
        /// </summary>
        public decimal Bid;
        /// <summary>
        /// The price of the last trade
        /// </summary>
        public decimal Last;
        /// <summary>
        /// The daily high
        /// </summary>
        public decimal High;
        /// <summary>
        /// The daily low
        /// </summary>
        public decimal Low;
        /// <summary>
        /// The daily running volume
        /// </summary>
        public decimal Volume;
    }

}
