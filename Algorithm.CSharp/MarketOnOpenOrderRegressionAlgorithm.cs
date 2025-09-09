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

using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm demonstrating the use of MarketOnOpen orders.
    /// </summary>
    public class MarketOnOpenOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private List<OrderTicket> _tickets = new();

        protected virtual bool AsynchronousOrders => false;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 03, 01);
            SetEndDate(2021, 03, 03);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Hour).Symbol;

            Schedule.On(DateRules.Tomorrow, TimeRules.Midnight, () =>
            {
                _tickets.Add(MarketOnOpenOrder(_symbol, 1, asynchronous: AsynchronousOrders));

                var marketOrderTicket = MarketOrder(_symbol, 1, asynchronous: AsynchronousOrders);
                _tickets.Add(marketOrderTicket);

                if (marketOrderTicket.OrderType != OrderType.MarketOnOpen)
                {
                    throw new RegressionTestException($"Expected order type to be MarketOnOpen, but was {marketOrderTicket.OrderType}");
                }

                foreach (var ticket in _tickets)
                {
                    if (ticket.Status != OrderStatus.New && ticket.Status != OrderStatus.Submitted)
                    {
                        throw new RegressionTestException($"Expected tickets status to be New or Submitted, but one was {ticket.Status}");
                    }
                }
            });
        }

        public override void OnEndOfAlgorithm()
        {
            if (_tickets.Count == 0)
            {
                throw new RegressionTestException("Expected to have submitted orders, but did not");
            }

            foreach (var ticket in _tickets)
            {
                if (ticket.Status != OrderStatus.Filled)
                {
                    throw new RegressionTestException($"Expected tickets status to be Filled, but one was {ticket.Status}");
                }

                if (ticket.SubmitRequest.Asynchronous != AsynchronousOrders)
                {
                    throw new RegressionTestException("Expected all orders to have the same asynchronous flag as the algorithm.");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 50;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-2.547%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99981.16"},
            {"Net Profit", "-0.019%"},
            {"Sharpe Ratio", "-147.421"},
            {"Sortino Ratio", "-147.421"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.023"},
            {"Beta", "0.003"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "14.89"},
            {"Tracking Error", "0.061"},
            {"Treynor Ratio", "-9.006"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$1400000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.26%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "56a7b3ac0475f32f06c567b494741b0d"}
        };
    }
}
