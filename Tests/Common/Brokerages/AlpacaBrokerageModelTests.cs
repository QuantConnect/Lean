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
    }
}
