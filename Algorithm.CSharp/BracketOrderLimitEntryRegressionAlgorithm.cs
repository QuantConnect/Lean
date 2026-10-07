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
    /// Regression algorithm asserting the behavior of a bracket order (OTOCO) built through the generic <see cref="OrderFactory"/> api (an entry which triggers a one cancels other)
    /// using a limit entry order: the take profit and the stop loss are held, they can't fill, until the entry order fills
    /// </summary>
    public class BracketOrderLimitEntryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private SubmitOrderRequest _entry;
        private SubmitOrderRequest _takeProfit;
        private SubmitOrderRequest _stopLoss;
        private DateTime? _entryFillTime;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_entry == null)
            {
                var price = Securities[_symbol].Price;

                // the take profit and stop loss are held until the entry fills
                _entry = OrderFactory.LimitOrder(_symbol, 100, Math.Round(price * 0.999m, 2), tag: "Entry");
                _takeProfit = OrderFactory.LimitOrder(_symbol, -100, Math.Round(price * 1.004m, 2), tag: "Take profit");
                _stopLoss = OrderFactory.StopMarketOrder(_symbol, -100, Math.Round(price * 0.99m, 2), tag: "Stop loss");
                _entry.Triggers(OrderFactory.OneCancelsOther(_takeProfit, _stopLoss));

                // composed but not submitted yet: the contingency is already set, the set id is not
                if (_entry.OrderId > 0 || Ticket(_entry) != null || _entry.Contingency.Id != 0 || _entry.Contingency.Links.Single().Role != ContingencyRole.Parent
                    || _takeProfit.Contingency.Links.Count != 2 || _stopLoss.Contingency.Links.Count != 2 || _stopLoss.Contingency.Links[1].Type != ContingencyType.OneCancelsOther)
                {
                    throw new RegressionTestException("Unexpected order request state before being submitted");
                }

                var tickets = Order(_entry);

                if (tickets.Count != 3 || tickets[0] != Ticket(_entry) || tickets[1] != Ticket(_takeProfit) || tickets[2] != Ticket(_stopLoss)
                    || _entry.OrderId <= 0 || _takeProfit.OrderId <= 0 || _stopLoss.OrderId <= 0
                    || tickets[1].Contingency.Links.Single(link => link.Role == null).Type != ContingencyType.OneCancelsOther)
                {
                    throw new RegressionTestException("Unexpected order tickets");
                }

                // an order request can only be submitted once
                try
                {
                    Order(_entry);
                    throw new RegressionTestException("Expected an exception when submitting an order request twice");
                }
                catch (ArgumentException)
                {
                }
            }

            if (Ticket(_entry).Status != OrderStatus.Filled)
            {
                foreach (var child in new[] { Ticket(_takeProfit), Ticket(_stopLoss) })
                {
                    if (!child.Contingency.IsWaitingForTrigger || child.Status != OrderStatus.Submitted || child.QuantityFilled != 0)
                    {
                        throw new RegressionTestException($"Expected the child order to be held waiting for the entry to fill: {child}");
                    }
                }

                // held orders are not accounted as open quantity
                var openQuantity = Transactions.GetOpenOrdersRemainingQuantity(_symbol);
                if (openQuantity != 100)
                {
                    throw new RegressionTestException($"Expected the open orders remaining quantity to be 100 but was {openQuantity}");
                }
            }
        }

        /// <summary>
        /// Order event handler
        /// </summary>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            if (orderEvent.OrderId == Ticket(_entry).OrderId)
            {
                _entryFillTime = orderEvent.UtcTime;
            }
            else
            {
                var triggeredTime = orderEvent.Ticket.Contingency.Links.Single(x => x.Role == ContingencyRole.Child).TriggeredTime;
                if (!_entryFillTime.HasValue || triggeredTime != _entryFillTime || orderEvent.UtcTime <= triggeredTime)
                {
                    throw new RegressionTestException($"Expected the exit order to fill after being triggered by the entry fill at {_entryFillTime}: {orderEvent}");
                }
            }
        }

        private OrderTicket Ticket(SubmitOrderRequest request)
        {
            return Transactions.GetOrderTicket(request.OrderId);
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_entryFillTime == null)
            {
                throw new RegressionTestException("Expected the entry order to be filled");
            }

            var exits = new[] { Ticket(_takeProfit), Ticket(_stopLoss) };
            if (exits.Count(x => x.Status == OrderStatus.Filled) != 1 || exits.Count(x => x.Status == OrderStatus.Canceled) != 1)
            {
                throw new RegressionTestException($"Expected one exit to fill and the other to be canceled: {string.Join(" | ", exits.Select(x => x.ToString()))}");
            }

            if (exits.Any(x => x.Contingency.IsWaitingForTrigger || x.Contingency.Links.Single(link => link.Role == ContingencyRole.Child).TriggeredTime != _entryFillTime))
            {
                throw new RegressionTestException("Expected both exits to be triggered at the entry fill time");
            }

            if (Portfolio.Invested || Transactions.GetOpenOrders().Count != 0)
            {
                throw new RegressionTestException("Expected the position to be closed and no open orders");
            }

            // the orders keep their contingencies
            var order = Transactions.GetOrderById(Ticket(_stopLoss).OrderId);
            if (order.Contingency?.Count != 3 || order.Contingency.Links.Count != 2 || order.IsWaitingForTrigger())
            {
                throw new RegressionTestException("Unexpected order contingencies");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

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
            {"Total Orders", "3"},
            {"Average Win", "0.07%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "5.626%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100070"},
            {"Net Profit", "0.070%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$24000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.80%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "52e1a35402ecc7e967322fe561f176d8"}
        };
    }
}
