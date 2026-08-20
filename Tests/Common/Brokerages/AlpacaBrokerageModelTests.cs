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

using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class AlpacaBrokerageModelTests
    {
        private static IEnumerable<TestCaseData> OrderOusideRegularHoursTestCases
        {
            get
            {
                yield return new(OrderType.Market, TimeInForce.Day, false);
                yield return new(OrderType.Market, TimeInForce.GoodTilCanceled, false);
                yield return new(OrderType.Market, TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7)), false);

                yield return new(OrderType.StopMarket, TimeInForce.Day, false);
                yield return new(OrderType.StopMarket, TimeInForce.GoodTilCanceled, false);
                yield return new(OrderType.StopMarket, TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7)), false);

                yield return new(OrderType.StopLimit, TimeInForce.Day, false);
                yield return new(OrderType.StopLimit, TimeInForce.GoodTilCanceled, false);
                yield return new(OrderType.StopLimit, TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7)), false);

                yield return new(OrderType.Limit, TimeInForce.Day, true); // The only supported case
                yield return new(OrderType.Limit, TimeInForce.GoodTilCanceled, false);
                yield return new(OrderType.Limit, TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7)), false);
            }
        }

        [TestCaseSource(nameof(OrderOusideRegularHoursTestCases))]
        public void CanSubmitOrderWhenOutsideRegularTradingHours(OrderType orderType, TimeInForce timeInForce, bool shouldSubmit)
        {
            var security = TestsHelpers.GetSecurity(symbol: "AAPL", securityType: SecurityType.Equity, market: Market.USA);
            var symbol = security.Symbol;

            var orderProperties = new AlpacaOrderProperties()
            {
                OutsideRegularTradingHours = true,
                TimeInForce = timeInForce
            };
            Order order = orderType switch
            {
                OrderType.Market => new MarketOrder(symbol, 1, DateTime.UtcNow, properties: orderProperties),
                OrderType.Limit => new LimitOrder(symbol, 1, 100m, DateTime.UtcNow, properties: orderProperties),
                OrderType.StopMarket => new StopMarketOrder(symbol, 1, 100m, DateTime.UtcNow, properties: orderProperties),
                OrderType.StopLimit => new StopLimitOrder(symbol, 1, 100m, 90m, DateTime.UtcNow, properties: orderProperties),
                _ => throw new ArgumentException($"Unsupported order type: {orderType}"),
            };

            var brokerageModel = new AlpacaBrokerageModel();
            var canSubmit = brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(shouldSubmit));
        }

        [TestCase(GroupExecutionType.Combo, true)]
        [TestCase(GroupExecutionType.OneCancelsTheOther, true)]
        // an execution type this model has not opted in to must still be refused, so a blanket "true" fails here
        [TestCase((GroupExecutionType)99, false)]
        public void SupportsGroupExecution(GroupExecutionType groupExecutionType, bool expected)
        {
            var brokerageModel = new AlpacaBrokerageModel();
            Assert.That(brokerageModel.SupportsGroupExecution(groupExecutionType), Is.EqualTo(expected));
        }

        [Test]
        public void CanSubmitValidOneCancelsTheOtherGroup()
        {
            var groupOrderManager = new GroupOrderManager(1, legCount: 2, quantity: -100) { ExecutionType = GroupExecutionType.OneCancelsTheOther };
            var security = TestsHelpers.GetSecurity(symbol: Symbols.AAPL.Value, securityType: SecurityType.Equity, market: Market.USA);
            var order = new LimitOrder(Symbols.AAPL, -100, 220m, DateTime.UtcNow) { GroupOrderManager = groupOrderManager };

            Assert.IsTrue(new AlpacaBrokerageModel().CanSubmitOrder(security, order, out var message));
            Assert.IsNull(message);
        }

        private static IEnumerable<TestCaseData> InvalidOneCancelsTheOtherLegTestCases
        {
            get
            {
                // Alpaca's group is always exactly a take profit plus a stop loss
                yield return new TestCaseData(3, SecurityType.Equity, OrderType.Limit, -100m, TimeInForce.GoodTilCanceled)
                    .SetName("RejectsMoreThanTwoLegs");

                // US equities only: crypto and options do not support the OCO order class
                yield return new TestCaseData(2, SecurityType.Crypto, OrderType.Limit, -100m, TimeInForce.GoodTilCanceled)
                    .SetName("RejectsNonEquitySecurityType");

                // only a Limit take profit and a StopMarket stop loss are mapped
                yield return new TestCaseData(2, SecurityType.Equity, OrderType.StopLimit, -100m, TimeInForce.GoodTilCanceled)
                    .SetName("RejectsUnsupportedLegOrderType");

                // the group's direction comes from its first leg, so a leg facing the other way is a mixed-side group
                yield return new TestCaseData(2, SecurityType.Equity, OrderType.Limit, 100m, TimeInForce.GoodTilCanceled)
                    .SetName("RejectsLegOnTheOppositeSide");

                // Alpaca only accepts a day or good til canceled time in force for these groups
                yield return new TestCaseData(2, SecurityType.Equity, OrderType.Limit, -100m, TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7)))
                    .SetName("RejectsUnsupportedTimeInForce");
            }
        }

        [TestCaseSource(nameof(InvalidOneCancelsTheOtherLegTestCases))]
        public void CannotSubmitInvalidOneCancelsTheOtherLeg(int legCount, SecurityType securityType, OrderType orderType,
            decimal legQuantity, TimeInForce timeInForce)
        {
            // the group quantity stays negative, so a positive leg quantity is a leg on the opposite side
            var groupOrderManager = new GroupOrderManager(1, legCount, quantity: -100) { ExecutionType = GroupExecutionType.OneCancelsTheOther };
            var symbol = securityType == SecurityType.Crypto ? Symbols.BTCUSD : Symbols.AAPL;
            var security = TestsHelpers.GetSecurity(symbol: symbol.Value, securityType: securityType,
                market: securityType == SecurityType.Crypto ? Market.Coinbase : Market.USA);
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };

            Order order = orderType switch
            {
                OrderType.StopLimit => new StopLimitOrder(symbol, legQuantity, 190m, 189m, DateTime.UtcNow, properties: orderProperties),
                _ => new LimitOrder(symbol, legQuantity, 220m, DateTime.UtcNow, properties: orderProperties)
            };
            order.GroupOrderManager = groupOrderManager;

            Assert.IsFalse(new AlpacaBrokerageModel().CanSubmitOrder(security, order, out var message));
            Assert.IsNotNull(message);
        }
    }
}
