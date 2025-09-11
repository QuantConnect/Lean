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
using QuantConnect.Data.Market;
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
        private static AlpacaBrokerageModel _brokerageModel = new AlpacaBrokerageModel();

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

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(shouldSubmit));
        }

        [TestCase(8, 0, true, Description = "8 AM - valid submission")]
        [TestCase(12, 0, false, Description = "12 PM - invalid submission")]
        [TestCase(15, 30, false, Description = "3:30 PM - invalid submission")]
        [TestCase(15, 59, false, Description = "15:59 PM - invalid submission")]
        [TestCase(17, 0, false, Description = "5 PM - valid submission")]
        [TestCase(19, 0, true, Description = "19 PM - valid submission")]
        [TestCase(19, 1, true, Description = "19 PM - valid submission")]
        [TestCase(21, 0, true, Description = "9 PM - valid submission")]
        public void CanSubmitMarketOnOpen(int hourOfDay, int minuteOfDay, bool shouldSubmit)
        {
            var symbol = Symbols.SPY;
            var algorithm = new AlgorithmStub();
            algorithm.SetStartDate(2025, 04, 30);

            var security = algorithm.AddSecurity(symbol.ID.SecurityType, symbol.ID.Symbol);
            algorithm.SetFinishedWarmingUp();
            security.Update([new Tick(algorithm.Time, symbol, string.Empty, string.Empty, 10m, 550m)], typeof(TradeBar));

            // Set algorithm time to the given hour
            var targetTime = algorithm.Time.Date.AddHours(hourOfDay).AddMinutes(minuteOfDay);
            algorithm.SetDateTime(targetTime.ConvertToUtc(algorithm.TimeZone));

            var order = new MarketOnOpenOrder(security.Symbol, 1, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(shouldSubmit));
        }
    }
}
