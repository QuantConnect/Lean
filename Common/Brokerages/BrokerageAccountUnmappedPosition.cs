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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable brokerage position for a contract that could not be mapped to a LEAN symbol.
    /// </summary>
    public sealed class BrokerageAccountUnmappedPosition
    {
        /// <summary>
        /// Gets the brokerage contract identifier.
        /// </summary>
        public string BrokerageContractId { get; }

        /// <summary>
        /// Gets the brokerage symbol.
        /// </summary>
        public string BrokerageSymbol { get; }

        /// <summary>
        /// Gets the brokerage local symbol.
        /// </summary>
        public string LocalSymbol { get; }

        /// <summary>
        /// Gets the brokerage security type.
        /// </summary>
        public string BrokerageSecurityType { get; }

        /// <summary>
        /// Gets the contract currency.
        /// </summary>
        public string Currency { get; }

        /// <summary>
        /// Gets the contract exchange.
        /// </summary>
        public string Exchange { get; }

        /// <summary>
        /// Gets the contract primary exchange.
        /// </summary>
        public string PrimaryExchange { get; }

        /// <summary>
        /// Gets the brokerage trading class.
        /// </summary>
        public string TradingClass { get; }

        /// <summary>
        /// Gets the contract expiration.
        /// </summary>
        public string Expiration { get; }

        /// <summary>
        /// Gets the contract strike.
        /// </summary>
        public decimal Strike { get; }

        /// <summary>
        /// Gets the brokerage strike representation before decimal conversion.
        /// </summary>
        public string BrokerageStrike { get; }

        /// <summary>
        /// Gets the contract right.
        /// </summary>
        public string Right { get; }

        /// <summary>
        /// Gets the contract multiplier.
        /// </summary>
        public string Multiplier { get; }

        /// <summary>
        /// Gets the signed position quantity.
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Gets the average position price.
        /// </summary>
        public decimal AveragePrice { get; }

        /// <summary>
        /// Gets the brokerage average-cost representation before normalization.
        /// </summary>
        public string BrokerageAverageCost { get; }

        /// <summary>
        /// Gets the brokerage model code, when available.
        /// </summary>
        public string ModelCode { get; }

        /// <summary>
        /// Gets the symbol-mapping error.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes an immutable unmapped brokerage account position.
        /// </summary>
        /// <param name="brokerageContractId">Brokerage contract identifier.</param>
        /// <param name="brokerageSymbol">Brokerage symbol.</param>
        /// <param name="localSymbol">Brokerage local symbol.</param>
        /// <param name="brokerageSecurityType">Brokerage security type.</param>
        /// <param name="currency">Contract currency.</param>
        /// <param name="exchange">Contract exchange.</param>
        /// <param name="primaryExchange">Contract primary exchange.</param>
        /// <param name="tradingClass">Brokerage trading class.</param>
        /// <param name="expiration">Contract expiration.</param>
        /// <param name="strike">Contract strike.</param>
        /// <param name="right">Contract right.</param>
        /// <param name="multiplier">Contract multiplier.</param>
        /// <param name="quantity">Signed position quantity.</param>
        /// <param name="averagePrice">Average position price.</param>
        /// <param name="modelCode">Brokerage model code.</param>
        /// <param name="errorMessage">Symbol-mapping error.</param>
        /// <param name="brokerageStrike">Raw brokerage strike representation.</param>
        /// <param name="brokerageAverageCost">Raw brokerage average-cost representation.</param>
        public BrokerageAccountUnmappedPosition(
            string brokerageContractId,
            string brokerageSymbol,
            string localSymbol,
            string brokerageSecurityType,
            string currency,
            string exchange,
            string primaryExchange,
            string tradingClass,
            string expiration,
            decimal strike,
            string right,
            string multiplier,
            decimal quantity,
            decimal averagePrice,
            string modelCode = "",
            string errorMessage = "",
            string brokerageStrike = "",
            string brokerageAverageCost = "")
        {
            BrokerageContractId = brokerageContractId?.Trim() ?? string.Empty;
            BrokerageSymbol = brokerageSymbol?.Trim() ?? string.Empty;
            LocalSymbol = localSymbol?.Trim() ?? string.Empty;
            BrokerageSecurityType = brokerageSecurityType?.Trim() ?? string.Empty;
            Currency = currency?.Trim() ?? string.Empty;
            Exchange = exchange?.Trim() ?? string.Empty;
            PrimaryExchange = primaryExchange?.Trim() ?? string.Empty;
            TradingClass = tradingClass?.Trim() ?? string.Empty;
            Expiration = expiration?.Trim() ?? string.Empty;
            Strike = strike;
            BrokerageStrike = brokerageStrike?.Trim() ?? string.Empty;
            Right = right?.Trim() ?? string.Empty;
            Multiplier = multiplier?.Trim() ?? string.Empty;
            Quantity = quantity;
            AveragePrice = averagePrice;
            BrokerageAverageCost = brokerageAverageCost?.Trim() ?? string.Empty;
            ModelCode = modelCode?.Trim() ?? string.Empty;
            ErrorMessage = errorMessage?.Trim() ?? string.Empty;
        }
    }
}
