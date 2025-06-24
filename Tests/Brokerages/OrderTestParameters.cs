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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Helper class to abstract test cases from individual order types
    /// </summary>
    public abstract class OrderTestParameters
    {
        public Symbol Symbol { get; private set; }
        public SecurityType SecurityType { get; private set; }
        public IOrderProperties Properties { get; private set; }
        public OrderSubmissionData OrderSubmissionData { get; internal set; }
        public SymbolPropertiesDatabase SPDB { get; internal set; }

        protected OrderTestParameters(Symbol symbol, IOrderProperties properties = null, OrderSubmissionData orderSubmissionData = null)
        {
            Symbol = symbol;
            SecurityType = symbol.ID.SecurityType;
            Properties = properties;
            OrderSubmissionData = orderSubmissionData;
            SPDB = SymbolPropertiesDatabase.FromDataFolder();
        }

        public MarketOrder CreateLongMarketOrder(decimal quantity)
        {
            return new MarketOrder(Symbol, Math.Abs(quantity), DateTime.Now, properties: Properties)
            {
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }
        public MarketOrder CreateShortMarketOrder(decimal quantity)
        {
            return new MarketOrder(Symbol, -Math.Abs(quantity), DateTime.Now, properties: Properties)
            {
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        /// <summary>
        /// Creates a sell order of this type
        /// </summary>
        public abstract Order CreateShortOrder(decimal quantity);
        /// <summary>
        /// Creates a long order of this type
        /// </summary>
        public abstract Order CreateLongOrder(decimal quantity);
        /// <summary>
        /// Modifies the order so it is more likely to fill
        /// </summary>
        public abstract bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice);
        /// <summary>
        /// The status to expect when submitting this order, typically just Submitted,
        /// unless market order, then Filled
        /// </summary>
        public abstract OrderStatus ExpectedStatus { get; }
        /// <summary>
        /// The status to expect when cancelling this order
        /// </summary>
        public abstract bool ExpectedCancellationResult { get; }

        /// <summary>
        /// True to continue modifying the order until it is filled, false otherwise
        /// </summary>
        public virtual bool ModifyUntilFilled => true;

        protected SymbolProperties GetSymbolProperties(Symbol symbol)
        {
            return SPDB.GetSymbolProperties(symbol.ID.Market, symbol, SecurityType, Currencies.USD);
        }

        protected decimal RoundPrice(Order order, decimal price)
        {
            var roundOffPlaces = GetSymbolProperties(order.Symbol).MinimumPriceVariation.GetDecimalPlaces();
            return Math.Round(price / roundOffPlaces) * roundOffPlaces;
        }
    }
}
