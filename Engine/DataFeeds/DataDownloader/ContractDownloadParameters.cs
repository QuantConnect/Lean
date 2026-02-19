/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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
 *
*/

using System;

namespace QuantConnect.Lean.Engine.DataFeeds.DataDownloader
{
    /// <summary>
    /// Represents a unique key for caching contract download operations,
    /// combining the contract symbol, tick type, and resolution to prevent duplicate downloads.
    /// </summary>
    public class ContractDownloadParameters
    {
        /// <summary>
        /// The contract symbol.
        /// </summary>
        private readonly Symbol _contract;

        /// <summary>
        /// The tick type of the data to download.
        /// </summary>
        private readonly TickType _tickType;

        /// <summary>
        /// The resolution of the data to download.
        /// </summary>
        private readonly Resolution _resolution;

        /// <summary>
        /// Initializes a new instance of <see cref="ContractDownloadParameters"/>.
        /// </summary>
        /// <param name="contract">The contract symbol.</param>
        /// <param name="tickType">The tick type of the data to download.</param>
        /// <param name="resolution">The resolution of the data to download.</param>
        public ContractDownloadParameters(Symbol contract, TickType tickType, Resolution resolution)
        {
            _contract = contract;
            _tickType = tickType;
            _resolution = resolution;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ContractDownloadParameters other &&
            _contract == other._contract &&
            _tickType == other._tickType &&
            _resolution == other._resolution;
        }

        /// <summary>
        /// Returns a hash code based on the contract, tick type, and resolution.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(_contract, _tickType, _resolution);
        }
    }
}
