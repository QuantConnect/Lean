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
using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models GDAX order fees
    /// </summary>
    public class GDAXFeeModel : FeeModel
    {
        /// <summary>
        /// Tier 1 taker fees
        /// https://pro.coinbase.com/orders/fees
        /// https://blog.coinbase.com/coinbase-pro-market-structure-update-fbd9d49f43d7
        /// </summary>
        private static readonly List<FeeHistoryEntry> FeeHistory = new List<FeeHistoryEntry>
        {
            new FeeHistoryEntry(DateTime.MinValue, 0m, 0.003m),
            new FeeHistoryEntry(new DateTime(2019, 3, 23, 1, 30, 0), 0.0015m, 0.0025m)
        };

        /// <summary>
        /// Get the fee for this order in quote currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in quote currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            // marketable limit orders are considered takers
            var isMaker = order.Type == OrderType.Limit && !order.IsMarketable;

            var feePercentage = GetFeePercentage(order.Time, isMaker);

            // get order value in quote currency, then apply maker/taker fee factor
            var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
            unitPrice *= security.SymbolProperties.ContractMultiplier;

            // currently we do not model 30-day volume, so we use the first tier

            var fee = unitPrice * order.AbsoluteQuantity * feePercentage;

            return new OrderFee(new CashAmount(fee, security.QuoteCurrency.Symbol));
        }

        /// <summary>
        /// Returns the maker/taker fee percentage effective at the requested date.
        /// </summary>
        /// <param name="utcTime">The date/time requested (UTC)</param>
        /// <param name="isMaker">true if the maker percentage fee is requested, false otherwise</param>
        /// <returns>The fee percentage effective at the requested date</returns>
        public static decimal GetFeePercentage(DateTime utcTime, bool isMaker)
        {
            for (var index = FeeHistory.Count - 1; index >= 0; index--)
            {
                var entry = FeeHistory[index];
                if (utcTime >= entry.EffectiveDateTime)
                {
                    return isMaker ? entry.MakerFeePercentage : entry.TakerFeePercentage;
                }
            }

            return 0m;
        }

        /// <summary>
        /// Represents an entry in the list of historical fee changes.
        /// </summary>
        public class FeeHistoryEntry
        {
            /// <summary>
            /// The date/time (UTC) when the new fees go into effect.
            /// </summary>
            public DateTime EffectiveDateTime { get; set; }

            /// <summary>
            /// The maker fee percentage.
            /// </summary>
            public decimal MakerFeePercentage { get; set; }

            /// <summary>
            /// The taker fee percentage.
            /// </summary>
            public decimal TakerFeePercentage { get; set; }

            /// <summary>
            /// Creates an instance of the <see cref="FeeHistoryEntry"/> class.
            /// </summary>
            /// <param name="effectiveDateTime">The date/time (UTC) when the new fees go into effect.</param>
            /// <param name="makerFeePercentage">The maker fee percentage.</param>
            /// <param name="takerFeePercentage">The taker fee percentage.</param>
            public FeeHistoryEntry(DateTime effectiveDateTime, decimal makerFeePercentage, decimal takerFeePercentage)
            {
                EffectiveDateTime = effectiveDateTime;
                MakerFeePercentage = makerFeePercentage;
                TakerFeePercentage = takerFeePercentage;
            }
        }
    }
}
