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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting contingent orders in a cash account: open orders reserve the cash they require, but the members
    /// of a one cancels other contingency don't reserve it twice, since at most one of them will fill, nor do the orders held
    /// waiting for their parent to fill. So we can submit a take profit and a stop loss for our whole position.
    /// </summary>
    public class OneCancelsOtherOrderCashAccountRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private List<OrderTicket> _bracketTickets;
        private List<OrderTicket> _exitTickets;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4);
            SetEndDate(2018, 4, 4);
            SetCash(10000);
            SetCash("BTC", 1m);

            SetBrokerageModel(BrokerageName.Default, AccountType.Cash);

            _symbol = AddCrypto("BTCUSD", Resolution.Minute, Market.Coinbase).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_exitTickets != null)
            {
                return;
            }

            var price = Securities[_symbol].Price;

            // selling all the BTC we hold, each of them requires the whole position
            _exitTickets = OneCancelsOtherOrder(new List<SubmitOrderRequest>
            {
                OrderFactory.LimitOrder(_symbol, -1, Math.Round(price * 1.002m, 2), tag: "Take Profit"),
                OrderFactory.StopMarketOrder(_symbol, -1, Math.Round(price * 0.998m, 2), tag: "Stop Loss")
            });

            // using all our cash to buy more, once filled we sell what we bought
            var quantity = Math.Round(9000 / price, 4);
            _bracketTickets = BracketOrder(_symbol, quantity, takeProfitPrice: Math.Round(price * 1.5m, 2), stopLossPrice: Math.Round(price * 0.5m, 2),
                limitPrice: Math.Round(price * 0.999m, 2));

            foreach (var ticket in _exitTickets.Concat(_bracketTickets))
            {
                if (ticket.Status != OrderStatus.Submitted)
                {
                    throw new RegressionTestException($"Expected the order to be submitted: {ticket}. {ticket.SubmitRequest.Response}");
                }
            }
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_exitTickets.Count(x => x.Status == OrderStatus.Filled) != 1 || _exitTickets.Count(x => x.Status == OrderStatus.Canceled) != 1)
            {
                throw new RegressionTestException($"Expected one exit to fill and the other to be canceled: {string.Join(" | ", _exitTickets)}");
            }

            if (_bracketTickets[0].Status != OrderStatus.Filled || _bracketTickets.Skip(1).Any(x => x.Contingency.IsWaitingForTrigger || x.Status == OrderStatus.Invalid))
            {
                throw new RegressionTestException($"Expected the bracket entry to be filled and its exits triggered: {string.Join(" | ", _bracketTickets)}");
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
        public long DataPoints => 2897;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 10;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "17296.00"},
            {"End Equity", "16638.25"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$43000.00"},
            {"Lowest Capacity Asset", "BTCUSD 2XR"},
            {"Portfolio Turnover", "97.76%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d90a481c0d453bc43c7db8a13cedb04b"}
        };
    }
}
