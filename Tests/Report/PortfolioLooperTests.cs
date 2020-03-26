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
    }
}
