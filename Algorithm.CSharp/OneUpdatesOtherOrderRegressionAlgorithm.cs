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
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of the <see cref="QCAlgorithm.OneUpdatesOtherOrder"/> helper method (OUO):
    /// a partial fill of an order reduces the remaining quantity of its siblings proportionally, which are canceled once it completely fills.
    /// A custom fill model is used to partially fill limit orders.
    /// </summary>
    public class OneUpdatesOtherOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private OrderTicket _takeProfit;
        private OrderTicket _stopLoss;
        private readonly List<decimal> _stopLossQuantities = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            var equity = AddEquity("SPY", Resolution.Minute);
            equity.SetFillModel(new PartialLimitFillModel());
            _symbol = equity.Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_takeProfit != null)
            {
                // the sibling quantity is reduced by the brokerage right after each partial fill
                if (_stopLossQuantities.Count == 0 || _stopLossQuantities[^1] != _stopLoss.Quantity)
                {
                    _stopLossQuantities.Add(_stopLoss.Quantity);
                }
                return;
            }

            MarketOrder(_symbol, 100);

            var price = Securities[_symbol].Price;
            var tickets = OneUpdatesOtherOrder(new List<SubmitOrderRequest>
            {
                OrderFactory.LimitOrder(_symbol, -100, Math.Round(price * 1.001m, 2), tag: "Take Profit"),
                // twice the size so we can assert it's reduced proportionally
                OrderFactory.StopMarketOrder(_symbol, -200, Math.Round(price * 0.9m, 2), tag: "Stop Loss")
            });
            _takeProfit = tickets[0];
            _stopLoss = tickets[1];

            if (tickets.Any(x => x.Contingency.Links.Single().Type != ContingencyType.OneUpdatesOther || x.Contingency.IsWaitingForTrigger))
            {
                throw new RegressionTestException("Unexpected contingencies");
            }
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_takeProfit.Status != OrderStatus.Filled || _stopLoss.Status != OrderStatus.Canceled)
            {
                throw new RegressionTestException($"Expected the take profit to be filled and the stop loss canceled: {_takeProfit} | {_stopLoss}");
            }

            var partialFills = _takeProfit.OrderEvents.Count(x => x.Status == OrderStatus.PartiallyFilled);
            if (partialFills != 2)
            {
                throw new RegressionTestException($"Expected 2 partial fills but got {partialFills}");
            }

            // 40 out of 100 filled => 200 * 60 / 100 = 120. Then 40 out of 60 remaining filled => 120 * 20 / 60 = 40
            var expectedQuantities = new[] { -200m, -120m, -40m };
            if (!_stopLossQuantities.SequenceEqual(expectedQuantities))
            {
                throw new RegressionTestException($"Unexpected stop loss quantities: {string.Join(",", _stopLossQuantities)}");
            }

            if (Portfolio.Invested || Transactions.GetOpenOrders().Count != 0)
            {
                throw new RegressionTestException("Expected the position to be closed and no open orders");
            }
        }

        /// <summary>
        /// Fill model which fills limit orders in chunks of 40 shares
        /// </summary>
        private class PartialLimitFillModel : FillModel
        {
            private readonly Dictionary<int, decimal> _absoluteRemainingByOrderId = new();

            public override OrderEvent LimitFill(Security asset, LimitOrder order)
            {
                var fill = base.LimitFill(asset, order);
                if (fill.Status != OrderStatus.Filled)
                {
                    return fill;
                }

                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out var absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                }

                if (absoluteRemaining <= 40)
                {
                    fill.FillQuantity = Math.Sign(order.Quantity) * absoluteRemaining;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    fill.FillQuantity = Math.Sign(order.Quantity) * 40;
                    fill.Status = OrderStatus.PartiallyFilled;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining - 40;
                }
                return fill;
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
        public long DataPoints => 3943;

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
            {"Total Orders", "3"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.929%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100011.83"},
            {"Net Profit", "0.012%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$16000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.79%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "08b9360bf7365db81d43a86f81dfa919"}
        };
    }
}
