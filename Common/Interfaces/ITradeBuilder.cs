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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Generates trades from executions and market price updates
    /// </summary>
    public interface ITradeBuilder
    {
        /// <summary>
        /// Sets the security manager instance
        /// </summary>
        /// <param name="securities">The security manager</param>
        void SetSecurityManager(SecurityManager securities);

        /// <summary>
        /// Sets the live mode flag
        /// </summary>
        /// <param name="live">The live mode flag</param>
        void SetLiveMode(bool live);

        /// <summary>
        /// The list of closed trades
        /// </summary>
        List<Trade> ClosedTrades { get; }

        /// <summary>
        /// Returns true if there is an open position for the symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>true if there is an open position for the symbol</returns>
        bool HasOpenPosition(Symbol symbol);

        /// <summary>
        /// Sets the current market price for the symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="price"></param>
        void SetMarketPrice(Symbol symbol, decimal price);

        /// <summary>
        /// Applies a split to the trade builder
        /// </summary>
        /// <param name="split">The split to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="dataNormalizationMode">The <see cref="DataNormalizationMode"/> for this security</param>
        void ApplySplit(Split split, bool liveMode, DataNormalizationMode dataNormalizationMode);

        /// <summary>
        /// Processes a new fill, eventually creating new trades
        /// </summary>
        /// <param name="fill">The new fill order event</param>
        /// <param name="securityConversionRate">The current security market conversion rate into the account currency</param>
        /// <param name="feeInAccountCurrency">The current order fee in the account currency</param>
        /// <param name="multiplier">The contract multiplier</param>
        void ProcessFill(OrderEvent fill,
            decimal securityConversionRate,
            decimal feeInAccountCurrency,
            decimal multiplier = 1.0m);
    }
}
