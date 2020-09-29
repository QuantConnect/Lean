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

using Deedle;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Report;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class PortfolioLooperTests
    {
        [Test]
        public void EmptyEquitySeriesDoesNotCrash()
        {
            var equityPoints = new SortedList<DateTime, double>
            {
                { new DateTime(2019, 1, 3, 5, 0, 5), 100000 }
            };
            var series = new Series<DateTime, double>(equityPoints);
            var order = new MarketOrder(Symbols.SPY, 1m, new DateTime(2019, 1, 3, 5, 0, 0));

            // Force an order ID >= 1 on the order, otherwise the test will fail
            // because the order will be filtered out.
            order.GetType().GetProperty("Id").SetValue(order, 1);

            var orders = new List<Order>
            {
                order
            };

            Assert.DoesNotThrow(() => PortfolioLooper.FromOrders(series, orders).ToList());
        }

        [TestCase(OrderType.Market, 0, 0)]
        [TestCase(OrderType.Limit, 0, 80000)]
        [TestCase(OrderType.StopLimit, 80000, 80000)]
        [TestCase(OrderType.StopMarket, 80000, 0)]
        [TestCase(OrderType.MarketOnOpen, 0, 0, true)]
        [TestCase(OrderType.MarketOnClose, 0, 0, true)]
        public void OrderProcessedInLooper(OrderType orderType, double stopPrice, double limitPrice, bool hasNullLastFillTime = false)
        {
            var equityPoints = new SortedList<DateTime, double>
            {
                { new DateTime(2019, 1, 3, 5, 0, 5), 100000 },
                { new DateTime(2019, 1, 4, 5, 0, 5), 90000 },
            };

            var series = new Series<DateTime, double>(equityPoints);
            var entryOrder = Order.CreateOrder(new SubmitOrderRequest(
                orderType,
                SecurityType.Equity,
                Symbols.SPY,
                1,
                (decimal)stopPrice,
                (decimal)limitPrice,
                new DateTime(2019, 1, 3, 5, 0, 5),
                string.Empty
            ));
            var exitOrder = Order.CreateOrder(new SubmitOrderRequest(
                orderType,
                SecurityType.Equity,
                Symbols.SPY,
                -1,
                (decimal)stopPrice,
                (decimal)limitPrice,
                new DateTime(2019, 1, 4, 5, 0, 5),
                string.Empty
            ));

            if (!hasNullLastFillTime)
            {
                entryOrder.LastFillTime = new DateTime(2019, 1, 3, 5, 0, 5);
                exitOrder.LastFillTime = new DateTime(2019, 1, 4, 5, 0, 5);
            }

            entryOrder.GetType().GetProperty("Id").SetValue(entryOrder, 1);
            entryOrder.GetType().GetProperty("Price").SetValue(entryOrder, 100000m);
            Order marketOnFillOrder = null;
            if (hasNullLastFillTime)
            {
                marketOnFillOrder = entryOrder.Clone();
                marketOnFillOrder.GetType().GetProperty("Status").SetValue(marketOnFillOrder, OrderStatus.Filled);
                marketOnFillOrder.GetType().GetProperty("Time").SetValue(marketOnFillOrder, new DateTime(2019, 1, 3, 6, 0 ,5));
            }
            exitOrder.GetType().GetProperty("Id").SetValue(exitOrder, 2);
            exitOrder.GetType().GetProperty("Price").SetValue(exitOrder, 80000m);
            exitOrder.GetType().GetProperty("Status").SetValue(exitOrder, OrderStatus.Filled);

            var orders = new[] { entryOrder, marketOnFillOrder, exitOrder }.Where(x => x != null);

            var looper = PortfolioLooper.FromOrders(series, orders);
            var pointInTimePortfolio = looper.ToList();

            Assert.AreEqual(3, pointInTimePortfolio.Count);
            Assert.AreEqual(100000, pointInTimePortfolio[0].TotalPortfolioValue);
            Assert.AreEqual(80000, pointInTimePortfolio[1].TotalPortfolioValue);
            Assert.AreEqual(80000, pointInTimePortfolio[2].TotalPortfolioValue);
        }
    }
}
