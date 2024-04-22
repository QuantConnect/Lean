
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

using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration on how to access order tickets right after placing an order.
    /// </summary>
    public class OrderTicketAssignmentDemoAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        private OrderTicket _ticket;

        private int _tradeCount;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;

            Consolidate(_symbol, TimeSpan.FromHours(1), (TradeBar bar) =>
            {
                // Reset _ticket to null on each new bar
                _ticket = null;
                _ticket = MarketOrder(_symbol, 1, asynchronous: true);
                Debug($"{Time}: Buy: Price {bar.Price}, orderId: {_ticket.OrderId}");
                _tradeCount++;
            });
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // We cannot access _ticket directly because it is assigned asynchronously:
            // this order event could be triggered before _ticket is assigned.
            var ticket = orderEvent.Ticket;
            if (ticket == null)
            {
                throw new Exception("Expected order ticket in order event to not be null");
            }
            if (orderEvent.Status == OrderStatus.Submitted && _ticket != null)
            {
                throw new Exception("Field _ticket not expected no be assigned on the first order event");
            }

            Debug(ticket.ToString());
        }

        public override void OnEndOfAlgorithm()
        {
            // Just checking that orders were placed
            if (!Portfolio.Invested || _tradeCount != Transactions.OrdersCount)
            {
                throw new Exception($"Expected the portfolio to have holdings and to have {_tradeCount} trades, but had {Transactions.OrdersCount}");
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
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "35"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "3.632%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100045.62"},
            {"Net Profit", "0.046%"},
            {"Sharpe Ratio", "4.618"},
            {"Sortino Ratio", "13.697"},
            {"Probabilistic Sharpe Ratio", "73.517%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.025"},
            {"Beta", "0.027"},
            {"Annual Standard Deviation", "0.006"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.991"},
            {"Tracking Error", "0.217"},
            {"Treynor Ratio", "1.042"},
            {"Total Fees", "$34.00"},
            {"Estimated Strategy Capacity", "$36000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.99%"},
            {"OrderListHash", "ac3803a8abaf1d1e77e009c418ba68e2"}
        };
    }
}
