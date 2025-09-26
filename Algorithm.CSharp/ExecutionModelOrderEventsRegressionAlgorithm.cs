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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating how to get order events in custom execution models
    /// and asserting that they match the algorithm's order events.
    /// </summary>
    public class ExecutionModelOrderEventsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly List<OrderEvent> _orderEvents = new();
        private CustomImmediateExecutionModel _executionModel;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Resolution.Daily));

            _executionModel = new CustomImmediateExecutionModel();
            SetExecution(_executionModel);
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            _orderEvents.Add(orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_executionModel.OrderEvents.Count != _orderEvents.Count)
            {
                throw new RegressionTestException($"Order events count mismatch. Execution model: {_executionModel.OrderEvents.Count}, Algorithm: {_orderEvents.Count}");
            }

            for (int i = 0; i < _orderEvents.Count; i++)
            {
                var modelEvent = _executionModel.OrderEvents[i];
                var algoEvent = _orderEvents[i];

                if (modelEvent.Id != algoEvent.Id ||
                    modelEvent.OrderId != algoEvent.OrderId ||
                    modelEvent.Status != algoEvent.Status)
                {
                    throw new RegressionTestException($"Order event mismatch at index {i}. Execution model: {_executionModel.OrderEvents[i]}, Algorithm: {_orderEvents[i]}");
                }
            }
        }

        private class CustomImmediateExecutionModel : ExecutionModel
        {
            private readonly PortfolioTargetCollection _targetsCollection = new PortfolioTargetCollection();

            private readonly Dictionary<int, OrderTicket> _orderTickets = new();

            public List<OrderEvent> OrderEvents { get; } = new();

            public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                _targetsCollection.AddRange(targets);
                if (!_targetsCollection.IsEmpty)
                {
                    foreach (var target in _targetsCollection.OrderByMarginImpact(algorithm))
                    {
                        var security = algorithm.Securities[target.Symbol];

                        // calculate remaining quantity to be ordered
                        var quantity = OrderSizing.GetUnorderedQuantity(algorithm, target, security, true);

                        if (quantity != 0 &&
                            security.BuyingPowerModel.AboveMinimumOrderMarginPortfolioPercentage(security, quantity,
                                algorithm.Portfolio, algorithm.Settings.MinimumOrderMarginPortfolioPercentage))
                        {
                            var ticket = algorithm.MarketOrder(security, quantity, asynchronous: true, tag: target.Tag);
                            _orderTickets[ticket.OrderId] = ticket;
                        }
                    }

                    _targetsCollection.ClearFulfilled(algorithm);
                }
            }

            public override void OnOrderEvent(QCAlgorithm algorithm, OrderEvent orderEvent)
            {
                algorithm.Log($"{algorithm.Time} - Order event received: {orderEvent}");

                // This method will get events for all orders, but if we save the tickets in Execute we can filter
                // to process events for orders placed by this model
                if (_orderTickets.TryGetValue(orderEvent.OrderId, out var ticket))
                {
                    if (orderEvent.Status.IsFill())
                    {
                        algorithm.Debug($"Purchased Stock: {orderEvent.Symbol}");
                    }

                    if (orderEvent.Status.IsClosed())
                    {
                        // Once the order is closed we can remove it from our tracking dictionary
                        _orderTickets.Remove(orderEvent.OrderId);
                    }
                }

                OrderEvents.Add(orderEvent);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

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
            {"Average Win", "0%"},
            {"Average Loss", "-1.01%"},
            {"Compounding Annual Return", "261.134%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "101655.30"},
            {"Net Profit", "1.655%"},
            {"Sharpe Ratio", "8.472"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "66.840%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.091"},
            {"Beta", "1.006"},
            {"Annual Standard Deviation", "0.224"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "-33.445"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "1.885"},
            {"Total Fees", "$10.32"},
            {"Estimated Strategy Capacity", "$27000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.86%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "f209ed42701b0419858e0100595b40c0"}
        };
    }
}
