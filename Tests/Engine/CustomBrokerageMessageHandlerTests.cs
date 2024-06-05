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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Python;
using QuantConnect.Statistics;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class CustomBrokerageMessageHandlerTests
    {
        [Test]
        public void PythonCustomBrokerageMessageHandlerCallsBase()
        {
            using (Py.GIL())
            {
                dynamic CustomBrokerageMessageHandler = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageMessageHandler(DefaultBrokerageMessageHandler):
    def __init__(self, algorithm):
        super().__init__(algorithm)
        self._algorithm = algorithm

    def handle_order(self, event_args):
        order = event_args.order
        if order.tag is None or not order.tag.isdigit():
            raise Exception(""Expected all new brokerage-side orders to have a valid tag"")

        # We will only process orders with even tags
        return int(order.tag) % 2 == 0
                ").GetAttr("CustomBrokerageMessageHandler");

                var algorithm = new AlgorithmStub();
                var model = new BrokerageMessageHandlerPythonWrapper(CustomBrokerageMessageHandler(algorithm));
                Assert.DoesNotThrow(() => model.HandleMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 1, "Event!")));
            }
        }

        [Test]
        public void RunPartialCustomBrokerageMessageHandlerRegressionAlgorithm([Values(Language.Python)] Language language)
        {
            // We expect only half of the orders to be processed
            var expectedOrdersCount = (CustomBacktestingBrokerage.MaxOrderCount / 2) + 1;

            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("CustomBrokerageSideOrderHandlingRegressionPartialAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, expectedOrdersCount.ToStringInvariant()},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "-10.771%"},
                    {"Drawdown", "0.200%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "-0.146%"},
                    {"Sharpe Ratio", "-5.186"},
                    {"Sortino Ratio", "-6.53"},
                    {"Probabilistic Sharpe Ratio", "24.692%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0.059"},
                    {"Beta", "-0.072"},
                    {"Annual Standard Deviation", "0.016"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "-8.629"},
                    {"Tracking Error", "0.239"},
                    {"Treynor Ratio", "1.154"},
                    {"Total Fees", "$50.00"},
                    {"Estimated Strategy Capacity", "$17000000.00"},
                    {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
                    {"Portfolio Turnover", "1.45%"}
                },
                language,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                setupHandler: nameof(CustomBacktestingSetupHandler));
        }

        [Test]
        public void RunCustomBrokerageMessageHandlerRegressionAlgorithm([Values(Language.CSharp, Language.Python)] Language language)
        {
            // We expect only half of the orders to be processed
            var expectedOrdersCount = (CustomBacktestingBrokerage.MaxOrderCount / 2) + 1;

            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("CustomBrokerageSideOrderHandlingRegressionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, expectedOrdersCount.ToStringInvariant()},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "-10.771%"},
                    {"Drawdown", "0.200%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "-0.146%"},
                    {"Sharpe Ratio", "-5.186"},
                    {"Sortino Ratio", "-6.53"},
                    {"Probabilistic Sharpe Ratio", "24.692%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0.059"},
                    {"Beta", "-0.072"},
                    {"Annual Standard Deviation", "0.016"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "-8.629"},
                    {"Tracking Error", "0.239"},
                    {"Treynor Ratio", "1.154"},
                    {"Total Fees", "$50.00"},
                    {"Estimated Strategy Capacity", "$17000000.00"},
                    {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
                    {"Portfolio Turnover", "1.45%"}
                },
                language,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                setupHandler: nameof(CustomBacktestingSetupHandler));
        }

        public class CustomBacktestingBrokerage : BacktestingBrokerage
        {
            public static readonly int MaxOrderCount = 100;

            private OrderDirection _direction = OrderDirection.Buy;

            private int _orderCount;

            public CustomBacktestingBrokerage(IAlgorithm algorithm) : base(algorithm)
            {
            }

            public override void Scan()
            {
                if (_orderCount <= MaxOrderCount)
                {
                    var quantity = 0m;
                    // Only orders with even numbers in the tags will be processed
                    if (_orderCount % 2 == 0)
                    {
                        quantity = _direction == OrderDirection.Buy ? 1 : -1;
                        // Switch direction
                        _direction = OrderDirection.Sell;
                    }

                    var marketOrder = new MarketOrder(Symbols.SPY, quantity, Algorithm.UtcTime, tag: _orderCount.ToStringInvariant());
                    marketOrder.Status = OrderStatus.New;
                    OnNewBrokerageOrderNotification(new NewBrokerageOrderNotificationEventArgs(marketOrder));
                    _orderCount++;
                }

                base.Scan();
            }
        }

        public class CustomBacktestingSetupHandler : BacktestingSetupHandler
        {
            public override IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm, out IBrokerageFactory factory)
            {
                factory = new BacktestingBrokerageFactory();
                var brokerage = new CustomBacktestingBrokerage(uninitializedAlgorithm);
                brokerage.NewBrokerageOrderNotification += (sender, e) =>
                {
                    if (uninitializedAlgorithm.BrokerageMessageHandler.HandleOrder(e) &&
                        uninitializedAlgorithm.GetOrAddUnrequestedSecurity(e.Order.Symbol, out _))
                    {
                        brokerage.PlaceOrder(e.Order);
                    }
                };

                return brokerage;
            }
        }
    }
}
