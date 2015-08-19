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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// The TradeBuilder class generates trades from executions and market price updates
    /// </summary>
    public class TradeBuilder
    {
        /// <summary>
        /// Helper class to manage pending trades and market price updates for a symbol
        /// </summary>
        private class Position 
        {
            internal List<Trade> PendingTrades { get; set; }
            internal List<TradeExecution> Executions { get; set; }
            internal decimal MaxPrice { get; set; }
            internal decimal MinPrice { get; set; }

            public Position()
            {
                PendingTrades = new List<Trade>();
                Executions = new List<TradeExecution>();
            }
        }

        private readonly List<Trade> _closedTrades = new List<Trade>();
        private readonly Dictionary<string, Position> _positions = new Dictionary<string, Position>();
        private readonly FillGroupingMethod _groupingMethod;
        private readonly FillMatchingMethod _matchingMethod;

        /// <summary>
        /// Initializes a new instance of the TradeBuilder class
        /// </summary>
        public TradeBuilder(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            _groupingMethod = groupingMethod;
            _matchingMethod = matchingMethod;
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
        public bool HasOpenPosition(string symbol)
        {
            Position position;
            if (!_positions.TryGetValue(symbol, out position)) return false;

            if (_groupingMethod == FillGroupingMethod.FillToFill)
                return position.PendingTrades.Count > 0;

            return position.Executions.Count > 0;
        }

        /// <summary>
        /// Sets the current market price for the symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="price"></param>
        public void SetMarketPrice(string symbol, decimal price)
        {
            Position position;
            if (!_positions.TryGetValue(symbol, out position)) return;

            if (price > position.MaxPrice)
                position.MaxPrice = price;
            else if (price < position.MinPrice)
                position.MinPrice = price;
        }

        /// <summary>
        /// Processes a new execution, eventually creating a new trade
        /// </summary>
        /// <param name="execution">The new execution</param>
        public void AddExecution(TradeExecution execution)
        {
            switch (_groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    AddExecutionFillToFill(execution);
                    break;

                case FillGroupingMethod.FlatToFlat:
                    AddExecutionFlatToFlat(execution);
                    break;

                case FillGroupingMethod.FlatToReduced:
                    AddExecutionFlatToReduced(execution);
                    break;
            }
        }

        private void AddExecutionFillToFill(TradeExecution execution)
        {
            Position position;
            if (!_positions.TryGetValue(execution.Symbol, out position) || position.PendingTrades.Count == 0)
            {
                // no pending trades for symbol
                _positions[execution.Symbol] = new Position
                {
                    PendingTrades = new List<Trade>
                    {
                        new Trade
                        {
                            Symbol = execution.Symbol,
                            EntryTime = execution.Time,
                            EntryPrice = execution.Price,
                            Direction = execution.Quantity > 0 ? TradeDirection.Long : TradeDirection.Short,
                            Quantity = Math.Abs(execution.Quantity),
                        }
                    },
                    MinPrice = execution.Price,
                    MaxPrice = execution.Price
                };
                return;
            }

            SetMarketPrice(execution.Symbol, execution.Price);

            var index = _matchingMethod == FillMatchingMethod.FIFO ? 0 : position.PendingTrades.Count - 1;

            if (Math.Sign(execution.Quantity) == (position.PendingTrades[index].Direction == TradeDirection.Long ? +1 : -1))
            {
                // execution has same direction of trade
                position.PendingTrades.Add(new Trade
                {
                    Symbol = execution.Symbol,
                    EntryTime = execution.Time,
                    EntryPrice = execution.Price,
                    Direction = execution.Quantity > 0 ? TradeDirection.Long : TradeDirection.Short,
                    Quantity = Math.Abs(execution.Quantity),
                });
            }
            else
            {
                // execution has opposite direction of trade
                var totalExecutedQuantity = 0m;

                while (position.PendingTrades.Count > 0 && Math.Abs(totalExecutedQuantity) < Math.Abs(execution.Quantity))
                {
                    var trade = position.PendingTrades[index];

                    if (Math.Abs(execution.Quantity) >= trade.Quantity)
                    {
                        totalExecutedQuantity -= trade.Quantity * (trade.Direction == TradeDirection.Long ? +1 : -1);
                        position.PendingTrades.RemoveAt(index);

                        if (index > 0 && _matchingMethod == FillMatchingMethod.LIFO) index--;

                        trade.ExitTime = execution.Time;
                        trade.ExitPrice = execution.Price;
                        trade.ProfitLoss = (trade.ExitPrice - trade.EntryPrice) * trade.Quantity * (trade.Direction == TradeDirection.Long ? +1 : -1);
                        trade.MAE = (trade.Direction == TradeDirection.Long ? position.MinPrice - trade.EntryPrice : trade.EntryPrice - position.MaxPrice) * trade.Quantity;
                        trade.MFE = (trade.Direction == TradeDirection.Long ? position.MaxPrice - trade.EntryPrice : trade.EntryPrice - position.MinPrice) * trade.Quantity;
                        
                        _closedTrades.Add(trade);
                    }
                    else
                    {
                        totalExecutedQuantity += execution.Quantity;
                        trade.Quantity -= Math.Abs(execution.Quantity);

                        _closedTrades.Add(new Trade
                        {
                            Symbol = trade.Symbol,
                            EntryTime = trade.EntryTime,
                            EntryPrice = trade.EntryPrice,
                            Direction = trade.Direction,
                            Quantity = Math.Abs(execution.Quantity),
                            ExitTime = execution.Time,
                            ExitPrice = execution.Price,
                            ProfitLoss = (execution.Price - trade.EntryPrice) * Math.Abs(execution.Quantity) * (trade.Direction == TradeDirection.Long ? +1 : -1),
                            MAE = (trade.Direction == TradeDirection.Long ? position.MinPrice - trade.EntryPrice : trade.EntryPrice - position.MaxPrice) * Math.Abs(execution.Quantity),
                            MFE = (trade.Direction == TradeDirection.Long ? position.MaxPrice - trade.EntryPrice : trade.EntryPrice - position.MinPrice) * Math.Abs(execution.Quantity)
                        });
                    }
                }

                if (Math.Abs(totalExecutedQuantity) == Math.Abs(execution.Quantity) && position.PendingTrades.Count == 0)
                {
                    _positions.Remove(execution.Symbol);
                }
                else if (Math.Abs(totalExecutedQuantity) < Math.Abs(execution.Quantity))
                {
                    // direction reversal
                    execution.Quantity -= totalExecutedQuantity;
                    position.PendingTrades = new List<Trade>
                    {
                        new Trade
                        {
                            Symbol = execution.Symbol,
                            EntryTime = execution.Time,
                            EntryPrice = execution.Price,
                            Direction = execution.Quantity > 0 ? TradeDirection.Long : TradeDirection.Short,
                            Quantity = Math.Abs(execution.Quantity),
                        }
                    };
                    position.MinPrice = execution.Price;
                    position.MaxPrice = execution.Price;
                }
            }
        }

        private void AddExecutionFlatToFlat(TradeExecution execution)
        {
            Position position;
            if (!_positions.TryGetValue(execution.Symbol, out position) || position.Executions.Count == 0)
            {
                // no pending executions for symbol
                _positions[execution.Symbol] = new Position
                {
                    Executions = new List<TradeExecution> { execution },
                    MinPrice = execution.Price,
                    MaxPrice = execution.Price
                };
                return;
            }

            SetMarketPrice(execution.Symbol, execution.Price);

            if (Math.Sign(position.Executions[0].Quantity) == Math.Sign(execution.Quantity))
            {
                // execution has same direction of trade
                position.Executions.Add(execution);
            }
            else
            {
                // execution has opposite direction of trade
                if (position.Executions.Sum(x => x.Quantity) + execution.Quantity == 0 || Math.Abs(execution.Quantity) > Math.Abs(position.Executions.Sum(x => x.Quantity)))
                {
                    // trade closed
                    position.Executions.Add(execution);

                    var reverseQuantity = position.Executions.Sum(x => x.Quantity);

                    var index = _matchingMethod == FillMatchingMethod.FIFO ? 0 : position.Executions.Count - 1;

                    var entryTime = position.Executions[0].Time;
                    var totalEntryQuantity = 0m;
                    var totalExitQuantity = 0m;
                    var entryAveragePrice = 0m;
                    var exitAveragePrice = 0m;

                    while (position.Executions.Count > 0)
                    {
                        if (Math.Sign(position.Executions[index].Quantity) != Math.Sign(execution.Quantity))
                        {
                            // entry
                            totalEntryQuantity += position.Executions[index].Quantity;
                            entryAveragePrice += position.Executions[index].Quantity / totalEntryQuantity * (position.Executions[index].Price - entryAveragePrice);
                        }
                        else
                        {
                            // exit
                            totalExitQuantity += position.Executions[index].Quantity;
                            exitAveragePrice += position.Executions[index].Quantity / totalExitQuantity * (position.Executions[index].Price - exitAveragePrice);
                        }
                        position.Executions.RemoveAt(index);

                        if (_matchingMethod == FillMatchingMethod.LIFO && index > 0) index--;
                    }

                    var direction = Math.Sign(execution.Quantity) < 0 ? TradeDirection.Long : TradeDirection.Short;

                    _closedTrades.Add(new Trade
                    {
                        Symbol = execution.Symbol,
                        EntryTime = entryTime,
                        EntryPrice = entryAveragePrice,
                        Direction = direction,
                        Quantity = Math.Abs(totalEntryQuantity),
                        ExitTime = execution.Time,
                        ExitPrice = exitAveragePrice,
                        ProfitLoss = Math.Round((exitAveragePrice - entryAveragePrice) * Math.Abs(totalEntryQuantity) * Math.Sign(totalEntryQuantity), 2),
                        MAE = Math.Round((direction == TradeDirection.Long ? position.MinPrice - entryAveragePrice : entryAveragePrice - position.MaxPrice) * Math.Abs(totalEntryQuantity), 2),
                        MFE = Math.Round((direction == TradeDirection.Long ? position.MaxPrice - entryAveragePrice : entryAveragePrice - position.MinPrice) * Math.Abs(totalEntryQuantity), 2)
                    });

                    _positions.Remove(execution.Symbol);

                    if (reverseQuantity != 0)
                    {
                        // direction reversal
                        execution.Quantity = reverseQuantity;
                        _positions[execution.Symbol] = new Position
                        {
                            Executions = new List<TradeExecution> { execution },
                            MinPrice = execution.Price,
                            MaxPrice = execution.Price
                        };
                    }
                }
                else
                {
                    // trade open
                    position.Executions.Add(execution);
                }
            }
        }

        private void AddExecutionFlatToReduced(TradeExecution execution)
        {
            Position position;
            if (!_positions.TryGetValue(execution.Symbol, out position) || position.Executions.Count == 0)
            {
                // no pending executions for symbol
                _positions[execution.Symbol] = new Position
                {
                    Executions = new List<TradeExecution> { execution },
                    MinPrice = execution.Price,
                    MaxPrice = execution.Price
                };
                return;
            }

            SetMarketPrice(execution.Symbol, execution.Price);

            var index = _matchingMethod == FillMatchingMethod.FIFO ? 0 : position.Executions.Count - 1;

            if (Math.Sign(execution.Quantity) == Math.Sign(position.Executions[index].Quantity))
            {
                // execution has same direction of trade
                position.Executions.Add(execution);
            }
            else
            {
                // execution has opposite direction of trade
                var entryTime = position.Executions[index].Time;
                var totalExecutedQuantity = 0m;
                var entryPrice = 0m;

                while (position.Executions.Count > 0 && Math.Abs(totalExecutedQuantity) < Math.Abs(execution.Quantity))
                {
                    if (Math.Abs(execution.Quantity) >= Math.Abs(position.Executions[index].Quantity))
                    {
                        if (_matchingMethod == FillMatchingMethod.LIFO)
                            entryTime = position.Executions[index].Time;

                        totalExecutedQuantity -= position.Executions[index].Quantity;
                        entryPrice -= position.Executions[index].Quantity / totalExecutedQuantity * (position.Executions[index].Price - entryPrice);
                        position.Executions.RemoveAt(index);

                        if (_matchingMethod == FillMatchingMethod.LIFO && index > 0) index--;
                    }
                    else
                    {
                        totalExecutedQuantity += execution.Quantity;
                        entryPrice += execution.Quantity / totalExecutedQuantity * (position.Executions[index].Price - entryPrice);
                        position.Executions[index].Quantity += execution.Quantity;
                    }
                }

                var direction = totalExecutedQuantity < 0 ? TradeDirection.Long : TradeDirection.Short;

                _closedTrades.Add(new Trade
                {
                    Symbol = execution.Symbol,
                    EntryTime = entryTime,
                    EntryPrice = entryPrice,
                    Direction = direction,
                    Quantity = Math.Abs(totalExecutedQuantity),
                    ExitTime = execution.Time,
                    ExitPrice = execution.Price,
                    ProfitLoss = Math.Round((execution.Price - entryPrice) * Math.Abs(totalExecutedQuantity) * Math.Sign(-totalExecutedQuantity), 2),
                    MAE = Math.Round((direction == TradeDirection.Long ? position.MinPrice - entryPrice : entryPrice - position.MaxPrice) * Math.Abs(totalExecutedQuantity), 2),
                    MFE = Math.Round((direction == TradeDirection.Long ? position.MaxPrice - entryPrice : entryPrice - position.MinPrice) * Math.Abs(totalExecutedQuantity), 2)
                });

                if (Math.Abs(totalExecutedQuantity) == Math.Abs(execution.Quantity) && position.Executions.Count == 0)
                {
                    _positions.Remove(execution.Symbol);
                }
                else if (Math.Abs(totalExecutedQuantity) < Math.Abs(execution.Quantity))
                {
                    // direction reversal
                    execution.Quantity -= totalExecutedQuantity;
                    position.Executions = new List<TradeExecution> { execution };
                    position.MinPrice = execution.Price;
                    position.MaxPrice = execution.Price;
                }
            }
        }

    }
}
