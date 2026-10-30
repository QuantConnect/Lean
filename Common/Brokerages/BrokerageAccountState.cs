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
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable state for a single brokerage account.
    /// </summary>
    public class BrokerageAccountState
    {
        /// <summary>
        /// Gets the brokerage account identifier.
        /// </summary>
        public string AccountId { get; }

        /// <summary>
        /// Gets every account group currently containing this account.
        /// </summary>
        public IReadOnlyList<string> GroupNames { get; }

        /// <summary>
        /// Gets the brokerage-reported account type. May be empty when the brokerage omits the field.
        /// </summary>
        public string AccountType { get; }

        /// <summary>
        /// Gets the account net liquidation value, when available.
        /// </summary>
        public decimal? NetLiquidation { get; }

        /// <summary>
        /// Gets the account total cash value, when available.
        /// </summary>
        public decimal? TotalCashValue { get; }

        /// <summary>
        /// Gets the account available funds, when available.
        /// </summary>
        public decimal? AvailableFunds { get; }

        /// <summary>
        /// Gets the account excess liquidity, when available.
        /// </summary>
        public decimal? ExcessLiquidity { get; }

        /// <summary>
        /// Gets the account buying power, when available.
        /// </summary>
        public decimal? BuyingPower { get; }

        /// <summary>
        /// Gets the currency used for account valuation values.
        /// </summary>
        public string ValuationCurrency { get; }

        /// <summary>
        /// Gets the cash balance by currency.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> CashBalances { get; }

        /// <summary>
        /// Gets the positions held by the account.
        /// </summary>
        public IReadOnlyList<BrokerageAccountPosition> Positions { get; }

        /// <summary>
        /// Gets positions that the brokerage reported but LEAN could not map to a symbol.
        /// </summary>
        public IReadOnlyList<BrokerageAccountUnmappedPosition> UnmappedPositions { get; }

        /// <summary>
        /// Gets whether this account contains any unmapped positions.
        /// </summary>
        public bool HasUnmappedPositions => UnmappedPositions.Count != 0;

        /// <summary>
        /// Initializes immutable state for a brokerage account.
        /// </summary>
        /// <param name="accountId">Brokerage account identifier.</param>
        /// <param name="groupNames">Account groups currently containing the account.</param>
        /// <param name="accountType">Brokerage-reported account type; may be empty when omitted.</param>
        /// <param name="netLiquidation">Account net liquidation value.</param>
        /// <param name="totalCashValue">Account total cash value.</param>
        /// <param name="availableFunds">Account available funds.</param>
        /// <param name="excessLiquidity">Account excess liquidity.</param>
        /// <param name="buyingPower">Account buying power.</param>
        /// <param name="valuationCurrency">Currency used for account valuation values.</param>
        /// <param name="cashBalances">Cash balance by currency.</param>
        /// <param name="positions">Positions held by the account.</param>
        /// <param name="unmappedPositions">Positions that could not be mapped to a LEAN symbol.</param>
        public BrokerageAccountState(
            string accountId,
            IEnumerable<string> groupNames,
            string accountType,
            decimal? netLiquidation,
            decimal? totalCashValue,
            decimal? availableFunds,
            decimal? excessLiquidity,
            decimal? buyingPower,
            string valuationCurrency,
            IReadOnlyDictionary<string, decimal> cashBalances,
            IEnumerable<BrokerageAccountPosition> positions,
            IEnumerable<BrokerageAccountUnmappedPosition> unmappedPositions = null)
        {
            BrokerageAccountCollection.ValidateIdentifier(accountId, nameof(accountId));
            AccountId = accountId;
            GroupNames = BrokerageAccountCollection.CopyIdentifiers(
                groupNames,
                nameof(groupNames));
            AccountType = accountType?.Trim() ?? string.Empty;
            NetLiquidation = netLiquidation;
            TotalCashValue = totalCashValue;
            AvailableFunds = availableFunds;
            ExcessLiquidity = excessLiquidity;
            BuyingPower = buyingPower;
            ValuationCurrency = valuationCurrency?.Trim() ?? string.Empty;
            CashBalances = BrokerageAccountCollection.CopyDictionary(
                cashBalances,
                nameof(cashBalances),
                comparer: StringComparer.OrdinalIgnoreCase);
            var mappedPositions = (positions ?? Enumerable.Empty<BrokerageAccountPosition>()).ToArray();
            if (mappedPositions.Any(position => position == null))
            {
                throw new ArgumentException("Brokerage account positions cannot contain null values.", nameof(positions));
            }
            var unmapped = (unmappedPositions ?? Enumerable.Empty<BrokerageAccountUnmappedPosition>()).ToArray();
            if (unmapped.Any(position => position == null))
            {
                throw new ArgumentException(
                    "Unmapped brokerage account positions cannot contain null values.",
                    nameof(unmappedPositions));
            }
            Positions = Array.AsReadOnly(mappedPositions);
            UnmappedPositions = Array.AsReadOnly(unmapped);
        }
    }
}
