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
    /// The AlgorithmPerformance class calculates a set of statistics from a list of closed trades
    /// </summary>
    public class AlgorithmPerformance
    {
        /// <summary>
        /// The total number of trades
        /// </summary>
        public int TotalNumberOfTrades { get; private set; }

        /// <summary>
        /// The total number of winning trades
        /// </summary>
        public int NumberOfWinningTrades { get; private set; }

        /// <summary>
        /// The total number of losing trades
        /// </summary>
        public int NumberOfLosingTrades { get; private set; }

        /// <summary>
        /// The total profit/loss for all trades (as symbol currency)
        /// </summary>
        public decimal TotalProfitLoss { get; private set; }

        /// <summary>
        /// The total profit for all winning trades (as symbol currency)
        /// </summary>
        public decimal TotalProfit { get; private set; }

        /// <summary>
        /// The total loss for all losing trades (as symbol currency)
        /// </summary>
        public decimal TotalLoss { get; private set; }

        /// <summary>
        /// The largest profit in a single trade (as symbol currency)
        /// </summary>
        public decimal LargestProfit { get; private set; }

        /// <summary>
        /// The largest loss in a single trade (as symbol currency)
        /// </summary>
        public decimal LargestLoss { get; private set; }

        /// <summary>
        /// The average profit/loss (a.k.a. Expectancy or Average Trade) for all trades (as symbol currency)
        /// </summary>
        public decimal AverageProfitLoss { get; private set; }

        /// <summary>
        /// The average profit for all winning trades (as symbol currency)
        /// </summary>
        public decimal AverageProfit { get; private set; }

        /// <summary>
        /// The average loss for all winning trades (as symbol currency)
        /// </summary>
        public decimal AverageLoss { get; private set; }

        /// <summary>
        /// The average duration for all trades
        /// </summary>
        public TimeSpan AverageTradeDuration { get; private set; }

        /// <summary>
        /// The average duration for all winning trades
        /// </summary>
        public TimeSpan AverageWinningTradeDuration { get; private set; }

        /// <summary>
        /// The average duration for all losing trades
        /// </summary>
        public TimeSpan AverageLosingTradeDuration { get; private set; }

        /// <summary>
        /// The maximum number of consecutive winning trades
        /// </summary>
        public int MaxConsecutiveWinningTrades { get; private set; }

        /// <summary>
        /// The maximum number of consecutive losing trades
        /// </summary>
        public int MaxConsecutiveLosingTrades { get; private set; }

        /// <summary>
        /// The ratio of the average profit to the average loss
        /// </summary>
        /// <remarks>If the average loss is zero, ProfitLossRatio is set to -1</remarks>
        public decimal ProfitLossRatio { get; private set; }

        /// <summary>
        /// The ratio of the number of winning trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, WinRate is set to zero</remarks>
        public decimal WinRate { get; private set; }

        /// <summary>
        /// The ratio of the number of losing trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, LossRate is set to zero</remarks>
        public decimal LossRate { get; private set; }

        /// <summary>
        /// The average Maximum Adverse Excursion for all trades
        /// </summary>
        public decimal AverageMAE { get; private set; }

        /// <summary>
        /// The average Maximum Favorable Excursion for all trades
        /// </summary>
        public decimal AverageMFE { get; private set; }

        /// <summary>
        /// The largest Maximum Adverse Excursion in a single trade (as symbol currency)
        /// </summary>
        public decimal LargestMAE { get; private set; }

        /// <summary>
        /// The largest Maximum Favorable Excursion in a single trade (as symbol currency)
        /// </summary>
        public decimal LargestMFE { get; private set; }

        /// <summary>
        /// The maximum closed-trade drawdown for all trades (as symbol currency)
        /// </summary>
        /// <remarks>The calculation only takes into account the profit/loss of each trade</remarks>
        public decimal MaximumClosedTradeDrawdown { get; private set; }

        /// <summary>
        /// The maximum intra-trade drawdown for all trades (as symbol currency)
        /// </summary>
        /// <remarks>The calculation takes into account MAE and MFE of each trade</remarks>
        public decimal MaximumIntraTradeDrawdown { get; private set; }

        /// <summary>
        /// The standard deviation of the profits/losses for all trades (as symbol currency)
        /// </summary>
        public decimal ProfitLossStandardDeviation{ get; private set; }


        /// <summary>
        /// Initializes a new instance of the AlgorithmPerformance class
        /// </summary>
        /// <param name="trades">The list of closed trades</param>
        public AlgorithmPerformance(IEnumerable<Trade> trades)
        {
            var maxConsecutiveWinners = 0;
            var maxConsecutiveLosers = 0;
            var maxTotalProfitLoss = 0m;
            var maxTotalProfitLossWithMfe = 0m;
            var sumForVariance = 0m;

            foreach (var trade in trades)
            {
                TotalNumberOfTrades++;

                if (TotalProfitLoss + trade.MFE > maxTotalProfitLossWithMfe)
                    maxTotalProfitLossWithMfe = TotalProfitLoss + trade.MFE;

                if (TotalProfitLoss + trade.MAE - maxTotalProfitLossWithMfe < MaximumIntraTradeDrawdown)
                    MaximumIntraTradeDrawdown = TotalProfitLoss + trade.MAE - maxTotalProfitLossWithMfe;

                if (trade.ProfitLoss > 0)
                {
                    // winning trade
                    NumberOfWinningTrades++;

                    TotalProfitLoss += trade.ProfitLoss;
                    TotalProfit += trade.ProfitLoss;
                    AverageProfit += (trade.ProfitLoss - AverageProfit) / NumberOfWinningTrades;
                    AverageWinningTradeDuration += TimeSpan.FromSeconds((trade.Duration.TotalSeconds - AverageWinningTradeDuration.TotalSeconds) / NumberOfWinningTrades);

                    if (trade.ProfitLoss > LargestProfit) 
                        LargestProfit = trade.ProfitLoss;

                    maxConsecutiveWinners++;
                    maxConsecutiveLosers = 0;
                    if (maxConsecutiveWinners > MaxConsecutiveWinningTrades)
                        MaxConsecutiveWinningTrades = maxConsecutiveWinners;

                    if (TotalProfitLoss > maxTotalProfitLoss)
                        maxTotalProfitLoss = TotalProfitLoss;
                }
                else
                {
                    // losing trade
                    NumberOfLosingTrades++;

                    TotalProfitLoss += trade.ProfitLoss;
                    TotalLoss += trade.ProfitLoss;
                    AverageLoss += (trade.ProfitLoss - AverageLoss) / NumberOfLosingTrades;
                    AverageLosingTradeDuration += TimeSpan.FromSeconds((trade.Duration.TotalSeconds - AverageLosingTradeDuration.TotalSeconds) / NumberOfLosingTrades);

                    if (trade.ProfitLoss < LargestLoss)
                        LargestLoss = trade.ProfitLoss;

                    maxConsecutiveWinners = 0;
                    maxConsecutiveLosers++;
                    if (maxConsecutiveLosers > MaxConsecutiveLosingTrades)
                        MaxConsecutiveLosingTrades = maxConsecutiveLosers;

                    if (TotalProfitLoss - maxTotalProfitLoss < MaximumClosedTradeDrawdown)
                        MaximumClosedTradeDrawdown = TotalProfitLoss - maxTotalProfitLoss;
                }

                var prevAverageProfitLoss = AverageProfitLoss;
                AverageProfitLoss += (trade.ProfitLoss - AverageProfitLoss) / TotalNumberOfTrades;
                sumForVariance += (trade.ProfitLoss - prevAverageProfitLoss) * (trade.ProfitLoss - AverageProfitLoss);
                var variance = TotalNumberOfTrades > 1 ? sumForVariance / (TotalNumberOfTrades - 1) : 0;
                ProfitLossStandardDeviation = (decimal)Math.Sqrt((double)variance);

                AverageTradeDuration += TimeSpan.FromSeconds((trade.Duration.TotalSeconds - AverageTradeDuration.TotalSeconds) / TotalNumberOfTrades);
                AverageMAE += (trade.MAE - AverageMAE) / TotalNumberOfTrades;
                AverageMFE += (trade.MFE - AverageMFE) / TotalNumberOfTrades;

                if (trade.MAE < LargestMAE) 
                    LargestMAE = trade.MAE;

                if (trade.MFE > LargestMFE) 
                    LargestMFE = trade.MFE;
            }

            ProfitLossRatio = AverageLoss < 0 ? AverageProfit / Math.Abs(AverageLoss) : -1;
            WinRate = TotalNumberOfTrades > 0 ? (decimal)NumberOfWinningTrades / TotalNumberOfTrades : 0;
            LossRate = TotalNumberOfTrades > 0 ? 1 - WinRate : 0;
        }
    }
}
