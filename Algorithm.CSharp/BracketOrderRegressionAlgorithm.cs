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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of the <see cref="QCAlgorithm.BracketOrder"/> helper method (OTOCO):
    /// a market entry order which once filled triggers a take profit and a stop loss order, the first one to fill cancels the other
    /// </summary>
    public class BracketOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private List<OrderTicket> _tickets;
        private readonly List<OrderEvent> _orderEvents = new();

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
            if (_tickets != null)
            {
                return;
            }

            var price = Securities[_symbol].Price;
            _tickets = BracketOrder(_symbol, 100, takeProfitPrice: price * 1.005m, stopLossPrice: price * 0.995m, tag: "Bracket");

            if (_tickets.Count != 3)
            {
                throw new RegressionTestException($"Expected 3 order tickets, but got {_tickets.Count}");
            }

            var entry = _tickets[0];
            var takeProfit = _tickets[1];
            var stopLoss = _tickets[2];
            if (entry.OrderType != OrderType.Market || entry.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Expected the market entry order to be filled: {entry}");
            }
            if (takeProfit.OrderType != OrderType.Limit || takeProfit.Quantity != -100 || stopLoss.OrderType != OrderType.StopMarket || stopLoss.Quantity != -100)
            {
                throw new RegressionTestException("Unexpected take profit and stop loss orders");
            }

            foreach (var ticket in _tickets)
            {
                if (ticket.Contingency == null || ticket.Contingency.Count != 3
                    || !ticket.Contingency.OrderIds.SetEquals(_tickets.Select(x => x.OrderId)))
                {
                    throw new RegressionTestException($"Unexpected contingency for order {ticket.OrderId}");
                }
            }

            var parent = entry.Contingency.Links.Single();
            if (parent.Type != ContingencyType.OneTriggersOther || parent.Role != ContingencyRole.Parent)
            {
                throw new RegressionTestException($"Unexpected entry contingencies: {string.Join(",", entry.Contingency.Links)}");
            }

            foreach (var child in new[] { takeProfit, stopLoss })
            {
                // the entry already filled so they should of been triggered and be working
                if (child.Contingency.IsWaitingForTrigger || child.Status != OrderStatus.Submitted || child.Contingency.Links.Count != 2
                    || !child.Contingency.Links.Any(link => link.Type == ContingencyType.OneTriggersOther && link.Role == ContingencyRole.Child && link.Id == parent.Id
                        && link.Triggered && link.TriggeredTime == UtcTime)
                    || !child.Contingency.Links.Any(link => link.Type == ContingencyType.OneCancelsOther && link.Role == null))
                {
                    throw new RegressionTestException($"Unexpected child order state: {child}. Contingencies: {string.Join(",", child.Contingency.Links)}");
                }
            }
        }

        /// <summary>
        /// Order event handler
        /// </summary>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            _orderEvents.Add(orderEvent);
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_tickets == null)
            {
                throw new RegressionTestException("The bracket order was never submitted");
            }

            var exits = _tickets.Skip(1).ToList();
            var filled = exits.Where(x => x.Status == OrderStatus.Filled).ToList();
            var canceled = exits.Where(x => x.Status == OrderStatus.Canceled).ToList();
            if (filled.Count != 1 || canceled.Count != 1)
            {
                throw new RegressionTestException($"Expected one exit to fill and the other to be canceled: {string.Join(" | ", exits)}");
            }

            if (Portfolio.Invested)
            {
                throw new RegressionTestException("Expected the position to be closed by the bracket exit");
            }

            // the sibling is canceled right after the fill
            var fillIndex = _orderEvents.FindIndex(x => x.OrderId == filled[0].OrderId && x.Status == OrderStatus.Filled);
            var cancelEvent = _orderEvents[fillIndex + 1];
            if (cancelEvent.OrderId != canceled[0].OrderId || cancelEvent.Status != OrderStatus.Canceled || cancelEvent.UtcTime != _orderEvents[fillIndex].UtcTime)
            {
                throw new RegressionTestException($"Expected the sibling to be canceled right after the fill, but was: {cancelEvent}");
            }

            if (Transactions.GetOpenOrders().Count != 0)
            {
                throw new RegressionTestException("Unexpected open orders");
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
            {"Compounding Annual Return", "5.586%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100069.52"},
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
            {"Estimated Strategy Capacity", "$31000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.80%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "4ab291d7e7df4d2d944da910653113b6"}
        };
    }
}
