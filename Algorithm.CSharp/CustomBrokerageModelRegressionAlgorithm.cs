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

using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test we can specify a custom brokerage model, and override some of its methods
    /// </summary>
    public class CustomBrokerageModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _spyTicket;
        private OrderTicket _aigTicket;

        private bool _updateRequestSubmitted;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetBrokerageModel(new CustomBrokerageModel());
            AddEquity("SPY", Resolution.Daily);
            AddEquity("AIG", Resolution.Daily);

            _updateRequestSubmitted = false;

            if (BrokerageModel.DefaultMarkets[SecurityType.Equity] != Market.USA)
            {
                throw new Exception($"The default market for Equity should be {Market.USA}");
            }
            if (BrokerageModel.DefaultMarkets[SecurityType.Crypto] != Market.Binance)
            {
                throw new Exception($"The default market for Crypto should be {Market.Binance}");
            }
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                MarketOrder("SPY", 100.0);
                _aigTicket = MarketOrder("AIG", 100.0);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var ticket = Transactions.GetOrderTicket(orderEvent.OrderId);
            if (_updateRequestSubmitted == false)
            {
                var updateOrderFields = new UpdateOrderFields();
                updateOrderFields.Quantity = ticket.Quantity + 10;
                ticket.Update(updateOrderFields);
                _spyTicket = ticket;
                _updateRequestSubmitted = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var submitExpectedMessage = "BrokerageModel declared unable to submit order: [2] Information - Code:  - Symbol AIG can not be submitted";
            if (_aigTicket.SubmitRequest.Response.ErrorMessage != submitExpectedMessage)
            {
                throw new Exception($"Order with ID: {_aigTicket.OrderId} should not have submitted symbol AIG");
            }

            var updateExpectedMessage = "OrderID: 1 Information - Code:  - This order can not be updated";
            if (_spyTicket.UpdateRequests[0].Response.ErrorMessage != updateExpectedMessage)
            {
                throw new Exception($"Order with ID: {_spyTicket.OrderId} should have been updated");
            }
        }

        class CustomBrokerageModel : DefaultBrokerageModel
        {
            private static readonly IReadOnlyDictionary<SecurityType, string> _defaultMarketMap = new Dictionary<SecurityType, string>
            {
                {SecurityType.Equity, Market.USA},
                {SecurityType.Crypto, Market.Binance }
            }.ToReadOnlyDictionary();

            public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => _defaultMarketMap;

            public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
            {
                if (security.Symbol.Value == "AIG")
                {
                    message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "Symbol AIG can not be submitted");
                    return false;
                }

                message = null;
                return true;
            }

            public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Information, "", "This order can not be updated");
                return false;
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
        public long DataPoints => 53;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "19.594%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.245%"},
            {"Sharpe Ratio", "5.194"},
            {"Probabilistic Sharpe Ratio", "66.956%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.12"},
            {"Beta", "0.145"},
            {"Annual Standard Deviation", "0.032"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-9.54"},
            {"Tracking Error", "0.19"},
            {"Treynor Ratio", "1.156"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$4100000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.90%"},
            {"OrderListHash", "ddc47b6d41e85b84d8bb9cf1523e1829"}
        };
    }
}
