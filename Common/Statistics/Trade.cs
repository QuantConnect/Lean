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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Represents a closed trade
    /// </summary>
    public class Trade
    {
        /// <summary>
        /// The symbol of the traded instrument
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The date and time the trade was opened
        /// </summary>
        public DateTime EntryTime { get; set; }

        /// <summary>
        /// The price at which the trade was opened (or the average price if multiple entries)
        /// </summary>
        public decimal EntryPrice { get; set; }

        /// <summary>
        /// The direction of the trade (Long or Short)
        /// </summary>
        public TradeDirection Direction { get; set; }

        /// <summary>
        /// The total unsigned quantity of the trade
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The date and time the trade was closed
        /// </summary>
        public DateTime ExitTime { get; set; }

        /// <summary>
        /// The price at which the trade was closed (or the average price if multiple exits)
        /// </summary>
        public decimal ExitPrice { get; set; }

        /// <summary>
        /// The gross profit/loss of the trade (as account currency)
        /// </summary>
        public decimal ProfitLoss { get; set; }

        /// <summary>
        /// The total fees associated with the trade (always positive value) (as account currency)
        /// </summary>
        public decimal TotalFees { get; set; }

        /// <summary>
        /// The Maximum Adverse Excursion (as account currency)
        /// </summary>
        public decimal MAE { get; set; }

        /// <summary>
        /// The Maximum Favorable Excursion (as account currency)
        /// </summary>
        public decimal MFE { get; set; }

        /// <summary>
        /// Returns the duration of the trade
        /// </summary>
        public TimeSpan Duration
        {
            get { return ExitTime - EntryTime; }
        }

        /// <summary>
        /// Returns the amount of profit given back before the trade was closed
        /// </summary>
        public decimal EndTradeDrawdown
        {
            get { return ProfitLoss - MFE; }
        }

        /// <summary>
        /// Returns whether the trade was profitable (is a win) or not (a loss)
        /// </summary>
        /// <returns>True if the trade was profitable</returns>
        /// <remarks>
        /// Even when a trade is not profitable, it may still be a win:
        ///     - For an ITM option buyer, an option assignment trade is not profitable (money was paid),
        ///       but it might count as a win if the ITM amount is greater than the amount paid for the option.
        ///     - For an ITM option seller, an option assignment trade is profitable (money was received),
        ///       but it might count as a loss if the ITM amount is less than the amount received for the option.
        /// </remarks>
        public bool IsWin { get; set; }

        /// <summary>
        /// The IDs of the orders related to this trade
        /// </summary>
        public HashSet<int> OrderIds { get; init; } = new HashSet<int>();
    }
}
