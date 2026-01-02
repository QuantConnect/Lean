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

using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="IFeeModel"/> for Tradovate brokerage.
    /// Tradovate charges per-contract fees that vary by contract type.
    /// </summary>
    /// <remarks>
    /// Fee structure based on Tradovate pricing: https://www.tradovate.com/pricing/
    /// Fees include Tradovate commission + exchange fees + NFA fee.
    /// </remarks>
    public class TradovateFeeModel : FeeModel
    {
        /// <summary>
        /// Default fee per contract for futures not in the fee dictionary
        /// </summary>
        public const decimal DefaultFeePerContract = 1.29m;

        /// <summary>
        /// Fee per contract for micro futures
        /// </summary>
        public const decimal MicroFuturesFee = 0.79m;

        /// <summary>
        /// Fee per contract for E-mini futures
        /// </summary>
        public const decimal EminiFuturesFee = 1.29m;

        /// <summary>
        /// Fee per contract for standard futures
        /// </summary>
        public const decimal StandardFuturesFee = 1.79m;

        /// <summary>
        /// Per-contract fees by futures symbol
        /// </summary>
        private static readonly Dictionary<string, decimal> FuturesFees = new Dictionary<string, decimal>
        {
            // Micro E-mini Index Futures ($0.79/contract)
            { "MYM", MicroFuturesFee },  // Micro E-mini Dow
            { "MES", MicroFuturesFee },  // Micro E-mini S&P 500
            { "MNQ", MicroFuturesFee },  // Micro E-mini NASDAQ-100
            { "M2K", MicroFuturesFee },  // Micro E-mini Russell 2000

            // E-mini Index Futures ($1.29/contract)
            { "YM", EminiFuturesFee },   // E-mini Dow
            { "ES", EminiFuturesFee },   // E-mini S&P 500
            { "NQ", EminiFuturesFee },   // E-mini NASDAQ-100
            { "RTY", EminiFuturesFee },  // E-mini Russell 2000

            // Micro Treasury Futures ($0.79/contract)
            { "2YY", MicroFuturesFee },  // Micro 2-Year Treasury
            { "5YY", MicroFuturesFee },  // Micro 5-Year Treasury
            { "10Y", MicroFuturesFee },  // Micro 10-Year Treasury
            { "30Y", MicroFuturesFee },  // Micro 30-Year Treasury

            // Standard Treasury Futures ($1.79/contract)
            { "ZB", StandardFuturesFee },  // 30-Year T-Bond
            { "ZN", StandardFuturesFee },  // 10-Year T-Note
            { "ZF", StandardFuturesFee },  // 5-Year T-Note
            { "ZT", StandardFuturesFee },  // 2-Year T-Note

            // Micro Energy Futures ($0.79/contract)
            { "MCL", MicroFuturesFee },  // Micro Crude Oil

            // Standard Energy Futures ($1.79/contract)
            { "CL", StandardFuturesFee },  // Crude Oil WTI
            { "NG", StandardFuturesFee },  // Natural Gas

            // Micro Metals Futures ($0.79/contract)
            { "MGC", MicroFuturesFee },  // Micro Gold
            { "SIL", MicroFuturesFee },  // Micro Silver

            // Standard Metals Futures ($1.79/contract)
            { "GC", StandardFuturesFee },  // Gold
            { "SI", StandardFuturesFee },  // Silver

            // Agriculture Futures ($1.79/contract)
            { "ZC", StandardFuturesFee },  // Corn
            { "ZS", StandardFuturesFee },  // Soybeans
            { "ZW", StandardFuturesFee },  // Wheat
        };

        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency (USD)</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;
            var quantity = order.AbsoluteQuantity;

            // Get the root symbol for the futures contract
            var symbol = security.Symbol.ID.Symbol;

            // Look up the fee for this symbol, or use default
            if (!FuturesFees.TryGetValue(symbol, out var feePerContract))
            {
                feePerContract = DefaultFeePerContract;
            }

            var totalFee = quantity * feePerContract;

            return new OrderFee(new CashAmount(totalFee, Currencies.USD));
        }
    }
}
