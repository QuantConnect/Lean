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
    /// Regression algorithm asserting that a short option position is auto exercised even when there is insufficient margin,
    /// but triggering a margin call for the underlying stock to cover the assignment.
    /// </summary>
    public class InsufficientBuyingPowerForAutomaticExerciseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _stock;
        private Symbol _option;

        private bool _stockBought;
        private bool _optionSold;
        private bool _optionAssigned;
        private bool _marginCallReceived;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            _stock = AddEquity("GOOG").Symbol;

            var contracts = OptionChain(_stock).ToList();
            _option = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Put)
                .OrderBy(c => c.ID.Date)
                .First(c => c.ID.StrikePrice == 800m);

            AddOptionContract(_option);
        }

        public override void OnData(Slice slice)
        {
            // We are done with buying
            if (_stockBought && _optionSold)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                // We'll use all our buying power to buy the stock, so when we then open a short put position,
                // the margin will not be enough to cover the automatic exercise
                SetHoldings(_stock, 1);
            }

            if (_stockBought && Securities[_option].Price != 0)
            {
                MarketOrder(_option, -2);
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            if (!_optionAssigned)
            {
                throw new RegressionTestException("Expected option to have been assigned before the margin call " +
                    "(which should have been triggered by the auto-exercise of the option with inssuficient margin).");
            }

            if (_marginCallReceived)
            {
                throw new RegressionTestException("Received multiple margin calls. Expected just one.");
            }

            var request = requests.Single();
            if (request.Symbol != _stock)
            {
                throw new RegressionTestException("Expected margin call for the stock, but got margin call for: " + request.Symbol);
            }

            _marginCallReceived = true;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            Debug($"{Time} :: {order.Id} - {order.Type} - {orderEvent.Symbol}: {orderEvent.Status} - {orderEvent.Quantity} shares at {orderEvent.FillPrice}");

            if (orderEvent.Status == OrderStatus.Filled)
            {
                if (orderEvent.Symbol == _stock)
                {
                    _stockBought = true;
                }
                else if (orderEvent.Symbol == _option)
                {
                    if (order.Type == OrderType.Market)
                    {
                        if (!_stockBought)
                        {
                            throw new RegressionTestException("Stock should have been bought first");
                        }

                        _optionSold = true;
                    }
                    else if (order.Type == OrderType.OptionExercise && orderEvent.IsAssignment)
                    {
                        if (!_optionSold)
                        {
                            throw new RegressionTestException("Option should have been sold first");
                        }

                        _optionAssigned = true;
                    }
                }
                else
                {
                    throw new RegressionTestException("Unexpected symbol: " + orderEvent.Symbol);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_stockBought)
            {
                throw new RegressionTestException("Stock was not bought");
            }

            if (!_optionSold)
            {
                throw new RegressionTestException("Option was not sold");
            }

            if (!_optionAssigned)
            {
                throw new RegressionTestException("Option was not assigned");
            }

            if (!_marginCallReceived)
            {
                throw new RegressionTestException("Margin call was not received");
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
        public long DataPoints => 2821;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

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
            {"Average Win", "8.96%"},
            {"Average Loss", "-1.95%"},
            {"Compounding Annual Return", "-67.963%"},
            {"Drawdown", "2.900%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "98248.35"},
            {"Net Profit", "-1.752%"},
            {"Sharpe Ratio", "-6.542"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "1.125%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "4.60"},
            {"Alpha", "-0.007"},
            {"Beta", "1.181"},
            {"Annual Standard Deviation", "0.036"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-1.422"},
            {"Tracking Error", "0.03"},
            {"Treynor Ratio", "-0.2"},
            {"Total Fees", "$3.30"},
            {"Estimated Strategy Capacity", "$2400000.00"},
            {"Lowest Capacity Asset", "GOOCV 305RBQ20WHPNQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "54.01%"},
            {"OrderListHash", "97d1c3373a72ec15038949710e7c7a62"}
        };
    }
}

