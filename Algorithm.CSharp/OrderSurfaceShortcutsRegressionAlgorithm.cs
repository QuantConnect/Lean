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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the flat order surface shortcuts: the numeric OrderFee
    /// surface (OrderFee.Amount, arithmetic and comparison operators, OrderEvent.OrderFeeAmount),
    /// the flat combo group ids (Order.GroupOrderManagerId, OrderEvent.GroupId, ComboOrderTicket),
    /// the tag-argument tolerance of MarketOrder/Liquidate and OrderTargetNotional
    /// </summary>
    public class OrderSurfaceShortcutsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _equitySymbol;
        private Symbol _optionSymbol;
        private OrderTicket _taggedTicket;
        private OrderTicket _notionalTicket;
        private ComboOrderTicket _comboTicket;
        private readonly HashSet<int?> _comboFillGroupIds = new();
        private int _comboFillEventsCount;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            var equity = AddEquity("GOOG", leverage: 4, fillForward: true);
            _equitySymbol = equity.Symbol;
            var option = AddOption(equity.Symbol, fillForward: true);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.StandardsOnly().Strikes(-2, +2).Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (_taggedTicket == null && IsMarketOpen(_equitySymbol))
            {
                // tag in the tag argument slot; the Python version passes it in the third positional slot
                _taggedTicket = MarketOrder(_equitySymbol, 1, tag: "tagged entry");

                // target an absolute notional value instead of a portfolio percentage
                _notionalTicket = OrderTargetNotional(_equitySymbol, 10000);
                if (_notionalTicket == null)
                {
                    throw new RegressionTestException("OrderTargetNotional was expected to place an order");
                }

                // a tag slipped into the symbol slot must fail pointing to the tag parameter
                var liquidateFailed = false;
                try
                {
                    Liquidate("EOD close");
                }
                catch (ArgumentException exception)
                {
                    liquidateFailed = true;
                    if (!exception.Message.Contains("tag"))
                    {
                        throw new RegressionTestException(
                            $"Liquidate() with an unknown ticker was expected to point to the tag parameter but the error was: {exception.Message}");
                    }
                }
                if (!liquidateFailed)
                {
                    throw new RegressionTestException("Liquidate() with an unknown ticker was expected to fail");
                }
            }

            if (_comboTicket == null && IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                    .GroupBy(x => x.Expiry)
                    .OrderBy(grouping => grouping.Key)
                    .First()
                    .OrderBy(x => x.Strike)
                    .ToList();
                if (callContracts.Count < 3)
                {
                    return;
                }

                var legs = new List<Leg>()
                {
                    Leg.Create(callContracts[0].Symbol, 1),
                    Leg.Create(callContracts[1].Symbol, -2),
                    Leg.Create(callContracts[2].Symbol, 1)
                };
                _comboTicket = ComboMarketOrder(legs, 10);

                if (_comboTicket.Count != legs.Count || _comboTicket.Tickets.Count != legs.Count)
                {
                    throw new RegressionTestException($"Expected {legs.Count} leg tickets, found {_comboTicket.Count}");
                }
                if (_comboTicket.GroupOrderManagerId == null)
                {
                    throw new RegressionTestException("The combo order ticket was expected to have a group order manager id");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var order = Transactions.GetOrderById(orderEvent.OrderId);

            // the fee amount shortcuts and operators must match the two-level Value.Amount
            var feeAmount = orderEvent.OrderFee.Value.Amount;
            if (orderEvent.OrderFeeAmount != feeAmount || orderEvent.OrderFee.Amount != feeAmount)
            {
                throw new RegressionTestException($"Order fee amount shortcuts do not match the fee amount {feeAmount}");
            }
            if (orderEvent.OrderFee + orderEvent.OrderFee != 2 * feeAmount || (feeAmount != 0 && !(orderEvent.OrderFee > 0)))
            {
                throw new RegressionTestException($"Order fee operators do not match the fee amount {feeAmount}");
            }

            if (order.Type == OrderType.ComboMarket)
            {
                // Note: these fill events are received while the synchronous ComboMarketOrder() call is still
                // in flight, so the combo ticket is checked against them in OnEndOfAlgorithm
                _comboFillEventsCount++;
                if (order.GroupOrderManagerId == null)
                {
                    throw new RegressionTestException("Combo orders were expected to have a group order manager id");
                }
                if (orderEvent.GroupId != order.GroupOrderManagerId)
                {
                    throw new RegressionTestException($"Expected order event group id {order.GroupOrderManagerId}, found {orderEvent.GroupId}");
                }
                _comboFillGroupIds.Add(order.GroupOrderManagerId);
            }
            else if (order.GroupOrderManagerId != null || orderEvent.GroupId != null)
            {
                throw new RegressionTestException("Non-combo orders were expected to have null group ids");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_taggedTicket == null || _taggedTicket.Tag != "tagged entry")
            {
                throw new RegressionTestException("The market order tag was not set from the tag argument");
            }
            if (_notionalTicket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("The notional target order was expected to be filled");
            }
            if (_comboTicket == null || _comboFillEventsCount != _comboTicket.Count)
            {
                throw new RegressionTestException("The combo order was expected to be placed and filled");
            }
            if (!_comboTicket.Filled)
            {
                throw new RegressionTestException("The combo order ticket was expected to aggregate the leg fills");
            }
            if (_comboFillGroupIds.Single() != _comboTicket.GroupOrderManagerId)
            {
                throw new RegressionTestException($"Expected all combo fills to have group id {_comboTicket.GroupOrderManagerId}, " +
                    $"found {string.Join(", ", _comboFillGroupIds)}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 15023;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "198005.36"},
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
            {"Total Fees", "$28.00"},
            {"Estimated Strategy Capacity", "$80000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAT67A|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "35.27%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "94d4e9ad7a0c13884b49d68165ce6766"}
        };
    }
}
