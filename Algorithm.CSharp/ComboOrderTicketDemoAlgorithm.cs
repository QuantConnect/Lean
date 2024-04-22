
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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm for testing submit/update/cancel for combo orders
    /// </summary>
    public class ComboOrderTicketDemoAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly List<OrderTicket> _openMarketOrders = new();
        private readonly List<OrderTicket> _openLegLimitOrders = new();
        private readonly List<OrderTicket> _openLimitOrders = new();

        private Symbol _optionSymbol;
        private List<Leg> _orderLegs;

        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var equity = AddEquity("GOOG", leverage: 4, fillForward: true);
            var option = AddOption(equity.Symbol, fillForward: true);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                  .Expiration(0, 180));
        }

        public override void OnData(Slice data)
        {
            if (_orderLegs == null)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && data.OptionChains.TryGetValue(_optionSymbol, out chain))
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

                    _orderLegs = new List<Leg>()
                    {
                        Leg.Create(callContracts[0].Symbol, 1),
                        Leg.Create(callContracts[1].Symbol, -2),
                        Leg.Create(callContracts[2].Symbol, 1),
                    };
                }
            }
            else
            {
                // COMBO MARKET ORDERS

                ComboMarketOrders();

                // COMBO LIMIT ORDERS

                ComboLimitOrders();

                // COMBO LEG LIMIT ORDERS

                ComboLegLimitOrders();
            }
        }

        private void ComboMarketOrders()
        {
            if (_openMarketOrders.Count != 0 || _orderLegs == null)
            {
                return;
            }

            Log("Submitting combo market orders");

            var tickets = ComboMarketOrder(_orderLegs, 2, asynchronous: false);
            _openMarketOrders.AddRange(tickets);

            tickets = ComboMarketOrder(_orderLegs, 2, asynchronous: true);
            _openMarketOrders.AddRange(tickets);

            foreach (var ticket in tickets)
            {
                var response = ticket.Cancel("Attempt to cancel combo market order");
                if (response.IsSuccess)
                {
                    throw new Exception("Combo market orders should fill instantly, they should not be cancelable in backtest mode: " + response.OrderId);
                }
            }
        }

        private void ComboLimitOrders()
        {
            if (_openLimitOrders.Count == 0)
            {
                Log("Submitting ComboLimitOrder");

                var currentPrice = _orderLegs.Sum(leg => leg.Quantity * Securities[leg.Symbol].Close);

                var tickets = ComboLimitOrder(_orderLegs, 2, currentPrice + 1.5m);
                _openLimitOrders.AddRange(tickets);

                // These won't fill, we will test cancel with this
                tickets = ComboLimitOrder(_orderLegs, -2, currentPrice + 3m);
                _openLimitOrders.AddRange(tickets);
            }
            else
            {
                var combo1 = _openLimitOrders.Take(_orderLegs.Count).ToList();
                var combo2 = _openLimitOrders.Skip(_orderLegs.Count).Take(_orderLegs.Count).ToList();

                // check if either is filled and cancel the other
                if (CheckGroupOrdersForFills(combo1, combo2))
                {
                    return;
                }

                // if neither order has filled, bring in the limits by a penny

                var ticket = combo1[0];
                var newLimit = Math.Round(ticket.Get(OrderField.LimitPrice) + 0.01m, 2);
                Debug($"Updating limits - Combo 1 {ticket.OrderId}: {newLimit.ToStringInvariant("0.00")}");
                ticket.Update(new UpdateOrderFields
                {
                    LimitPrice = newLimit,
                    Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                });

                ticket = combo2[0];
                newLimit = Math.Round(ticket.Get(OrderField.LimitPrice) - 0.01m, 2);
                Debug($"Updating limits - Combo 2 {ticket.OrderId}: {newLimit.ToStringInvariant("0.00")}");
                ticket.Update(new UpdateOrderFields
                {
                    LimitPrice = newLimit,
                    Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                });
            }
        }

        private void ComboLegLimitOrders()
        {
            if (_openLegLimitOrders.Count == 0)
            {
                Log("Submitting ComboLegLimitOrder");

                // submit a limit order to buy 2 shares at .1% below the bar's close
                foreach (var leg in _orderLegs)
                {
                    var close = Securities[leg.Symbol].Close;
                    leg.OrderPrice = close * .999m;
                }

                var tickets = ComboLegLimitOrder(_orderLegs, quantity: 2);
                _openLegLimitOrders.AddRange(tickets);

                // submit another limit order to sell 2 shares at .1% above the bar's close
                foreach (var leg in _orderLegs)
                {
                    var close = Securities[leg.Symbol].Close;
                    leg.OrderPrice = close * 1.001m;
                }

                tickets = ComboLegLimitOrder(_orderLegs, -2);
                _openLegLimitOrders.AddRange(tickets);
            }
            else
            {
                var combo1 = _openLegLimitOrders.Take(_orderLegs.Count).ToList();
                var combo2 = _openLegLimitOrders.Skip(_orderLegs.Count).Take(_orderLegs.Count).ToList();

                // check if either is filled and cancel the other
                if (CheckGroupOrdersForFills(combo1, combo2))
                {
                    return;
                }

                // if neither order has filled, bring in the limits by a penny

                foreach (var ticket in combo1)
                {
                    var newLimit = Math.Round(ticket.Get(OrderField.LimitPrice) + (ticket.Quantity > 0 ? 1m : -1m) * 0.01m, 2);
                    Debug($"Updating limits - Combo #1: {newLimit.ToStringInvariant("0.00")}");

                    ticket.Update(new UpdateOrderFields
                    {
                        LimitPrice = newLimit,
                        Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                    });
                }

                foreach (var ticket in combo2)
                {
                    var newLimit = Math.Round(ticket.Get(OrderField.LimitPrice) + (ticket.Quantity > 0 ? 1m : -1m) * 0.01m, 2);
                    Debug($"Updating limits - Combo #2: {newLimit.ToStringInvariant("0.00")}");

                    ticket.Update(new UpdateOrderFields
                    {
                        LimitPrice = newLimit,
                        Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                    });
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);

            if (orderEvent.Quantity == 0)
            {
                throw new Exception("OrderEvent quantity is Not expected to be 0, it should hold the current order Quantity");
            }
            if (orderEvent.Quantity != order.Quantity)
            {
                throw new Exception($@"OrderEvent quantity should hold the current order Quantity. Got {orderEvent.Quantity
                    }, expected {order.Quantity}");
            }
            if (order is ComboLegLimitOrder && orderEvent.LimitPrice == 0)
            {
                throw new Exception("OrderEvent.LimitPrice is not expected to be 0 for ComboLegLimitOrder");
            }
        }

        private bool CheckGroupOrdersForFills(List<OrderTicket> combo1, List<OrderTicket> combo2)
        {
            if (combo1.All(x => x.Status == OrderStatus.Filled))
            {
                if (combo2.Any(x => x.Status.IsOpen()))
                {
                    Log(combo1[0].OrderType + ": Canceling combo #2, combo #1 is filled.");
                    combo2.ForEach(x => x.Cancel("Combo #1 filled."));
                }

                return true;
            }

            if (combo2.All(x => x.Status == OrderStatus.Filled))
            {
                if (combo1.Any(x => x.Status.IsOpen()))
                {
                    Log(combo1[0].OrderType + ": Canceling combo #1, combo #2 is filled.");
                    combo1.ForEach(x => x.Cancel("Combo #2 filled."));
                }

                return true;
            }

            return false;
        }

        public override void OnEndOfAlgorithm()
        {
            var filledOrders = Transactions.GetOrders(x => x.Status == OrderStatus.Filled).ToList();
            var orderTickets = Transactions.GetOrderTickets().ToList();
            var openOrders = Transactions.GetOpenOrders();
            var openOrderTickets = Transactions.GetOpenOrderTickets().ToList();
            var remainingOpenOrders = Transactions.GetOpenOrdersRemainingQuantity();

            // 6 market, 6 limit, 6 leg limit.
            // Out of the 6 limit orders, 3 are expected to be canceled.
            var expectedOrdersCount = 18;
            var expectedFillsCount = 15;
            if (filledOrders.Count != expectedFillsCount || orderTickets.Count != expectedOrdersCount)
            {
                throw new Exception($"There were expected {expectedFillsCount} filled orders and {expectedOrdersCount} order tickets, but there were {filledOrders.Count} filled orders and {orderTickets.Count} order tickets");
            }

            var filledComboMarketOrders = filledOrders.Where(x => x.Type == OrderType.ComboMarket).ToList();
            var filledComboLimitOrders = filledOrders.Where(x => x.Type == OrderType.ComboLimit).ToList();
            var filledComboLegLimitOrders = filledOrders.Where(x => x.Type == OrderType.ComboLegLimit).ToList();
            if (filledComboMarketOrders.Count != 6 || filledComboLimitOrders.Count != 3 || filledComboLegLimitOrders.Count != 6)
            {
                throw new Exception(
                    "There were expected 6 filled market orders, 3 filled combo limit orders and 6 filled combo leg limit orders, " +
                    $@"but there were {filledComboMarketOrders.Count} filled market orders, {filledComboLimitOrders.Count
                    } filled combo limit orders and {filledComboLegLimitOrders.Count} filled combo leg limit orders");
            }

            if (openOrders.Count != 0 || openOrderTickets.Count != 0)
            {
                throw new Exception($"No open orders or tickets were expected");
            }

            if (remainingOpenOrders != 0m)
            {
                throw new Exception($"No remaining quantity to be filled from open orders was expected");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

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
            {"Total Orders", "18"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98838"},
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
            {"Total Fees", "$26.00"},
            {"Estimated Strategy Capacity", "$2000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "58.98%"},
            {"OrderListHash", "e69460f62d4c165fe4b4a9bff1f48962"}
        };
    }
}
