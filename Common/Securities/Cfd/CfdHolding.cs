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

namespace QuantConnect.Securities.Cfd
{
    /// <summary>
    /// CFD holdings implementation of the base securities class
    /// </summary>
    /// <seealso cref="SecurityHolding"/>
    public class CfdHolding : SecurityHolding
    {
        private readonly Cfd _cfd;

        /// <summary>
        /// CFD Holding Class constructor
        /// </summary>
        /// <param name="security">The CFD security being held</param>
        public CfdHolding(Cfd security)
            : base(security)
        {
            _cfd = security;
        }

        /// <summary>
        /// Gets the conversion rate from the quote currency into the account currency
        /// </summary>
        public decimal ConversionRate
        {
            get { return _cfd.QuoteCurrency.ConversionRate; }
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
                var marketOrder = new MarketOrder(_cfd.Symbol, -Quantity, _cfd.LocalTime.ConvertToUtc(_cfd.Exchange.TimeZone));
                orderFee = _cfd.FeeModel.GetOrderFee(_cfd, marketOrder);
            }

            // we need to add a conversion since the data is in terms of the quote currency
            return (Price - AveragePrice) * Quantity * _cfd.ContractMultiplier * _cfd.QuoteCurrency.ConversionRate - orderFee;
        }
    }
}
