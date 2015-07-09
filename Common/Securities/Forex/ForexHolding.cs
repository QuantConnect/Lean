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

using QuantConnect.Orders;

namespace QuantConnect.Securities.Forex 
{
    /// <summary>
    /// FOREX holdings implementation of the base securities class
    /// </summary>
    /// <seealso cref="SecurityHolding"/>
    public class ForexHolding : SecurityHolding 
    {
        private readonly Forex _forex;

        /// <summary>
        /// Forex Holding Class
        /// </summary>
        /// <param name="security">The forex security being held</param>
        public ForexHolding(Forex security)
            : base(security)
        {
            _forex = security;
        }

        /// <summary>
        /// Gets the conversion rate from the quote currency into the account currency
        /// </summary>
        public decimal ConversionRate
        {
            get { return _forex.QuoteCurrency.ConversionRate; }
        }

        /// <summary>
        /// Acquisition cost of the security total holdings.
        /// </summary>
        public override decimal HoldingsCost
        {
            // we need to add a conversion since the data is in terms of the quote currency
            get { return base.HoldingsCost*_forex.QuoteCurrency.ConversionRate; }
        }

        /// <summary>
        /// Market value of our holdings.
        /// </summary>
        public override decimal HoldingsValue
        {
            // we need to add a conversion since the data is in terms of the quote currency
            get { return base.HoldingsValue*_forex.QuoteCurrency.ConversionRate; }
        }

        /// <summary>
        /// Profit if we closed the holdings right now including the approximate fees.
        /// </summary>
        /// <remarks>Does not use the transaction model for market fills but should.</remarks>
        public override decimal TotalCloseProfit()
        {
            if (AbsoluteQuantity == 0)
            {
                return 0;
            }

            decimal orderFee = 0;

            if (AbsoluteQuantity > 0)
            {
                // this is in the account currency
                var marketOrder = new MarketOrder(_forex.Symbol, -Quantity, _forex.LocalTime.ConvertToUtc(_forex.Exchange.TimeZone), type:_forex.Type);
                orderFee = _forex.TransactionModel.GetOrderFee(_forex, marketOrder);
            }

            // we need to add a conversion since the data is in terms of the quote currency
            return (Price - AveragePrice)*Quantity*_forex.QuoteCurrency.ConversionRate - orderFee;
        }
    }
}