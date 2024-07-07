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
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the default implementation of <see cref="IFeeModel"/> Refer to https://www.samco.in/technology/brokerage_calculator
    /// </summary>
    public class IndiaFeeModel : IFeeModel
    {
        /// <summary>
        /// Brokerage calculation Factor
        /// </summary>
        protected virtual decimal BrokerageMultiplier { get; set; }

        /// <summary>
        /// Maximum brokerage per order
        /// </summary>
        protected virtual decimal MaxBrokerage { get; set; }

        /// <summary>
        /// Securities Transaction Tax calculation Factor
        /// </summary>
        protected virtual decimal SecuritiesTransactionTaxTotalMultiplier { get; set; }

        /// <summary>
        /// Exchange Transaction Charge calculation Factor
        /// </summary>
        protected virtual decimal ExchangeTransactionChargeMultiplier { get; set; }

        /// <summary>
        /// State Tax calculation Factor
        /// </summary>
        protected virtual decimal StateTaxMultiplier { get; set; }

        /// <summary>
        /// Sebi Charges calculation Factor
        /// </summary>
        protected virtual decimal SebiChargesMultiplier { get; set; }

        /// <summary>
        /// Stamp Charges calculation Factor
        /// </summary>
        protected virtual decimal StampChargesMultiplier { get; set; }

        /// <summary>
        /// Checks if Stamp Charges is calculated from order valur or turnover
        /// </summary>
        protected virtual bool IsStampChargesFromOrderValue { get; set; }

        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">
        /// A <see cref="OrderFeeParameters"/> object containing the security and order
        /// </param>
        public OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters.Security == null)
            {
                return OrderFee.Zero;
            }
            var orderValue = parameters.Order.GetValue(parameters.Security);

            var fee = GetFee(orderValue);
            return new OrderFee(new CashAmount(fee, Currencies.INR));
        }

        private decimal GetFee(decimal orderValue)
        {
            bool isSell = orderValue < 0;
            orderValue = Math.Abs(orderValue);
            var multiplied = orderValue * BrokerageMultiplier;
            var brokerage = (multiplied > MaxBrokerage) ? MaxBrokerage : Math.Round(multiplied, 2);

            var turnover = Math.Round(orderValue, 2);

            decimal securitiesTransactionTaxTotal = 0;
            if (isSell)
            {
                securitiesTransactionTaxTotal = Math.Round(
                    orderValue * SecuritiesTransactionTaxTotalMultiplier,
                    2
                );
            }

            var exchangeTransactionCharge = Math.Round(
                turnover * ExchangeTransactionChargeMultiplier,
                2
            );
            var clearingCharge = 0;

            var stateTax = Math.Round(
                StateTaxMultiplier * (brokerage + exchangeTransactionCharge),
                2
            );

            var sebiCharges = Math.Round((turnover * SebiChargesMultiplier), 2);
            decimal stampCharges = 0;
            if (!isSell)
            {
                if (IsStampChargesFromOrderValue)
                {
                    stampCharges = Math.Round((orderValue * StampChargesMultiplier), 2);
                }
                else
                {
                    stampCharges = Math.Round((turnover * StampChargesMultiplier), 2);
                }
            }

            var totalTax = Math.Round(
                brokerage
                    + securitiesTransactionTaxTotal
                    + exchangeTransactionCharge
                    + stampCharges
                    + clearingCharge
                    + stateTax
                    + sebiCharges,
                2
            );

            return totalTax;
        }
    }
}
