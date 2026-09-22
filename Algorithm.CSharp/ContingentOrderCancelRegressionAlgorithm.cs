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
    /// Regression algorithm asserting the behavior of canceling contingent orders:
    ///  - canceling a parent order cancels the orders it would of triggered, including the ones those would trigger in turn
    ///  - canceling a member of a one cancels other contingency cancels its siblings too, the contingency is canceled as a whole
    ///    like brokerages do, whether the members are working or still held waiting for their parent
    /// </summary>
    public class ContingentOrderCancelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private int _step;
        private List<OrderTicket> _tickets;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 07);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            var price = Securities[_symbol].Price;
            // far from the market price, won't fill
            var entryPrice = Math.Round(price * 0.9m, 2);
            switch (_step)
            {
                case 0:
                    // a bracket whose take profit triggers another order in turn
                    var takeProfit = OrderFactory.LimitOrder(_symbol, -100, price * 1.1m).Triggers(OrderFactory.MarketOrder(_symbol, 10));
                    var stopLoss = OrderFactory.StopMarketOrder(_symbol, -100, price * 0.8m);
                    var entry = OrderFactory.LimitOrder(_symbol, 100, entryPrice).Triggers(OrderFactory.OneCancelsOther(takeProfit, stopLoss));
                    _tickets = Order(entry);
                    if (_tickets.Count != 4 || _tickets.Skip(1).Any(x => !x.Contingency.IsWaitingForTrigger))
                    {
                        throw new RegressionTestException("Unexpected order tickets");
                    }
                    break;

                case 1:
                    // canceling the parent cancels all the orders it would trigger
                    var response = _tickets[0].Cancel("Canceling the parent");
                    if (!response.IsSuccess)
                    {
                        throw new RegressionTestException($"Expected the cancel request to succeed: {response}");
                    }
                    break;

                case 2:
                    AssertCanceled(_tickets, _tickets);
                    // tickets are: entry, take profit, the order triggered by the take profit and the stop loss
                    var parentId = _tickets[0].OrderId;
                    var takeProfitId = _tickets[1].OrderId;
                    if (new[] { _tickets[1], _tickets[3] }.Any(x => !x.OrderEvents.Last().Message.Contains($"Contingent parent order {parentId} was canceled", StringComparison.InvariantCulture))
                        || !_tickets[2].OrderEvents.Last().Message.Contains($"Contingent parent order {takeProfitId} was canceled", StringComparison.InvariantCulture))
                    {
                        throw new RegressionTestException("Unexpected cancel event message");
                    }

                    _tickets = Order(OrderFactory.LimitOrder(_symbol, 100, entryPrice).Bracket(price * 1.1m, price * 0.8m));
                    break;

                case 3:
                    // canceling a held take profit cancels its sibling stop loss too, the parent keeps working
                    _tickets[1].Cancel("Canceling the held take profit");
                    break;

                case 4:
                    AssertCanceled(_tickets, _tickets.Skip(1));
                    if (!_tickets[2].OrderEvents.Last().Message.Contains($"Contingent sibling order {_tickets[1].OrderId} was canceled", StringComparison.InvariantCulture))
                    {
                        throw new RegressionTestException("Unexpected cancel event message for the sibling stop loss");
                    }
                    _tickets[0].Cancel();
                    break;

                case 5:
                    AssertCanceled(_tickets, _tickets);

                    MarketOrder(_symbol, 100);
                    _tickets = OneCancelsOtherOrder(new List<SubmitOrderRequest>
                    {
                        OrderFactory.LimitOrder(_symbol, -100, Math.Round(price * 1.1m, 2)),
                        OrderFactory.StopMarketOrder(_symbol, -100, Math.Round(price * 0.9m, 2))
                    });
                    break;

                case 6:
                    // canceling a member cancels its siblings
                    _tickets[0].Cancel("Canceling a sibling");
                    break;

                case 7:
                    AssertCanceled(_tickets, _tickets);

                    // liquidate
                    Liquidate();
                    break;

                case 8:
                    AssertCanceled(_tickets, _tickets);
                    if (Portfolio.Invested || Transactions.GetOpenOrders().Count != 0)
                    {
                        throw new RegressionTestException("Expected no position nor open orders");
                    }
                    break;
            }
            _step++;
        }

        private static void AssertCanceled(List<OrderTicket> tickets, IEnumerable<OrderTicket> expectedCanceled)
        {
            var canceled = expectedCanceled.Select(x => x.OrderId).ToHashSet();
            foreach (var ticket in tickets)
            {
                var expectedStatus = canceled.Contains(ticket.OrderId) ? OrderStatus.Canceled : OrderStatus.Submitted;
                if (ticket.Status != expectedStatus)
                {
                    throw new RegressionTestException($"Expected order {ticket.OrderId} status to be {expectedStatus} but was {ticket.Status}");
                }
            }
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_step < 9)
            {
                throw new RegressionTestException($"Unexpected step count {_step}");
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
        public long DataPoints => 795;

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
            {"Total Orders", "11"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99991.95"},
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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$21000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "28.97%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "b6bb48fdab4a83d8c32b3f700695b4e9"}
        };
    }
}
