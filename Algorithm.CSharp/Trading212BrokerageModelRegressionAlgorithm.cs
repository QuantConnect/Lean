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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Exercises the Trading 212 brokerage model end to end through
    /// <see cref="QCAlgorithm.SetBrokerageModel(BrokerageName)"/>: a supported order fills, the
    /// long-only and unsupported-order rules reject with the model's message, and equity sale
    /// proceeds are available immediately.
    /// </summary>
    public class Trading212BrokerageModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private int _day;
        private OrderTicket _buyTicket;
        private OrderTicket _sellTicket;
        private OrderTicket _overSellTicket;
        private OrderTicket _sellFromFlatTicket;
        private OrderTicket _goodTilDateTicket;
        private OrderTicket _marketOnCloseTicket;
        private List<OrderTicket> _bracketTickets;
        private decimal _cashBeforeSell;
        private bool _sellFilled;
        private readonly Dictionary<int, string> _orderEventMessages = new();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 10, 21);
            SetCash(100000);

            // Selects the model through BrokerageModel.Create, which passes AccountType.Margin.
            SetBrokerageModel(BrokerageName.Trading212);

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_symbol))
            {
                return;
            }

            switch (++_day)
            {
                case 1:
                    // A supported market order fills on the next data point.
                    _buyTicket = MarketOrder(_symbol, 10, asynchronous: true);
                    break;

                case 2:
                    if (_buyTicket.Status != OrderStatus.Filled || Securities[_symbol].Holdings.Quantity != 10)
                    {
                        throw new RegressionTestException(
                            $"Expected the supported market order to fill 10 shares, but the status is {_buyTicket.Status} " +
                            $"and the holding is {Securities[_symbol].Holdings.Quantity}.");
                    }

                    // Long only and unsupported order rules reject synchronously with an Invalid ticket.
                    _overSellTicket = MarketOrder(_symbol, -11, asynchronous: true);
                    RequireInvalid(_overSellTicket, "long only");

                    var goodTilDate = new OrderProperties { TimeInForce = TimeInForce.GoodTilDate(Time.AddDays(7)) };
                    _goodTilDateTicket = LimitOrder(_symbol, 1, Securities[_symbol].Price * 1.1m, orderProperties: goodTilDate);
                    RequireInvalid(_goodTilDateTicket, "GoodTilDateTimeInForce");

                    // MarketOnClose is accepted, since Lean converts daily market orders placed intraday into it.
                    // It is cancelled straight away so the position in this algorithm is unchanged.
                    _marketOnCloseTicket = MarketOnCloseOrder(_symbol, 1);
                    if (_marketOnCloseTicket.Status == OrderStatus.Invalid)
                    {
                        throw new RegressionTestException("Expected the MarketOnClose order to be accepted, but it was Invalid.");
                    }
                    _marketOnCloseTicket.Cancel();

                    // Contingent orders (brackets) are rejected by the model.
                    var price = Securities[_symbol].Price;
                    _bracketTickets = BracketOrder(_symbol, 1, price * 1.1m, price * 0.9m, asynchronous: true);
                    foreach (var ticket in _bracketTickets)
                    {
                        RequireInvalid(ticket, "contingent orders");
                    }

                    _cashBeforeSell = Portfolio.CashBook[Currencies.USD].Amount;
                    _sellTicket = MarketOrder(_symbol, -10, asynchronous: true);
                    break;

                case 3:
                    if (_sellTicket.Status != OrderStatus.Filled || Securities[_symbol].Holdings.Quantity != 0)
                    {
                        throw new RegressionTestException(
                            $"Expected the supported sell to fill and close the position, but the status is {_sellTicket.Status} " +
                            $"and the holding is {Securities[_symbol].Holdings.Quantity}.");
                    }

                    // Trading 212 lets sale proceeds be used to buy more shares before settlement, so
                    // Lean settles them immediately and they are not held as unsettled cash.
                    if (Portfolio.UnsettledCash != 0)
                    {
                        throw new RegressionTestException(
                            $"Expected immediate equity settlement, but {Portfolio.UnsettledCash} is unsettled.");
                    }
                    if (Portfolio.CashBook[Currencies.USD].Amount <= _cashBeforeSell)
                    {
                        throw new RegressionTestException(
                            $"Expected the sale proceeds to be credited immediately, but the cash went from " +
                            $"{_cashBeforeSell} to {Portfolio.CashBook[Currencies.USD].Amount}.");
                    }
                    _sellFilled = true;

                    _sellFromFlatTicket = MarketOrder(_symbol, -1, asynchronous: true);
                    RequireInvalid(_sellFromFlatTicket, "long only");
                    break;

                case 4:
                    // Nothing else to submit; let the algorithm run to the end.
                    break;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!string.IsNullOrEmpty(orderEvent.Message))
            {
                _orderEventMessages[orderEvent.OrderId] = orderEvent.Message;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_sellFilled)
            {
                throw new RegressionTestException("The supported sell never filled.");
            }
            if (Securities[_symbol].Holdings.Quantity != 0)
            {
                throw new RegressionTestException($"Expected a flat position, but the holding is {Securities[_symbol].Holdings.Quantity}.");
            }

            RequireInvalid(_overSellTicket, "long only");
            RequireInvalid(_sellFromFlatTicket, "long only");
            RequireInvalid(_goodTilDateTicket, "GoodTilDateTimeInForce");
            if (_marketOnCloseTicket == null || _marketOnCloseTicket.Status != OrderStatus.Canceled)
            {
                throw new RegressionTestException($"Expected the MarketOnClose order to be cancelled, but it is {_marketOnCloseTicket?.Status}.");
            }
            if (_bracketTickets == null || _bracketTickets.Count == 0 || _bracketTickets.Any(ticket => ticket.Status != OrderStatus.Invalid))
            {
                throw new RegressionTestException("Expected every bracket order ticket to be Invalid.");
            }
        }

        private void RequireInvalid(OrderTicket ticket, string expectedMessagePart)
        {
            if (ticket == null)
            {
                throw new RegressionTestException($"Expected a rejected order mentioning '{expectedMessagePart}', but no ticket was returned.");
            }
            if (ticket.Status != OrderStatus.Invalid)
            {
                throw new RegressionTestException(
                    $"Expected an Invalid order mentioning '{expectedMessagePart}', but the status is {ticket.Status}.");
            }

            var message = _orderEventMessages.GetValueOrDefault(ticket.OrderId)
                ?? ticket.SubmitRequest.Response.ErrorMessage ?? string.Empty;
            if (!message.Contains(expectedMessagePart))
            {
                throw new RegressionTestException(
                    $"Expected the rejection message to contain '{expectedMessagePart}', but it is '{message}'.");
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
        public long DataPoints => 88;

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
            {"Average Win", "0.01%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.346%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100012.94"},
            {"Net Profit", "0.013%"},
            {"Sharpe Ratio", "-7.344"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "15.489%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.012"},
            {"Beta", "0.004"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-15.219"},
            {"Tracking Error", "0.118"},
            {"Treynor Ratio", "-1.191"},
            {"Total Fees", "$0.03"},
            {"Estimated Strategy Capacity", "$1200000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.21%"},
            {"Drawdown Recovery", "1"},
            {"OrderListHash", "563859fba6d7c9610a6741cd51048f90"}
        };
    }
}
