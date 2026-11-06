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
 *
*/

using System;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Trading 212.
    /// </summary>
    /// <remarks>
    /// Trading 212 is commission free, so the only dealing fee is the FX fee of 0.15% charged on
    /// the converted notional when the instrument is priced in a currency other than the account
    /// currency. It is charged on both buys and sells. Sales of US-listed equities also carry the
    /// regulatory fees Trading 212 passes through: the SEC Section 31 fee and the FINRA trading
    /// activity fee.
    /// Trading 212 converts every trade to the account currency, so the fee is charged in the account
    /// currency. The model reports it in that currency too, which is also the currency Lean deducts it
    /// from, so the reported fee and the cash movement always agree.
    /// The account currency is the destination of the security's quote cash conversion, which the cash
    /// book sets to the algorithm's account currency. It is read at fee time, because the fee model is
    /// assigned while the security is created, before the cash book has set up conversions. Until then
    /// there is no destination to convert into, so the fee is charged in the quote currency with no FX fee.
    /// Lean invokes the fee model once, at the completed fill, against the order quantity in effect
    /// then. The fees are therefore computed on the full order rather than once per partial fill, and
    /// an amendment made before the fill is reflected without triggering a fee of its own. An order
    /// that fills partially and is then cancelled is charged no fee on the filled part; that is
    /// existing backtesting behaviour shared by every fee model, not specific to Trading 212.
    /// </remarks>
    public class Trading212FeeModel : FeeModel
    {
        /// <summary>
        /// The FX fee charged on the converted notional when the instrument currency differs
        /// from the account currency: 0.15%.
        /// </summary>
        private const decimal _fxFeeRate = 0.0015m;

        /// <summary>
        /// The SEC Section 31 fee charged on the value of a US-listed equity sale: 0.206 basis points
        /// ($20.60 per million dollars of sale value), effective April 4, 2026.
        /// </summary>
        /// <remarks>
        /// The SEC resets the rate each fiscal year, so this is only valid until the next adjustment.
        /// See the Section 31 transaction fee rate advisory for fiscal year 2026:
        /// https://www.sec.gov/rules-regulations/fee-rate-advisories/2026-2
        /// </remarks>
        private const decimal _secSection31FeeRate = 0.0000206m;

        /// <summary>
        /// The FINRA trading activity fee charged per share on a US-listed equity sale: $0.000195,
        /// effective January 1, 2026.
        /// </summary>
        /// <remarks>
        /// FINRA resets the rate and the per-trade maximum on a published schedule. See the FINRA fee
        /// adjustment schedule:
        /// https://www.finra.org/rules-guidance/rule-filings/sr-finra-2024-019/fee-adjustment-schedule
        /// </remarks>
        private const decimal _finraTafPerShare = 0.000195m;

        /// <summary>
        /// The maximum FINRA trading activity fee charged on a single US-listed equity sale: $9.79 per
        /// trade, effective January 1, 2026.
        /// </summary>
        /// <remarks>
        /// See the FINRA fee adjustment schedule:
        /// https://www.finra.org/rules-guidance/rule-filings/sr-finra-2024-019/fee-adjustment-schedule
        /// </remarks>
        private const decimal _finraTafMaximumPerTrade = 9.79m;

        /// <summary>
        /// Gets the order fee associated with the specified order, in the account currency.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object containing the
        /// security and order</param>
        /// <returns>The cost of the order in the account currency, or in the quote currency when the
        /// account currency is not set up yet</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            if (security.Type != SecurityType.Equity)
            {
                throw new ArgumentException(Messages.FeeModel.UnsupportedSecurityType(security));
            }

            var accountCurrency = security.QuoteCurrency.CurrencyConversion?.DestinationCurrency;

            // Order.GetValue returns the value in the account currency, but the fees are charged on
            // the trade value in the instrument's quote currency, so convert it back. A zero rate
            // means the conversion rate is not available yet.
            var conversionRate = security.QuoteCurrency.ConversionRate;
            var tradeValue = conversionRate == 0 ? 0m : Math.Abs(order.GetValue(security)) / conversionRate;
            var fee = 0m;

            // Trading 212 converts the notional to the account currency, so the FX fee applies
            // whenever the instrument is priced in another currency, on buys and sells alike.
            // Without an account currency there is nothing to convert into, so no FX fee applies.
            if (accountCurrency != null && security.QuoteCurrency.Symbol != accountCurrency)
            {
                fee += _fxFeeRate * tradeValue;
            }

            // The US regulatory fees are passed through on sales of US-listed equities.
            if (order.Direction == OrderDirection.Sell && security.Symbol.ID.Market == Market.USA)
            {
                fee += _secSection31FeeRate * tradeValue;
                // FINRA caps the trading activity fee at a maximum charge per trade.
                fee += Math.Min(_finraTafPerShare * order.AbsoluteQuantity, _finraTafMaximumPerTrade);
            }

            if (accountCurrency == null)
            {
                return new OrderFee(new CashAmount(fee, security.QuoteCurrency.Symbol));
            }

            // The fee was computed in the instrument's quote currency; convert it to the account currency,
            // which is the currency Trading 212 charges it in.
            return new OrderFee(new CashAmount(fee * conversionRate, accountCurrency));
        }
    }
}
