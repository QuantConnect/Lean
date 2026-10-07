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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of contingent combo orders: a combo market order which once all its legs fill
    /// triggers two combo limit orders related through a one cancels other contingency. Each combo order is handled as a single unit:
    /// when one of the combo limit orders fills all the legs of the other one are canceled.
    /// </summary>
    public class ContingentComboOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private List<SubmitOrderRequest> _parent;
        private List<SubmitOrderRequest> _farExit;
        private List<SubmitOrderRequest> _marketableExit;
        private List<OrderTicket> _parentTickets;
        private List<OrderTicket> _farExitTickets;
        private List<OrderTicket> _marketableExitTickets;
        private int _step;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var equity = AddEquity("GOOG", leverage: 4, fillForward: true);
            var option = AddOption(equity.Symbol, fillForward: true);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.StandardsOnly().Strikes(-2, +2).Expiration(0, 180));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_parent == null)
            {
                if (!IsMarketOpen(_optionSymbol) || !slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
                {
                    return;
                }

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

                var legs = new List<Leg>
                {
                    Leg.Create(callContracts[0].Symbol, 1),
                    Leg.Create(callContracts[1].Symbol, -2),
                    Leg.Create(callContracts[2].Symbol, 1),
                };
                var currentPrice = legs.Sum(leg => leg.Quantity * Securities[leg.Symbol].Close);

                // selling the combo: the first one is too expensive so it won't fill, the second one is marketable
                _farExit = OrderFactory.ComboLimitOrder(legs, -2, currentPrice + 3m, tag: "Far exit");
                _marketableExit = OrderFactory.ComboLimitOrder(legs, -2, currentPrice - 1.5m, tag: "Marketable exit");
                _parent = OrderFactory.ComboMarketOrder(legs, 2, tag: "Parent");
                // the legs of a combo order are a single unit, they trigger together
                var tickets = OneTriggersOtherOrder(_parent, OrderFactory.OneCancelsOther(_farExit.Concat(_marketableExit)));
                _parentTickets = tickets.Take(3).ToList();
                _farExitTickets = tickets.Skip(3).Take(3).ToList();
                _marketableExitTickets = tickets.Skip(6).ToList();

                if (tickets.Count != 9 || _parent.Any(leg => leg.Contingency.Count != 9) || _farExitTickets.Count != 3 || _marketableExitTickets.Count != 3
                    || tickets.Any(x => x.Contingency.Count != 9)
                    || tickets.Select(x => x.SubmitRequest.GroupOrderManager.Id).Distinct().Count() != 3)
                {
                    throw new RegressionTestException("Unexpected order tickets");
                }

                // the combo market order filled, all its legs, so the exits were triggered
                if (_parentTickets.Any(x => x.Status != OrderStatus.Filled)
                    || _farExitTickets.Concat(_marketableExitTickets).Any(x => x.Contingency.IsWaitingForTrigger || x.Status != OrderStatus.Submitted))
                {
                    throw new RegressionTestException("Expected the parent combo order to be filled and the exits to be triggered");
                }

                // each leg holds the contingencies of its combo order
                if (_parentTickets.Any(x => x.Contingency.Links.Single().Role != ContingencyRole.Parent)
                    || _farExitTickets.Concat(_marketableExitTickets).Any(x => x.Contingency.Links.Count != 2
                        || x.Contingency.Links.Count(link => link.Role == ContingencyRole.Child && link.Triggered) != 1
                        || x.Contingency.Links.Count(link => link.Role == null && link.Type == ContingencyType.OneCancelsOther) != 1))
                {
                    throw new RegressionTestException("Unexpected contingencies");
                }
                return;
            }

            if (++_step == 2)
            {
                // the marketable combo filled, all its legs, so all the legs of the other combo were canceled
                if (_marketableExitTickets.Any(x => x.Status != OrderStatus.Filled) || _farExitTickets.Any(x => x.Status != OrderStatus.Canceled))
                {
                    throw new RegressionTestException("Expected the marketable exit to be filled and the far exit to be canceled");
                }

                if (Portfolio.Invested || Transactions.GetOpenOrders().Count != 0)
                {
                    throw new RegressionTestException("Expected no position nor open orders");
                }
            }
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_step < 2)
            {
                throw new RegressionTestException("Expected the contingent combo orders to be submitted and asserted");
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
            {"Total Orders", "9"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99311.8"},
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
            {"Total Fees", "$8.20"},
            {"Estimated Strategy Capacity", "$12000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAT67A|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "24.33%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "ecd9865c9fd95b98a8abb3fca6ddf42e"}
        };
    }
}
