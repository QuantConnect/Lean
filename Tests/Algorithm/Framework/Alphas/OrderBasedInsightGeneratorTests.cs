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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class OrderBasedInsightGeneratorTests
    {
        private Security _security;

        [SetUp]
        public void SetUp()
        {
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true, true, false);
            _security = new Security(
                exchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void GeneratedPeriodAndCloseTimeAreSet(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();
            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                    1,
                    Symbols.SPY,
                    new DateTime(2013, 1, 1),
                    OrderStatus.Filled,
                    direction,
                    1,
                    direction == OrderDirection.Buy ? 1 : -1,
                    OrderFee.Zero
                ),
                new SecurityHolding(_security, new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol)));

            Assert.AreEqual(new DateTime(2013, 1, 1), insight.GeneratedTimeUtc);
            Assert.AreEqual(Time.EndOfTime, insight.CloseTimeUtc);
            Assert.AreEqual(Time.EndOfTimeTimeSpan, insight.Period);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void NoExistingHoldings(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();
            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                    1,
                    Symbols.SPY,
                    new DateTime(2013, 1, 1),
                    OrderStatus.Filled,
                    direction,
                    1,
                    direction == OrderDirection.Buy ? 1 : -1,
                    OrderFee.Zero
                ),
                new SecurityHolding(_security, new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol)));

            Assert.AreEqual(1, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Up : InsightDirection.Down, insight.Direction);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void ChangeMarketSide(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();

            var holding =
                new SecurityHolding(_security, new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol));
            holding.SetHoldings(1, direction == OrderDirection.Buy ? -1 : 1);

            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                    1,
                    Symbols.SPY,
                    new DateTime(2013, 1, 1),
                    OrderStatus.Filled,
                    direction,
                    1,
                    direction == OrderDirection.Buy ? 2 : -2,
                    OrderFee.Zero
                ), holding);

            Assert.AreEqual(1, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Up : InsightDirection.Down, insight.Direction);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void ClosePosition(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();

            var holding =
                new SecurityHolding(_security, new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol));
            holding.SetHoldings(1, direction == OrderDirection.Buy ? -1 : 1);

            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? 1 : -1,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(1, insight.Confidence);
            Assert.AreEqual(InsightDirection.Flat, insight.Direction);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void IncreasePosition(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();

            var holding =
                new SecurityHolding(_security, new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol));
            holding.SetHoldings(1, direction == OrderDirection.Buy ? 1 : -1);

            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? 1 : -1,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(1, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Up : InsightDirection.Down, insight.Direction);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void ReducePosition(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();

            var holding =
                new SecurityHolding(_security, new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol));
            holding.SetHoldings(1, direction == OrderDirection.Buy ? -2 : 2);

            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? 1 : -1,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(0.5, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Down : InsightDirection.Up, insight.Direction);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void ReducePositionWithExistingInsight(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();

            var holding = new SecurityHolding(_security,
                new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol));

            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? 2 : -2,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(1, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Up : InsightDirection.Down, insight.Direction);

            holding.SetHoldings(1, direction == OrderDirection.Buy ? 2 : -2);
            insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? -1 : 1,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(0.5, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Up : InsightDirection.Down, insight.Direction);

            holding.SetHoldings(1, direction == OrderDirection.Buy ? 1 : -1);
            insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? -0.5m: 0.5m,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(0.25, insight.Confidence);
            Assert.AreEqual(direction == OrderDirection.Buy
                ? InsightDirection.Up : InsightDirection.Down, insight.Direction);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void ExistingInsightCloseTimeIsUpdated(OrderDirection direction)
        {
            var insightGenerator = new OrderBasedInsightGenerator();

            var holding = new SecurityHolding(_security,
                new IdentityCurrencyConverter(_security.QuoteCurrency.Symbol));

            var insight = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2013, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? 2 : -2,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(new DateTime(2013, 1, 1), insight.GeneratedTimeUtc);

            holding.SetHoldings(1, direction == OrderDirection.Buy ? 2 : -2);
            var insight2 = insightGenerator.GenerateInsightFromFill(new OrderEvent(
                1,
                Symbols.SPY,
                new DateTime(2015, 1, 1),
                OrderStatus.Filled,
                direction,
                1,
                direction == OrderDirection.Buy ? -1 : 1,
                OrderFee.Zero
            ), holding);

            Assert.AreEqual(insight2.GeneratedTimeUtc, insight.CloseTimeUtc);
            // period will not change
            Assert.AreEqual(Time.EndOfTimeTimeSpan, insight.Period);
        }
    }
}
