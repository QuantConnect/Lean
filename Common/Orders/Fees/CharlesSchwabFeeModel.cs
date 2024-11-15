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
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Charles Schwab.
    /// </summary>
    /// <see href="https://www.schwab.com/pricing"/>
    public class CharlesSchwabFeeModel : FeeModel
    {
        /// <summary>
        /// The exchange processing fee for standard option securities.
        /// </summary>
        private const decimal _optionExchangeProcFee = 0.01m;

        /// <summary>
        /// The exchange processing fee for index option securities.
        /// </summary>
        private const decimal _indexOptionExchangeProcFee = 0.35m;

        /// <summary>
        /// Represents the fee associated with equity options transactions (per contract).
        /// </summary>
        private const decimal _equityOptionFee = 0.65m;

        /// <summary>
        /// Calculates the order fee based on the security type and order parameters.
        /// </summary>
        /// <param name="parameters">The parameters for the order fee calculation, which include security and order details.</param>
        /// <returns>
        /// An <see cref="OrderFee"/> instance representing the calculated order fee.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameters"/> is <c>null</c>.
        /// </exception>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            switch (parameters.Security.Type)
            {
                case SecurityType.IndexOption:
                case SecurityType.Option:
                    var estimatedCommission = parameters.Order.AbsoluteQuantity * _equityOptionFee;
                    var exchangeProcFee = GetExchangeProcFeeBySecurityType(parameters.Security.Type) * parameters.Order.AbsoluteQuantity;
                    return new OrderFee(new CashAmount(estimatedCommission + exchangeProcFee, Currencies.USD));
                default:
                    return new OrderFee(new CashAmount(0m, Currencies.USD));
            }
        }

        /// <summary>
        /// Retrieves the exchange processing fee associated with a given security type.
        /// The Exchange Process Fee is charged by Schwab to offset fees imposed on us directly
        /// or indirectly by national securities exchanges, self-regulatory organizations, or
        /// U.S. option exchanges. Schwab may determine the amount of such fees in its reasonable
        /// discretion, which may differ from or exceed the actual third-party fees paid by Schwab.
        /// </summary>
        /// <param name="securityType">The type of security for which the exchange processing fee is requested.</param>
        /// <returns>The exchange processing fee for the specified security type.</returns>
        /// <remarks>
        /// <list type="bullet">
        ///     <item><see href="https://www.schwab.com/legal/schwab-pricing-guide-for-individual-investors"/></item>
        ///     <item><seealso href="https://www.schwab.com/pricing#bcn-table--table-content-74511"/></item>
        /// </list>
        /// </remarks>
        private decimal GetExchangeProcFeeBySecurityType(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Option:
                    return _optionExchangeProcFee;
                case SecurityType.Index:
                case SecurityType.IndexOption:
                    return _indexOptionExchangeProcFee;
                default:
                    Log.Trace($"{nameof(CharlesSchwabFeeModel)}.{GetExchangeProcFeeBySecurityType}: Returning 0 commission for unrecognized security type: {securityType}.");
                    return 0m;
            }
        }
    }
}
