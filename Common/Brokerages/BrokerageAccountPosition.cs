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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable position held by a brokerage account.
    /// </summary>
    public class BrokerageAccountPosition
    {
        /// <summary>
        /// Gets the LEAN symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the signed position quantity.
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Gets the average position price.
        /// </summary>
        public decimal AveragePrice { get; }

        /// <summary>
        /// Gets the brokerage model code, when available.
        /// </summary>
        public string ModelCode { get; }

        /// <summary>
        /// Initializes an immutable brokerage account position.
        /// </summary>
        /// <param name="symbol">LEAN symbol.</param>
        /// <param name="quantity">Signed position quantity.</param>
        /// <param name="averagePrice">Average position price.</param>
        /// <param name="modelCode">Brokerage model code.</param>
        public BrokerageAccountPosition(Symbol symbol, decimal quantity, decimal averagePrice, string modelCode = "")
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Quantity = quantity;
            AveragePrice = averagePrice;
            ModelCode = modelCode?.Trim() ?? string.Empty;
        }
    }
}
