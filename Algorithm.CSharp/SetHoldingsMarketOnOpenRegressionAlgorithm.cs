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
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm reproduces GH issue 3781
    /// </summary>
    public class SetHoldingsMarketOnOpenRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            AddEquity("SPY");
            _aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                if (Securities[_aapl].HasData)
                {
                    SetHoldings(_aapl, 1);
                    var orderTicket = Transactions.GetOpenOrderTickets(_aapl).Single();
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                var orderTickets = Transactions.GetOpenOrderTickets(_aapl).Single();
            }
            else
            {
                // should be filled
                var orderTickets = Transactions.GetOpenOrderTickets(_aapl).ToList(ticket => ticket);
                if (!orderTickets.IsNullOrEmpty())
                {
                    throw new Exception($"We don't expect any open order tickets: {orderTickets[0]}");
                }
            }

            if (orderEvent.OrderId > 1)
            {
                throw new Exception($"We only expect 1 order to be placed: {orderEvent}");
            }
            Debug($"OnOrderEvent: {orderEvent}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5508;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "61.726%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100660.71"},
            {"Net Profit", "0.661%"},
            {"Sharpe Ratio", "2.515"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "54.049%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.772"},
            {"Beta", "0.66"},
            {"Annual Standard Deviation", "0.212"},
            {"Annual Variance", "0.045"},
            {"Information Ratio", "-8.483"},
            {"Tracking Error", "0.17"},
            {"Treynor Ratio", "0.806"},
            {"Total Fees", "$32.32"},
            {"Estimated Strategy Capacity", "$190000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "20.39%"},
            {"OrderListHash", "62589535089e12bd3b4bb5c0b7b4c91b"}
        };
    }
}
