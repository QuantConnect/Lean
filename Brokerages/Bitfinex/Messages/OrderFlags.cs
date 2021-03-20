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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Bitfinex Order Flags
    /// </summary>
    public static class OrderFlags
    {
        /// <summary>
        /// The hidden order option ensures an order does not appear in the order book; thus does not influence other market participants.
        /// </summary>
        public const int Hidden = 64;

        /// <summary>
        /// Close position if position present.
        /// </summary>
        public const int Close = 512;

        /// <summary>
        /// Ensures that the executed order does not flip the opened position.
        /// </summary>
        public const int ReduceOnly = 1024;

        /// <summary>
        /// The post-only limit order option ensures the limit order will be added to the order book and not match with a pre-existing order.
        /// </summary>
        public const int PostOnly = 4096;

        /// <summary>
        /// The one cancels other order option allows you to place a pair of orders stipulating that if one order is executed fully or partially,
        /// then the other is automatically canceled.
        /// </summary>
        public const int Oco = 16384;

        /// <summary>
        /// Excludes variable rate funding offers from matching against this order, if on margin
        /// </summary>
        public const int NoVarRates = 524288;
    }
}
