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
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// The <see cref="TradeStatistics"/> class represents a set of statistics calculated from a list of closed trades
    /// </summary>
    public class TradeStatistics
    {
        /// <summary>
        /// The entry date/time of the first trade
        /// </summary>
        public DateTime? StartDateTime { get; set; }

        /// <summary>
        /// The exit date/time of the last trade
        /// </summary>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// The total number of trades
        /// </summary>
        public int TotalNumberOfTrades { get; set; }

        /// <summary>
        /// The total number of winning trades
        /// </summary>
        public int NumberOfWinningTrades { get; set; }

        /// <summary>
        /// The total number of losing trades
        /// </summary>
        public int NumberOfLosingTrades { get; set; }

        /// <summary>
        /// The total profit/loss for all trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TotalProfitLoss { get; set; }

        /// <summary>
        /// The total profit for all winning trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TotalProfit { get; set; }

        /// <summary>
        /// The total loss for all losing trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TotalLoss { get; set; }

        /// <summary>
        /// The largest profit in a single trade (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal LargestProfit { get; set; }

        /// <summary>
        /// The largest loss in a single trade (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal LargestLoss { get; set; }

        /// <summary>
        /// The average profit/loss (a.k.a. Expectancy or Average Trade) for all trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageProfitLoss { get; set; }

        /// <summary>
        /// The average profit for all winning trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageProfit { get; set; }

        /// <summary>
        /// The average loss for all winning trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageLoss { get; set; }

        /// <summary>
        /// The average duration for all trades
        /// </summary>
        public TimeSpan AverageTradeDuration { get; set; }

        /// <summary>
        /// The average duration for all winning trades
        /// </summary>
        public TimeSpan AverageWinningTradeDuration { get; set; }

        /// <summary>
        /// The average duration for all losing trades
        /// </summary>
        public TimeSpan AverageLosingTradeDuration { get; set; }

        /// <summary>
        /// The median duration for all trades
        /// </summary>
        public TimeSpan MedianTradeDuration { get; set; }

        /// <summary>
        /// The median duration for all winning trades
        /// </summary>
        public TimeSpan MedianWinningTradeDuration { get; set; }

        /// <summary>
        /// The median duration for all losing trades
        /// </summary>
        public TimeSpan MedianLosingTradeDuration { get; set; }

        /// <summary>
        /// The maximum number of consecutive winning trades
        /// </summary>
        public int MaxConsecutiveWinningTrades { get; set; }

        /// <summary>
        /// The maximum number of consecutive losing trades
        /// </summary>
        public int MaxConsecutiveLosingTrades { get; set; }

        /// <summary>
        /// The ratio of the average profit per trade to the average loss per trade
        /// </summary>
        /// <remarks>If the average loss is zero, ProfitLossRatio is set to 0</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProfitLossRatio { get; set; }

        /// <summary>
        /// The ratio of the number of winning trades to the number of losing trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, WinLossRatio is set to zero</remarks>
        /// <remarks>If the number of losing trades is zero and the number of winning trades is nonzero, WinLossRatio is set to 10</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal WinLossRatio { get; set; }

        /// <summary>
        /// The ratio of the number of winning trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, WinRate is set to zero</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal WinRate { get; set; }

        /// <summary>
        /// The ratio of the number of losing trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, LossRate is set to zero</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal LossRate { get; set; }

        /// <summary>
        /// The average Maximum Adverse Excursion for all trades
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageMAE { get; set; }

        /// <summary>
        /// The average Maximum Favorable Excursion for all trades
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageMFE { get; set; }

        /// <summary>
        /// The largest Maximum Adverse Excursion in a single trade (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal LargestMAE { get; set; }

        /// <summary>
        /// The largest Maximum Favorable Excursion in a single trade (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal LargestMFE { get; set; }

        /// <summary>
        /// The maximum closed-trade drawdown for all trades (as symbol currency)
        /// </summary>
        /// <remarks>The calculation only takes into account the profit/loss of each trade</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal MaximumClosedTradeDrawdown { get; set; }

        /// <summary>
        /// The maximum intra-trade drawdown for all trades (as symbol currency)
        /// </summary>
        /// <remarks>The calculation takes into account MAE and MFE of each trade</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal MaximumIntraTradeDrawdown { get; set; }

        /// <summary>
        /// The standard deviation of the profits/losses for all trades (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProfitLossStandardDeviation { get; set; }

        /// <summary>
        /// The downside deviation of the profits/losses for all trades (as symbol currency)
        /// </summary>
        /// <remarks>This metric only considers deviations of losing trades</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProfitLossDownsideDeviation { get; set; }

        /// <summary>
        /// The ratio of the total profit to the total loss
        /// </summary>
        /// <remarks>If the total profit is zero, ProfitFactor is set to zero</remarks>
        /// <remarks>if the total loss is zero and the total profit is nonzero, ProfitFactor is set to 10</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProfitFactor { get; set; }

        /// <summary>
        /// The ratio of the average profit/loss to the standard deviation
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal SharpeRatio { get; set; }

        /// <summary>
        /// The ratio of the average profit/loss to the downside deviation
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal SortinoRatio { get; set; }

        /// <summary>
        /// The ratio of the total profit/loss to the maximum closed trade drawdown
        /// </summary>
        /// <remarks>If the total profit/loss is zero, ProfitToMaxDrawdownRatio is set to zero</remarks>
        /// <remarks>if the drawdown is zero and the total profit is nonzero, ProfitToMaxDrawdownRatio is set to 10</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProfitToMaxDrawdownRatio { get; set; }

        /// <summary>
        /// The maximum amount of profit given back by a single trade before exit (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal MaximumEndTradeDrawdown { get; set; }

        /// <summary>
        /// The average amount of profit given back by all trades before exit (as symbol currency)
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageEndTradeDrawdown { get; set; }

        /// <summary>
        /// The maximum amount of time to recover from a drawdown (longest time between new equity highs or peaks)
        /// </summary>
        public TimeSpan MaximumDrawdownDuration { get; set; }

        /// <summary>
        /// The sum of fees for all trades
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TotalFees { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeStatistics"/> class
        /// </summary>
        /// <param name="trades">The list of closed trades</param>
        public TradeStatistics(IEnumerable<Trade> trades)
        {
            var maxConsecutiveWinners = 0;
            var maxConsecutiveLosers = 0;
            var maxTotalProfitLoss = 0m;
            var maxTotalProfitLossWithMfe = 0m;
            var sumForVariance = 0m;
            var sumForDownsideVariance = 0m;
            var lastPeakTime = DateTime.MinValue;
            var isInDrawdown = false;
            var allTradeDurationsTicks = new List<long>();
            var winningTradeDurationsTicks = new List<long>();
            var losingTradeDurationsTicks = new List<long>();
            var numberOfITMOptionsWinningTrades = 0;

            foreach (var trade in trades)
            {
                if (lastPeakTime == DateTime.MinValue) lastPeakTime = trade.EntryTime;

                if (StartDateTime == null || trade.EntryTime < StartDateTime)
                    StartDateTime = trade.EntryTime;

                if (EndDateTime == null || trade.ExitTime > EndDateTime)
                    EndDateTime = trade.ExitTime;

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

                    winningTradeDurationsTicks.Add(trade.Duration.Ticks);

                    if (trade.ProfitLoss > LargestProfit)
                        LargestProfit = trade.ProfitLoss;

                    maxConsecutiveWinners++;
                    maxConsecutiveLosers = 0;
                    if (maxConsecutiveWinners > MaxConsecutiveWinningTrades)
                        MaxConsecutiveWinningTrades = maxConsecutiveWinners;

                    if (TotalProfitLoss > maxTotalProfitLoss)
                    {
                        // new equity high
                        maxTotalProfitLoss = TotalProfitLoss;

                        if (isInDrawdown && trade.ExitTime - lastPeakTime > MaximumDrawdownDuration)
                            MaximumDrawdownDuration = trade.ExitTime - lastPeakTime;

                        lastPeakTime = trade.ExitTime;
                        isInDrawdown = false;
                    }
                }
                else
                {
                    // losing trade
                    NumberOfLosingTrades++;

                    TotalProfitLoss += trade.ProfitLoss;
                    TotalLoss += trade.ProfitLoss;
                    var prevAverageLoss = AverageLoss;
                    AverageLoss += (trade.ProfitLoss - AverageLoss) / NumberOfLosingTrades;

                    sumForDownsideVariance += (trade.ProfitLoss - prevAverageLoss) * (trade.ProfitLoss - AverageLoss);
                    var downsideVariance = NumberOfLosingTrades > 1 ? sumForDownsideVariance / (NumberOfLosingTrades - 1) : 0;
                    ProfitLossDownsideDeviation = (decimal)Math.Sqrt((double)downsideVariance);

                    AverageLosingTradeDuration += TimeSpan.FromSeconds((trade.Duration.TotalSeconds - AverageLosingTradeDuration.TotalSeconds) / NumberOfLosingTrades);

                    losingTradeDurationsTicks.Add(trade.Duration.Ticks);

                    if (trade.ProfitLoss < LargestLoss)
                        LargestLoss = trade.ProfitLoss;

                    // even though losing money, an ITM option trade is a winning trade,
                    // so IsWin for an ITM OptionTrade will return true even if the trade was not profitable.
                    if (trade.IsWin)
                    {
                        numberOfITMOptionsWinningTrades++;
                        maxConsecutiveLosers = 0;
                        maxConsecutiveWinners++;
                        if (maxConsecutiveWinners > MaxConsecutiveWinningTrades)
                            MaxConsecutiveWinningTrades = maxConsecutiveWinners;
                    }
                    else
                    {
                        maxConsecutiveWinners = 0;
                        maxConsecutiveLosers++;
                        if (maxConsecutiveLosers > MaxConsecutiveLosingTrades)
                            MaxConsecutiveLosingTrades = maxConsecutiveLosers;
                    }

                    if (TotalProfitLoss - maxTotalProfitLoss < MaximumClosedTradeDrawdown)
                        MaximumClosedTradeDrawdown = TotalProfitLoss - maxTotalProfitLoss;

                    isInDrawdown = true;
                }

                var prevAverageProfitLoss = AverageProfitLoss;
                AverageProfitLoss += (trade.ProfitLoss - AverageProfitLoss) / TotalNumberOfTrades;

                sumForVariance += (trade.ProfitLoss - prevAverageProfitLoss) * (trade.ProfitLoss - AverageProfitLoss);
                var variance = TotalNumberOfTrades > 1 ? sumForVariance / (TotalNumberOfTrades - 1) : 0;
                ProfitLossStandardDeviation = (decimal)Math.Sqrt((double)variance);

                AverageTradeDuration += TimeSpan.FromSeconds((trade.Duration.TotalSeconds - AverageTradeDuration.TotalSeconds) / TotalNumberOfTrades);
                allTradeDurationsTicks.Add(trade.Duration.Ticks);
                AverageMAE += (trade.MAE - AverageMAE) / TotalNumberOfTrades;
                AverageMFE += (trade.MFE - AverageMFE) / TotalNumberOfTrades;

                if (trade.MAE < LargestMAE)
                    LargestMAE = trade.MAE;

                if (trade.MFE > LargestMFE)
                    LargestMFE = trade.MFE;

                if (trade.EndTradeDrawdown < MaximumEndTradeDrawdown)
                    MaximumEndTradeDrawdown = trade.EndTradeDrawdown;

                TotalFees += trade.TotalFees;
            }

            // Adjust number of winning and losing trades: ITM options assignment loss counts as a loss for profit and loss calculations,
            // but adds a win to the wins count since this is an actual win even though premium paid is a loss.
            NumberOfWinningTrades += numberOfITMOptionsWinningTrades;
            NumberOfLosingTrades -= numberOfITMOptionsWinningTrades;

            ProfitLossRatio = AverageLoss == 0 ? 0 : AverageProfit / Math.Abs(AverageLoss);
            WinLossRatio = TotalNumberOfTrades == 0 ? 0 : (NumberOfLosingTrades > 0 ? (decimal)NumberOfWinningTrades / NumberOfLosingTrades : 10);
            WinRate = TotalNumberOfTrades > 0 ? (decimal)NumberOfWinningTrades / TotalNumberOfTrades : 0;
            LossRate = TotalNumberOfTrades > 0 ? 1 - WinRate : 0;
            ProfitFactor = TotalProfit == 0 ? 0 : (TotalLoss < 0 ? TotalProfit / Math.Abs(TotalLoss) : 10);
            SharpeRatio = ProfitLossStandardDeviation > 0 ? AverageProfitLoss / ProfitLossStandardDeviation : 0;
            SortinoRatio = ProfitLossDownsideDeviation > 0 ? AverageProfitLoss / ProfitLossDownsideDeviation : 0;
            ProfitToMaxDrawdownRatio = TotalProfitLoss == 0 ? 0 : (MaximumClosedTradeDrawdown < 0 ? TotalProfitLoss / Math.Abs(MaximumClosedTradeDrawdown) : 10);

            AverageEndTradeDrawdown = AverageProfitLoss - AverageMFE;

            if (allTradeDurationsTicks.Count > 0)
                MedianTradeDuration = TimeSpan.FromTicks(allTradeDurationsTicks.Median());
            if (winningTradeDurationsTicks.Count > 0)
                MedianWinningTradeDuration = TimeSpan.FromTicks(winningTradeDurationsTicks.Median());
            if (losingTradeDurationsTicks.Count > 0)
                MedianLosingTradeDuration = TimeSpan.FromTicks(losingTradeDurationsTicks.Median());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeStatistics"/> class
        /// </summary>
        public TradeStatistics()
        {
        }

    }
}
