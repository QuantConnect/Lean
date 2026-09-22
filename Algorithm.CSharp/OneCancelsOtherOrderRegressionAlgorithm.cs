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
    /// Regression algorithm asserting the behavior of the <see cref="QCAlgorithm.OneCancelsOtherOrder"/> helper method (OCO/OCA):
    /// a set of orders working at the same time where the first one to fill cancels the rest. We use it to exit an existing
    /// position, each time it's closed we open it again and submit a new set of exit orders.
    /// </summary>
    public class OneCancelsOtherOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private List<OrderTicket> _tickets;
        private int _completedSets;
        private readonly HashSet<int> _contingentOrderSetIds = new();

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
                if (_tickets.Any(x => x.Status.IsClosed()))
                {
                    AssertCompletedSet();
                    _tickets = null;
                }
                return;
            }

            if (Portfolio.Invested || Transactions.GetOpenOrders().Count != 0)
            {
                throw new RegressionTestException("Expected no position nor open orders before submitting a new set of orders");
            }

            MarketOrder(_symbol, 100);

            var price = Securities[_symbol].Price;
            _tickets = OneCancelsOtherOrder(new List<SubmitOrderRequest>
            {
                OrderFactory.LimitOrder(_symbol, -100, price * 1.003m, tag: "Take Profit"),
                OrderFactory.StopMarketOrder(_symbol, -100, price * 0.997m, tag: "Stop Loss"),
                OrderFactory.StopLimitOrder(_symbol, -100, price * 0.99m, price * 0.98m, tag: "Far Stop Loss")
            });

            if (_tickets.Count != 3)
            {
                throw new RegressionTestException($"Expected 3 order tickets, but got {_tickets.Count}");
            }

            foreach (var ticket in _tickets)
            {
                var contingency = ticket.Contingency.Links.Single();
                if (ticket.Contingency.IsWaitingForTrigger || ticket.Status != OrderStatus.Submitted || ticket.Contingency.Count != 3
                    || contingency.Type != ContingencyType.OneCancelsOther || contingency.Role != null
                    || contingency.Id != _tickets[0].Contingency.Links[0].Id)
                {
                    throw new RegressionTestException($"Unexpected order state: {ticket}. Contingencies: {string.Join(",", ticket.Contingency.Links)}");
                }
            }

            if (!_contingentOrderSetIds.Add(_tickets[0].Contingency.Id))
            {
                throw new RegressionTestException("Expected a new contingent order set id for each set of orders");
            }

            // at most one of them will fill
            var openQuantity = Transactions.GetOpenOrdersRemainingQuantity(_symbol);
            if (openQuantity != -100)
            {
                throw new RegressionTestException($"Expected the open orders remaining quantity to be -100 but was {openQuantity}");
            }
        }

        private void AssertCompletedSet()
        {
            if (_tickets.Count(x => x.Status == OrderStatus.Filled) != 1 || _tickets.Count(x => x.Status == OrderStatus.Canceled) != 2)
            {
                throw new RegressionTestException($"Expected one order to fill and the others to be canceled: {string.Join(" | ", _tickets)}");
            }

            foreach (var canceled in _tickets.Where(x => x.Status == OrderStatus.Canceled))
            {
                var cancelEvent = canceled.OrderEvents.Single(x => x.Status == OrderStatus.Canceled);
                if (!cancelEvent.Message.Contains("Contingent sibling order", System.StringComparison.InvariantCulture))
                {
                    throw new RegressionTestException($"Unexpected cancel event message: {cancelEvent.Message}");
                }
            }

            if (Portfolio.Invested)
            {
                throw new RegressionTestException("Expected the position to be closed");
            }
            _completedSets++;
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_completedSets < 2)
            {
                throw new RegressionTestException($"Expected at least 2 completed sets of orders but got {_completedSets}");
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
            {"Total Orders", "84"},
            {"Average Win", "0.05%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "14.732%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0.166"},
            {"Start Equity", "100000"},
            {"End Equity", "100175.87"},
            {"Net Profit", "0.176%"},
            {"Sharpe Ratio", "3.916"},
            {"Sortino Ratio", "20.439"},
            {"Probabilistic Sharpe Ratio", "64.087%"},
            {"Loss Rate", "45%"},
            {"Win Rate", "55%"},
            {"Profit-Loss Ratio", "1.12"},
            {"Alpha", "-0.133"},
            {"Beta", "0.125"},
            {"Annual Standard Deviation", "0.029"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-9.548"},
            {"Tracking Error", "0.195"},
            {"Treynor Ratio", "0.913"},
            {"Total Fees", "$41.00"},
            {"Estimated Strategy Capacity", "$29000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "118.45%"},
            {"Drawdown Recovery", "2"},
            {"OrderListHash", "224828e3037b4636bab46ec66613ab34"}
        };
    }
}
