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
    /// The <see cref="AlgorithmPerformance"/> class is a wrapper for <see cref="TradeStatistics"/> and <see cref="PortfolioStatistics"/>
    /// </summary>
    public class AlgorithmPerformance
    {
        /// <summary>
        /// The algorithm statistics on closed trades
        /// </summary>
        public TradeStatistics TradeStatistics { get; set; }

        /// <summary>
        /// The algorithm statistics on portfolio
        /// </summary>
        public PortfolioStatistics PortfolioStatistics { get; set; }

        /// <summary>
        /// The list of closed trades
        /// </summary>
        public List<Trade> ClosedTrades { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmPerformance"/> class
        /// </summary>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="equity">The list of daily equity values</param>
        /// <param name="listPerformance">The list of algorithm performance values</param>
        /// <param name="listBenchmark">The list of benchmark values</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        public AlgorithmPerformance(
            List<Trade> trades,
            SortedDictionary<DateTime, decimal> profitLoss,
            SortedDictionary<DateTime, decimal> equity,
            List<double> listPerformance,
            List<double> listBenchmark, 
            decimal startingCapital)
        {
            TradeStatistics = new TradeStatistics(trades);
            PortfolioStatistics = new PortfolioStatistics(profitLoss, equity, listPerformance, listBenchmark, startingCapital);
            ClosedTrades = trades;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmPerformance"/> class
        /// </summary>
        public AlgorithmPerformance()
        {
            TradeStatistics = new TradeStatistics();
            PortfolioStatistics = new PortfolioStatistics();
            ClosedTrades = new List<Trade>();
        }

    }
}
