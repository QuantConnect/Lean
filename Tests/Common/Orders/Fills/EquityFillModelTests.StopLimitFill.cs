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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public partial class EquityFillModelTests
    {
        // buy: the trigger bar closes above the limit, the next bar trades through it and closes above it
        [TestCase(100, 100, 100.25, 100.6, 100.8, 99.8, 100.6, 100.25, false)]
        // buy: the next bar opens below the limit, favorable gap
        [TestCase(100, 100, 100.25, 99.9, 100.8, 99.8, 100.6, 99.9, true)]
        // sell: the trigger bar closes below the limit, the next bar trades through it and closes below it
        [TestCase(-100, 100, 99.75, 99.4, 100.2, 99.2, 99.4, 99.75, false)]
        // sell: the next bar opens above the limit, favorable gap
        [TestCase(-100, 100, 99.75, 100.1, 100.2, 99.2, 99.4, 100.1, true)]
        public void StopLimitFillsAsLimitOrderOnBarsAfterTheTriggerBar(decimal quantity, decimal stopPrice, decimal limitPrice,
            decimal open, decimal high, decimal low, decimal close, decimal expectedFillPrice, bool favorableGap)
        {
            var model = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, quantity, stopPrice, limitPrice, Noon.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);
            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // the stop triggers but the bar closes on the wrong side of the limit, the low/high could predate the trigger
            var triggerBar = quantity > 0
                ? new TradeBar(Noon, Symbols.SPY, 99.5m, 101.5m, 99.5m, 101m, 100)
                : new TradeBar(Noon, Symbols.SPY, 100.5m, 100.5m, 98.5m, 99m, 100);
            TimeKeeper.SetUtcDateTime(triggerBar.EndTime.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(triggerBar);

            var fill = model.Fill(parameters).Single();

            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.IsTrue(order.StopTriggered);
            Assert.AreEqual(triggerBar.EndTime.ConvertToUtc(TimeZones.NewYork), order.StopTriggeredTime);

            // the next bar is entirely after the trigger, so it fills like a resting limit order
            var nextBar = new TradeBar(triggerBar.EndTime, Symbols.SPY, open, high, low, close, 100);
            TimeKeeper.SetUtcDateTime(nextBar.EndTime.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(nextBar);

            fill = model.Fill(parameters).Single();

            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expectedFillPrice, fill.FillPrice);
            Assert.AreEqual(favorableGap, fill.Message.StartsWith("Due to a favorable gap", StringComparison.InvariantCulture));
        }

        [TestCase(100, 100, 100.25)]
        [TestCase(-100, 100, 99.75)]
        public void StopLimitDoesNotFillUsingTheTriggerBarRange(decimal quantity, decimal stopPrice, decimal limitPrice)
        {
            var model = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, quantity, stopPrice, limitPrice, Noon.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);
            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // the bar trades through both the stop and the limit but closes on the wrong side of the limit
            var triggerBar = quantity > 0
                ? new TradeBar(Noon, Symbols.SPY, 99.5m, 101.5m, 99.5m, 101m, 100)
                : new TradeBar(Noon, Symbols.SPY, 100.5m, 100.5m, 98.5m, 99m, 100);
            TimeKeeper.SetUtcDateTime(triggerBar.EndTime.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(triggerBar);

            var fill = model.Fill(parameters).Single();

            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.IsTrue(order.StopTriggered);

            // scanning the trigger bar again, e.g. live mode, must not use its range either
            fill = model.Fill(parameters).Single();

            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(0, fill.FillQuantity);
        }
    }
}
