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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// The <see cref="TradeBuilder"/> class generates trades from executions and market price updates
    /// </summary>
    public class TradeBuilder : ITradeBuilder
    {
        /// <summary>
        /// Helper class to manage pending trades and market price updates for a symbol
        /// </summary>
        private class Position
        {
            internal List<Trade> PendingTrades { get; set; }
            internal List<OrderEvent> PendingFills { get; set; }
            internal decimal TotalFees { get; set; }
            internal decimal MaxPrice { get; set; }
            internal decimal MinPrice { get; set; }

            public Position()
            {
                PendingTrades = new List<Trade>();
                PendingFills = new List<OrderEvent>();
            }
        }

        private const int LiveModeMaxTradeCount = 10000;
        private const int LiveModeMaxTradeAgeMonths = 12;
        private const int MaxOrderIdCacheSize = 1000;

        private readonly List<Trade> _closedTrades = new List<Trade>();
        private readonly Dictionary<Symbol, Position> _positions = new Dictionary<Symbol, Position>();
        private readonly FixedSizeHashQueue<int> _ordersWithFeesAssigned = new FixedSizeHashQueue<int>(MaxOrderIdCacheSize);
        private readonly FillGroupingMethod _groupingMethod;
        private readonly FillMatchingMethod _matchingMethod;
        private bool _liveMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeBuilder"/> class
        /// </summary>
        public TradeBuilder(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            _groupingMethod = groupingMethod;
            _matchingMethod = matchingMethod;
        }

        /// <summary>
        /// Sets the live mode flag
        /// </summary>
        /// <param name="live">The live mode flag</param>
        public void SetLiveMode(bool live)
        {
            _liveMode = live;
        }

        /// <summary>
        /// The list of closed trades
        /// </summary>
        public List<Trade> ClosedTrades
        {
            get { return _closedTrades; }
        }

        /// <summary>
        /// Returns true if there is an open position for the symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>true if there is an open position for the symbol</returns>
        public bool HasOpenPosition(Symbol symbol)
        {
            Position position;
            if (!_positions.TryGetValue(symbol, out position)) return false;

            if (_groupingMethod == FillGroupingMethod.FillToFill)
                return position.PendingTrades.Count > 0;

            return position.PendingFills.Count > 0;
        }

        /// <summary>
        /// Sets the current market price for the symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="price"></param>
        public void SetMarketPrice(Symbol symbol, decimal price)
        {
            Position position;
            if (!_positions.TryGetValue(symbol, out position)) return;

            if (price > position.MaxPrice)
                position.MaxPrice = price;
            else if (price < position.MinPrice)
                position.MinPrice = price;
        }

        /// <summary>
        /// Processes a new fill, eventually creating new trades
        /// </summary>
        /// <param name="fill">The new fill order event</param>
        /// <param name="conversionRate">The current security market conversion rate into the account currency</param>
        /// <param name="feeInAccountCurrency">The current order fee in the account currency</param>
        /// <param name="multiplier">The contract multiplier</param>
        public void ProcessFill(OrderEvent fill,
            decimal conversionRate,
            decimal feeInAccountCurrency,
            decimal multiplier = 1.0m)
        {
            // If we have multiple fills per order, we assign the order fee only to its first fill
            // to avoid counting the same order fee multiple times.
            var orderFee = 0m;
            if (!_ordersWithFeesAssigned.Contains(fill.OrderId))
            {
                orderFee = feeInAccountCurrency;
                _ordersWithFeesAssigned.Add(fill.OrderId);
            }

            switch (_groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    ProcessFillUsingFillToFill(fill.Clone(), orderFee, conversionRate, multiplier);
                    break;

                case FillGroupingMethod.FlatToFlat:
                    ProcessFillUsingFlatToFlat(fill.Clone(), orderFee, conversionRate, multiplier);
                    break;

                case FillGroupingMethod.FlatToReduced:
                    ProcessFillUsingFlatToReduced(fill.Clone(), orderFee, conversionRate, multiplier);
                    break;
            }
        }

        private void ProcessFillUsingFillToFill(OrderEvent fill, decimal orderFee, decimal conversionRate, decimal multiplier)
        {
            Position position;
            if (!_positions.TryGetValue(fill.Symbol, out position) || position.PendingTrades.Count == 0)
            {
                // no pending trades for symbol
                _positions[fill.Symbol] = new Position
                {
                    PendingTrades = new List<Trade>
                    {
                        new Trade
                        {
                            Symbol = fill.Symbol,
                            EntryTime = fill.UtcTime,
                            EntryPrice = fill.FillPrice,
                            Direction = fill.FillQuantity > 0 ? TradeDirection.Long : TradeDirection.Short,
                            Quantity = fill.AbsoluteFillQuantity,
                            TotalFees = orderFee
                        }
                    },
                    MinPrice = fill.FillPrice,
                    MaxPrice = fill.FillPrice
                };
                return;
            }

            SetMarketPrice(fill.Symbol, fill.FillPrice);

            var index = _matchingMethod == FillMatchingMethod.FIFO ? 0 : position.PendingTrades.Count - 1;

            if (Math.Sign(fill.FillQuantity) == (position.PendingTrades[index].Direction == TradeDirection.Long ? +1 : -1))
            {
                // execution has same direction of trade
                position.PendingTrades.Add(new Trade
                {
                    Symbol = fill.Symbol,
                    EntryTime = fill.UtcTime,
                    EntryPrice = fill.FillPrice,
                    Direction = fill.FillQuantity > 0 ? TradeDirection.Long : TradeDirection.Short,
                    Quantity = fill.AbsoluteFillQuantity,
                    TotalFees = orderFee
                });
            }
            else
            {
                // execution has opposite direction of trade
                var totalExecutedQuantity = 0m;
                var orderFeeAssigned = false;
                while (position.PendingTrades.Count > 0 && Math.Abs(totalExecutedQuantity) < fill.AbsoluteFillQuantity)
                {
                    var trade = position.PendingTrades[index];
                    var absoluteUnexecutedQuantity = fill.AbsoluteFillQuantity - Math.Abs(totalExecutedQuantity);

                    if (absoluteUnexecutedQuantity >= trade.Quantity)
                    {
                        totalExecutedQuantity -= trade.Quantity * (trade.Direction == TradeDirection.Long ? +1 : -1);
                        position.PendingTrades.RemoveAt(index);

                        if (index > 0 && _matchingMethod == FillMatchingMethod.LIFO) index--;

                        trade.ExitTime = fill.UtcTime;
                        trade.ExitPrice = fill.FillPrice;
                        trade.ProfitLoss = Math.Round((trade.ExitPrice - trade.EntryPrice) * trade.Quantity * (trade.Direction == TradeDirection.Long ? +1 : -1) * conversionRate * multiplier, 2);
                        // if closing multiple trades with the same order, assign order fee only once
                        trade.TotalFees += orderFeeAssigned ? 0 : orderFee;
                        trade.MAE = Math.Round((trade.Direction == TradeDirection.Long ? position.MinPrice - trade.EntryPrice : trade.EntryPrice - position.MaxPrice) * trade.Quantity * conversionRate * multiplier, 2);
                        trade.MFE = Math.Round((trade.Direction == TradeDirection.Long ? position.MaxPrice - trade.EntryPrice : trade.EntryPrice - position.MinPrice) * trade.Quantity * conversionRate * multiplier, 2);

                        AddNewTrade(trade);
                    }
                    else
                    {
                        totalExecutedQuantity += absoluteUnexecutedQuantity * (trade.Direction == TradeDirection.Long ? -1 : +1);
                        trade.Quantity -= absoluteUnexecutedQuantity;

                        AddNewTrade(new Trade
                        {
                            Symbol = trade.Symbol,
                            EntryTime = trade.EntryTime,
                            EntryPrice = trade.EntryPrice,
                            Direction = trade.Direction,
                            Quantity = absoluteUnexecutedQuantity,
                            ExitTime = fill.UtcTime,
                            ExitPrice = fill.FillPrice,
                            ProfitLoss = Math.Round((fill.FillPrice - trade.EntryPrice) * absoluteUnexecutedQuantity * (trade.Direction == TradeDirection.Long ? +1 : -1) * conversionRate * multiplier, 2),
                            TotalFees = trade.TotalFees + (orderFeeAssigned ? 0 : orderFee),
                            MAE = Math.Round((trade.Direction == TradeDirection.Long ? position.MinPrice - trade.EntryPrice : trade.EntryPrice - position.MaxPrice) * absoluteUnexecutedQuantity * conversionRate * multiplier, 2),
                            MFE = Math.Round((trade.Direction == TradeDirection.Long ? position.MaxPrice - trade.EntryPrice : trade.EntryPrice - position.MinPrice) * absoluteUnexecutedQuantity * conversionRate * multiplier, 2)
                        });

                        trade.TotalFees = 0;
                    }

                    orderFeeAssigned = true;
                }

                if (Math.Abs(totalExecutedQuantity) == fill.AbsoluteFillQuantity && position.PendingTrades.Count == 0)
                {
                    _positions.Remove(fill.Symbol);
                }
                else if (Math.Abs(totalExecutedQuantity) < fill.AbsoluteFillQuantity)
                {
                    // direction reversal
                    fill.FillQuantity -= totalExecutedQuantity;
                    position.PendingTrades = new List<Trade>
                    {
                        new Trade
                        {
                            Symbol = fill.Symbol,
                            EntryTime = fill.UtcTime,
                            EntryPrice = fill.FillPrice,
                            Direction = fill.FillQuantity > 0 ? TradeDirection.Long : TradeDirection.Short,
                            Quantity = fill.AbsoluteFillQuantity,
                            TotalFees = 0
                        }
                    };
                    position.MinPrice = fill.FillPrice;
                    position.MaxPrice = fill.FillPrice;
                }
            }
        }

        private void ProcessFillUsingFlatToFlat(OrderEvent fill, decimal orderFee, decimal conversionRate, decimal multiplier)
        {
            Position position;
            if (!_positions.TryGetValue(fill.Symbol, out position) || position.PendingFills.Count == 0)
            {
                // no pending executions for symbol
                _positions[fill.Symbol] = new Position
                {
                    PendingFills = new List<OrderEvent> { fill },
                    TotalFees = orderFee,
                    MinPrice = fill.FillPrice,
                    MaxPrice = fill.FillPrice
                };
                return;
            }

            SetMarketPrice(fill.Symbol, fill.FillPrice);

            if (Math.Sign(position.PendingFills[0].FillQuantity) == Math.Sign(fill.FillQuantity))
            {
                // execution has same direction of trade
                position.PendingFills.Add(fill);
                position.TotalFees += orderFee;
            }
            else
            {
                // execution has opposite direction of trade
                if (position.PendingFills.Aggregate(0m, (d, x) => d + x.FillQuantity) + fill.FillQuantity == 0 || fill.AbsoluteFillQuantity > Math.Abs(position.PendingFills.Aggregate(0m, (d, x) => d + x.FillQuantity)))
                {
                    // trade closed
                    position.PendingFills.Add(fill);
                    position.TotalFees += orderFee;

                    var reverseQuantity = position.PendingFills.Sum(x => x.FillQuantity);

                    var index = _matchingMethod == FillMatchingMethod.FIFO ? 0 : position.PendingFills.Count - 1;

                    var entryTime = position.PendingFills[0].UtcTime;
                    var totalEntryQuantity = 0m;
                    var totalExitQuantity = 0m;
                    var entryAveragePrice = 0m;
                    var exitAveragePrice = 0m;

                    while (position.PendingFills.Count > 0)
                    {
                        if (Math.Sign(position.PendingFills[index].FillQuantity) != Math.Sign(fill.FillQuantity))
                        {
                            // entry
                            totalEntryQuantity += position.PendingFills[index].FillQuantity;
                            entryAveragePrice += (position.PendingFills[index].FillPrice - entryAveragePrice) * position.PendingFills[index].FillQuantity / totalEntryQuantity;
                        }
                        else
                        {
                            // exit
                            totalExitQuantity += position.PendingFills[index].FillQuantity;
                            exitAveragePrice += (position.PendingFills[index].FillPrice - exitAveragePrice) * position.PendingFills[index].FillQuantity / totalExitQuantity;
                        }
                        position.PendingFills.RemoveAt(index);

                        if (_matchingMethod == FillMatchingMethod.LIFO && index > 0) index--;
                    }

                    var direction = Math.Sign(fill.FillQuantity) < 0 ? TradeDirection.Long : TradeDirection.Short;

                    AddNewTrade(new Trade
                    {
                        Symbol = fill.Symbol,
                        EntryTime = entryTime,
                        EntryPrice = entryAveragePrice,
                        Direction = direction,
                        Quantity = Math.Abs(totalEntryQuantity),
                        ExitTime = fill.UtcTime,
                        ExitPrice = exitAveragePrice,
                        ProfitLoss = Math.Round((exitAveragePrice - entryAveragePrice) * Math.Abs(totalEntryQuantity) * Math.Sign(totalEntryQuantity) * conversionRate * multiplier, 2),
                        TotalFees = position.TotalFees,
                        MAE = Math.Round((direction == TradeDirection.Long ? position.MinPrice - entryAveragePrice : entryAveragePrice - position.MaxPrice) * Math.Abs(totalEntryQuantity) * conversionRate * multiplier, 2),
                        MFE = Math.Round((direction == TradeDirection.Long ? position.MaxPrice - entryAveragePrice : entryAveragePrice - position.MinPrice) * Math.Abs(totalEntryQuantity) * conversionRate * multiplier, 2)
                    });

                    _positions.Remove(fill.Symbol);

                    if (reverseQuantity != 0)
                    {
                        // direction reversal
                        fill.FillQuantity = reverseQuantity;
                        _positions[fill.Symbol] = new Position
                        {
                            PendingFills = new List<OrderEvent> { fill },
                            TotalFees = 0,
                            MinPrice = fill.FillPrice,
                            MaxPrice = fill.FillPrice
                        };
                    }
                }
                else
                {
                    // trade open
                    position.PendingFills.Add(fill);
                    position.TotalFees += orderFee;
                }
            }
        }

        private void ProcessFillUsingFlatToReduced(OrderEvent fill, decimal orderFee, decimal conversionRate, decimal multiplier)
        {
            Position position;
            if (!_positions.TryGetValue(fill.Symbol, out position) || position.PendingFills.Count == 0)
            {
                // no pending executions for symbol
                _positions[fill.Symbol] = new Position
                {
                    PendingFills = new List<OrderEvent> { fill },
                    TotalFees = orderFee,
                    MinPrice = fill.FillPrice,
                    MaxPrice = fill.FillPrice
                };
                return;
            }

            SetMarketPrice(fill.Symbol, fill.FillPrice);

            var index = _matchingMethod == FillMatchingMethod.FIFO ? 0 : position.PendingFills.Count - 1;

            if (Math.Sign(fill.FillQuantity) == Math.Sign(position.PendingFills[index].FillQuantity))
            {
                // execution has same direction of trade
                position.PendingFills.Add(fill);
                position.TotalFees += orderFee;
            }
            else
            {
                // execution has opposite direction of trade
                var entryTime = position.PendingFills[index].UtcTime;
                var totalExecutedQuantity = 0m;
                var entryPrice = 0m;
                position.TotalFees += orderFee;

                while (position.PendingFills.Count > 0 && Math.Abs(totalExecutedQuantity) < fill.AbsoluteFillQuantity)
                {
                    var absoluteUnexecutedQuantity = fill.AbsoluteFillQuantity - Math.Abs(totalExecutedQuantity);
                    if (absoluteUnexecutedQuantity >= Math.Abs(position.PendingFills[index].FillQuantity))
                    {
                        if (_matchingMethod == FillMatchingMethod.LIFO)
                            entryTime = position.PendingFills[index].UtcTime;

                        totalExecutedQuantity -= position.PendingFills[index].FillQuantity;
                        entryPrice -= (position.PendingFills[index].FillPrice - entryPrice) * position.PendingFills[index].FillQuantity / totalExecutedQuantity;
                        position.PendingFills.RemoveAt(index);

                        if (_matchingMethod == FillMatchingMethod.LIFO && index > 0) index--;
                    }
                    else
                    {
                        var executedQuantity = absoluteUnexecutedQuantity * Math.Sign(fill.FillQuantity);
                        totalExecutedQuantity += executedQuantity;
                        entryPrice += (position.PendingFills[index].FillPrice - entryPrice) * executedQuantity / totalExecutedQuantity;
                        position.PendingFills[index].FillQuantity += executedQuantity;
                    }
                }

                var direction = totalExecutedQuantity < 0 ? TradeDirection.Long : TradeDirection.Short;

                AddNewTrade(new Trade
                {
                    Symbol = fill.Symbol,
                    EntryTime = entryTime,
                    EntryPrice = entryPrice,
                    Direction = direction,
                    Quantity = Math.Abs(totalExecutedQuantity),
                    ExitTime = fill.UtcTime,
                    ExitPrice = fill.FillPrice,
                    ProfitLoss = Math.Round((fill.FillPrice - entryPrice) * Math.Abs(totalExecutedQuantity) * Math.Sign(-totalExecutedQuantity) * conversionRate * multiplier, 2),
                    TotalFees = position.TotalFees,
                    MAE = Math.Round((direction == TradeDirection.Long ? position.MinPrice - entryPrice : entryPrice - position.MaxPrice) * Math.Abs(totalExecutedQuantity) * conversionRate * multiplier, 2),
                    MFE = Math.Round((direction == TradeDirection.Long ? position.MaxPrice - entryPrice : entryPrice - position.MinPrice) * Math.Abs(totalExecutedQuantity) * conversionRate * multiplier, 2)
                });

                if (Math.Abs(totalExecutedQuantity) < fill.AbsoluteFillQuantity)
                {
                    // direction reversal
                    fill.FillQuantity -= totalExecutedQuantity;
                    position.PendingFills = new List<OrderEvent> { fill };
                    position.TotalFees = 0;
                    position.MinPrice = fill.FillPrice;
                    position.MaxPrice = fill.FillPrice;
                }
                else if (Math.Abs(totalExecutedQuantity) == fill.AbsoluteFillQuantity)
                {
                    if (position.PendingFills.Count == 0)
                        _positions.Remove(fill.Symbol);
                    else
                        position.TotalFees = 0;
                }
            }
        }

        /// <summary>
        /// Adds a trade to the list of closed trades, capping the total number only in live mode
        /// </summary>
        private void AddNewTrade(Trade trade)
        {
            _closedTrades.Add(trade);

            // Due to memory constraints in live mode, we cap the number of trades
            if (!_liveMode)
                return;

            // maximum number of trades
            if (_closedTrades.Count > LiveModeMaxTradeCount)
            {
                _closedTrades.RemoveRange(0, _closedTrades.Count - LiveModeMaxTradeCount);
            }

            // maximum age of trades
            while (_closedTrades.Count > 0 && _closedTrades[0].ExitTime.Date.AddMonths(LiveModeMaxTradeAgeMonths) < DateTime.Today)
            {
                _closedTrades.RemoveAt(0);
            }
        }

    }
}
