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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Tests.Common.Securities.Forex
{
    public class ForexTransactionModelTests
    {
        [Test]
        public void PerformsMarketFillBuy()
        {
            var model = new ForexTransactionModel();
            var order = new MarketOrder("USDJPY", 100, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101.123m));

            var fill = model.MarketFill(security, order);

            var slip = model.GetSlippageApproximation(security, order);

            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price + slip, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }
        [Test]
        public void PerformsMarketFillSell()
        {
            var model = new ForexTransactionModel();
            var order = new MarketOrder("USDJPY", -100, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101.123m));

            var fill = model.MarketFill(security, order);

            var slip = model.GetSlippageApproximation(security, order);

            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price - slip, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }

        [Test]
        public void PerformsLimitFillBuy()
        {
            var model = new ForexTransactionModel();
            var order = new LimitOrder("USDJPY", 100, 101.5m, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);
            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 102m));

            var fill = model.LimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 1.123m));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }

        [Test]
        public void PerformsLimitFillSell()
        {
            var model = new ForexTransactionModel();
            var order = new LimitOrder("USDJPY", -100, 101.5m, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);
            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101m));

            var fill = model.LimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101.623m));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }

        [Test]
        public void PerformsStopLimitFillBuy()
        {
            var model = new ForexTransactionModel();
            var order = new StopLimitOrder("USDJPY", 100, 101.5m, 101.75m, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);
            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 100m));

            var fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 102m));

            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }

        [Test]
        public void PerformsStopLimitFillSell()
        {
            var model = new ForexTransactionModel();
            var order = new StopLimitOrder("USDJPY", -100, 101.75m, 101.50m, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);
            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 102m));

            var fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101m));

            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }

        [Test]
        public void PerformsStopMarketFillBuy()
        {
            var model = new ForexTransactionModel();
            var order = new StopMarketOrder("USDJPY", 100, 101.5m, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);
            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101m));

            var fill = model.StopMarketFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 102.5m));

            fill = model.StopMarketFill(security, order);

            var slip = model.GetSlippageApproximation(security, order);

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(security.Price + slip, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }

        [Test]
        public void PerformsStopMarketFillSell()
        {
            var model = new ForexTransactionModel();
            var order = new StopMarketOrder("USDJPY", -100, 101.5m, DateTime.Now, type: SecurityType.Forex);
            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "USDJPY", Resolution.Minute, true, true, true, true, false, 0);
            var security = new Security(config, 1);
            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 102m));

            var fill = model.StopMarketFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(OrderStatus.None, order.Status);

            security.SetMarketPrice(DateTime.Now, new IndicatorDataPoint("USDJPY", DateTime.Now, 101m));

            fill = model.StopMarketFill(security, order);

            var slip = model.GetSlippageApproximation(security, order);

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(security.Price - slip, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(OrderStatus.Filled, order.Status);
        }
    }
}
