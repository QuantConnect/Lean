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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "63.657%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.677%"},
            {"Sharpe Ratio", "2.328"},
            {"Probabilistic Sharpe Ratio", "54.318%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.179"},
            {"Beta", "0.443"},
            {"Annual Standard Deviation", "0.182"},
            {"Annual Variance", "0.033"},
            {"Information Ratio", "-4.85"},
            {"Tracking Error", "0.193"},
            {"Treynor Ratio", "0.957"},
            {"Total Fees", "$7.83"},
            {"Fitness Score", "0.203"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "35.877"},
            {"Portfolio Turnover", "0.203"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "5a4566ceb1751ebd89fa054941174f5e"}
        };
    }
}
