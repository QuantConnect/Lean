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
    /// Basic algorithm demonstrating the use of a MarketOnClose order
    /// </summary>
    public class MarketOnCloseOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private OrderTicket _ticket;

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

            Schedule.On(DateRules.Tomorrow, TimeRules.Noon, () =>
            {
                _ticket = MarketOnCloseOrder(_symbol, 1, asynchronous: AsynchronousOrders);
                if (_ticket.Status != OrderStatus.New && _ticket.Status != OrderStatus.Submitted)
                {
                    throw new RegressionTestException($"Expected the MarketOnClose order to be New or Submitted, instead found {_ticket.Status}");
                }
            });
        }

        public override void OnEndOfAlgorithm()
        {
            if (_ticket == null)
            {
                throw new RegressionTestException("Expected to have placed a MarketOnClose order");
            }

            if (_ticket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Expected the MarketOnClose order to be filled, instead found {_ticket.Status}");
            }

            if (_ticket.SubmitRequest.Asynchronous != AsynchronousOrders)
            {
                throw new RegressionTestException("Expected all orders to have the same asynchronous flag as the algorithm.");
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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.832%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99993.90"},
            {"Net Profit", "-0.006%"},
            {"Sharpe Ratio", "-22.06"},
            {"Sortino Ratio", "-22.06"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.003"},
            {"Beta", "0.008"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "15.221"},
            {"Tracking Error", "0.061"},
            {"Treynor Ratio", "-1.348"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$53000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.13%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "1add16936335a9c85b72eed80dcacb39"}
        };
    }
}
