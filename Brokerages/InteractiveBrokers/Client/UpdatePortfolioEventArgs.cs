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
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.UpdatePortfolio"/> event
    /// </summary>
    public class UpdatePortfolioEventArgs : EventArgs
    {
        /// <summary>
        /// This structure contains a description of the contract which is being traded.
        /// The exchange field in a contract is not set for portfolio update.
        /// </summary>
        public Contract Contract { get; private set; }

        /// <summary>
        /// The number of positions held.
        /// If the position is 0, it means the position has just cleared.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// The unit price of the instrument.
        /// </summary>
        public double MarketPrice { get; private set; }

        /// <summary>
        /// The total market value of the instrument.
        /// </summary>
        public double MarketValue { get; private set; }

        /// <summary>
        /// The average cost per share is calculated by dividing your cost (execution price + commission) by the quantity of your position.
        /// </summary>
        public double AverageCost { get; private set; }

        /// <summary>
        /// The difference between the current market value of your open positions and the average cost, or Value - Average Cost.
        /// </summary>
        public double UnrealisedPnl { get; private set; }

        /// <summary>
        /// Shows your profit on closed positions, which is the difference between your entry execution cost (execution price + commissions to open the position) and exit execution cost ((execution price + commissions to close the position)
        /// </summary>
        public double RealisedPnl { get; private set; }

        /// <summary>
        /// The name of the account to which the message applies.  Useful for Financial Advisor sub-account messages.
        /// </summary>
        public string AccountName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePortfolioEventArgs"/> class
        /// </summary>
        public UpdatePortfolioEventArgs(Contract contract, int position, double marketPrice, double marketValue, double averageCost, double unrealisedPnl, double realisedPnl, string accountName)
        {
            Contract = contract;
            Position = position;
            MarketPrice = marketPrice;
            MarketValue = marketValue;
            AverageCost = averageCost;
            UnrealisedPnl = unrealisedPnl;
            RealisedPnl = realisedPnl;
            AccountName = accountName;
        }
    }
}