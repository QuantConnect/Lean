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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that a position opened with a combo order is properly closed with another combo order in the opposite direction.
    /// </summary>
    public class RevertComboOrderPositionsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int _comboQuantity = 10;

        private Option _option;

        private List<Leg> _orderLegs;

        private List<OrderTicket> _entryOrderTickets = new();
        private List<OrderTicket> _exitOrderTickets = new();

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(10000);

            var equitySymbol = AddEquity("GOOG", leverage: 4, fillForward: true).Symbol;
            _option = AddOption(equitySymbol, fillForward: true);
            _option.SetFilter(optionFilterUniverse => optionFilterUniverse
                .Strikes(-2, 2)
                .Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (_orderLegs == null)
            {
                OptionChain chain;
                if (IsMarketOpen(_option.Symbol) && slice.OptionChains.TryGetValue(_option.Symbol, out chain))
                {
                    var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                        .GroupBy(x => x.Expiry)
                        .OrderBy(grouping => grouping.Key)
                        .First()
                        .OrderBy(x => x.Strike)
                        .ToList();

                    // Let's wait until we have at least three contracts
                    if (callContracts.Count < 3)
                    {
                        return;
                    }

                    Debug("Placing entry combo market order");
                    _orderLegs = new List<Leg>()
                    {
                        Leg.Create(callContracts[0].Symbol, 1),
                        Leg.Create(callContracts[1].Symbol, -2),
                        Leg.Create(callContracts[2].Symbol, 1)
                    };
                    _entryOrderTickets = ComboMarketOrder(_orderLegs, _comboQuantity);
                }
            }
            else if (Portfolio.Invested && _exitOrderTickets.Count == 0)
            {
                Debug("Placing exit combo limit order");
                var entryOrderFillPrice = GetComboOrderFillPrice(_entryOrderTickets);
                _exitOrderTickets = ComboLimitOrder(_orderLegs, -_comboQuantity, -entryOrderFillPrice * 1.05m);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                // The multiplier depends on whether this order belongs either to the entry or exit combo order
                var multiplier = _exitOrderTickets.Count > 0 ? -1 : 1;
                var expectedQuantity = multiplier * _comboQuantity * _orderLegs.Where(leg => leg.Symbol == orderEvent.Symbol).Single().Quantity;
                if (orderEvent.Quantity != expectedQuantity)
                {
                    throw new Exception($"Order event quantity {orderEvent.Quantity} does not match expected quantity {expectedQuantity}");
                }
                if (orderEvent.FillQuantity != expectedQuantity)
                {
                    throw new Exception(
                        $"Order event fill quantity {orderEvent.FillQuantity} does not match expected fill quantity {expectedQuantity}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception("Portfolio should not be invested at the end of the algorithm.");
            }

            if (_entryOrderTickets.Count == 0 || _entryOrderTickets.Any(ticket => ticket.Status != OrderStatus.Filled))
            {
                throw new Exception("Entry order was not filled");
            }

            if (_exitOrderTickets.Count == 0 || _exitOrderTickets.Any(ticket => ticket.Status != OrderStatus.Filled))
            {
                throw new Exception("Exit order was not filled");
            }

            for (var i = 0; i < _orderLegs.Count; i++)
            {
                var leg = _orderLegs[i];
                var entryOrderTicket = _entryOrderTickets[i];
                var exitOrderTicket = _exitOrderTickets[i];

                var expectedEntryQuantity = leg.Quantity * _comboQuantity;
                if (entryOrderTicket.Quantity != expectedEntryQuantity || entryOrderTicket.QuantityFilled != expectedEntryQuantity)
                {
                    throw new Exception($@"Entry order ticket quantity and filled quantity do not match expected quantity for leg {i
                        }. Expected: {expectedEntryQuantity}. Actual quantity: {entryOrderTicket.Quantity}. Actual filled quantity: {
                        entryOrderTicket.QuantityFilled}");
                }

                var expectedExitQuantity = -expectedEntryQuantity;
                if (exitOrderTicket.Quantity != expectedExitQuantity || exitOrderTicket.QuantityFilled != expectedExitQuantity)
                {
                    throw new Exception($@"Exit order ticket quantity and filled quantity do not match expected quantity for leg {i
                        }. Expected: {expectedExitQuantity}. Actual quantity: {exitOrderTicket.Quantity}. Actual filled quantity: {
                        exitOrderTicket.QuantityFilled}");
                }
            }
        }

        private decimal GetComboOrderFillPrice(List<OrderTicket> orderTickets)
        {
            return orderTickets.Aggregate(0m, (accumulatedPrice, ticket) =>
            {
                var legQuantity = _orderLegs.Where(leg => leg.Symbol == ticket.Symbol).Single().Quantity;
                return accumulatedPrice + ticket.AverageFillPrice * legQuantity;
            });
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 471135;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "10000"},
            {"End Equity", "5764"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$36.00"},
            {"Estimated Strategy Capacity", "$16000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "2088.83%"},
            {"OrderListHash", "52947bba687287a189cee038daec6918"}
        };
    }
}
