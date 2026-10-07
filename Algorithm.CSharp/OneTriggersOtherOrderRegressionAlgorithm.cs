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
    /// Regression algorithm asserting the behavior of the <see cref="QCAlgorithm.OneTriggersOtherOrder"/> helper method (OTO):
    /// a parent order which once filled triggers multiple independent orders, for different symbols, one of which triggers another in turn (chain)
    /// </summary>
    public class OneTriggersOtherOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _ibm;
        private Symbol _bac;
        private SubmitOrderRequest _parent;
        private SubmitOrderRequest _ibmChild;
        private SubmitOrderRequest _bacGrandChild;
        private SubmitOrderRequest _limitChild;
        private List<OrderTicket> _tickets;
        private readonly List<int> _fillOrder = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            _ibm = AddEquity("IBM", Resolution.Minute).Symbol;
            _bac = AddEquity("BAC", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_parent == null)
            {
                if (!slice.ContainsKey(_spy) || !slice.ContainsKey(_ibm) || !slice.ContainsKey(_bac))
                {
                    return;
                }

                var price = Securities[_spy].Price;
                _parent = OrderFactory.LimitOrder(_spy, 100, Math.Round(price * 0.999m, 2), tag: "Parent");
                _bacGrandChild = OrderFactory.MarketOrder(_bac, 20, tag: "Grand child");
                _ibmChild = OrderFactory.MarketOrder(_ibm, 10, tag: "Child").Triggers(_bacGrandChild);
                _limitChild = OrderFactory.LimitOrder(_spy, -50, Math.Round(price * 1.05m, 2), tag: "Independent child");

                _tickets = OneTriggersOtherOrder(_parent, new List<SubmitOrderRequest> { _ibmChild, _limitChild });

                var expectedTickets = new[] { Ticket(_parent), Ticket(_ibmChild), Ticket(_bacGrandChild), Ticket(_limitChild) };
                if (!_tickets.SequenceEqual(expectedTickets) || _tickets.Any(x => x.Contingency.Count != 4))
                {
                    throw new RegressionTestException("Unexpected order tickets");
                }

                // the IBM order is the child of a contingency and the parent of another one
                var contingencies = Ticket(_ibmChild).Contingency.Links;
                if (contingencies.Count != 2 || contingencies.Any(x => x.Type != ContingencyType.OneTriggersOther)
                    || contingencies.Single(x => x.Role == ContingencyRole.Child).Id != Ticket(_parent).Contingency.Links.Single().Id
                    || contingencies.Single(x => x.Role == ContingencyRole.Parent).Id != Ticket(_bacGrandChild).Contingency.Links.Single().Id
                    || Ticket(_limitChild).Contingency.Links.Single().Role != ContingencyRole.Child)
                {
                    throw new RegressionTestException("Unexpected contingencies");
                }
            }

            if (Ticket(_parent).Status != OrderStatus.Filled)
            {
                if (_tickets.Skip(1).Any(x => !x.Contingency.IsWaitingForTrigger || x.Status != OrderStatus.Submitted))
                {
                    throw new RegressionTestException("Expected all the orders to be held waiting for the parent to fill");
                }
            }
            else if (_tickets.Any(x => x.Contingency.IsWaitingForTrigger))
            {
                throw new RegressionTestException("Expected all the orders to be triggered once the parent filled");
            }
        }

        /// <summary>
        /// Order event handler
        /// </summary>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _fillOrder.Add(orderEvent.OrderId);
            }
            else if (orderEvent.Status == OrderStatus.Canceled)
            {
                throw new RegressionTestException($"Unexpected canceled order event, the triggered orders are independent: {orderEvent}");
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
            var expectedFillOrder = new[] { Ticket(_parent).OrderId, Ticket(_ibmChild).OrderId, Ticket(_bacGrandChild).OrderId };
            if (!_fillOrder.SequenceEqual(expectedFillOrder))
            {
                throw new RegressionTestException($"Unexpected fill order: {string.Join(",", _fillOrder)}");
            }

            // market orders fill right away once triggered
            var parentFillTime = Ticket(_parent).OrderEvents.Single(x => x.Status == OrderStatus.Filled).UtcTime;
            if (Ticket(_ibmChild).OrderEvents.Single(x => x.Status == OrderStatus.Filled).UtcTime != parentFillTime
                || Ticket(_bacGrandChild).OrderEvents.Single(x => x.Status == OrderStatus.Filled).UtcTime != parentFillTime)
            {
                throw new RegressionTestException("Expected the market orders to fill once triggered");
            }

            if (Portfolio[_spy].Quantity != 100 || Portfolio[_ibm].Quantity != 10 || Portfolio[_bac].Quantity != 20)
            {
                throw new RegressionTestException("Unexpected holdings");
            }

            // the independent limit order is still working
            var openOrder = Transactions.GetOpenOrders().Single();
            if (openOrder.Id != Ticket(_limitChild).OrderId || openOrder.IsWaitingForTrigger() || openOrder.Status != OrderStatus.Submitted)
            {
                throw new RegressionTestException("Expected the independent limit order to be still working");
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
        public long DataPoints => 11743;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "25.744%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100293.32"},
            {"Net Profit", "0.293%"},
            {"Sharpe Ratio", "5.352"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "66.734%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.125"},
            {"Beta", "0.16"},
            {"Annual Standard Deviation", "0.036"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-9.545"},
            {"Tracking Error", "0.187"},
            {"Treynor Ratio", "1.193"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$510000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "3.21%"},
            {"Drawdown Recovery", "2"},
            {"OrderListHash", "b4f102bd24c3554af06b65aece16548e"}
        };
    }
}
