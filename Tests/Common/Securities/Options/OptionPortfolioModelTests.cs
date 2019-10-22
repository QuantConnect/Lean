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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionPortfolioModelTests
    {
        [Test]
        public void NonAccountCurrencyOption_Exercise()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var option = new Option(
                Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                new OptionSymbolProperties(SymbolProperties.GetDefault("EUR")),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            option.Underlying = security;
            security.SetMarketPrice(new Tick { Value = 1000 });
            portfolio.Securities.Add(option);
            var fakeOrderProcessor = new FakeOrderProcessor();
            portfolio.Transactions.SetOrderProcessor(fakeOrderProcessor);

            var fillPrice = 1000m;
            var fillQuantity = 1;
            option.ExerciseSettlement = SettlementType.Cash;
            var orderFee = new OrderFee(new CashAmount(1, "EUR"));
            var order = new OptionExerciseOrder(Symbols.SPY_C_192_Feb19_2016, fillQuantity, DateTime.UtcNow);
            fakeOrderProcessor.AddOrder(order);
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(order.Id, option.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFill(fill);

            // (1000 (price) - 192 (call strike)) * 1 quantity => 808 EUR
            Assert.AreEqual(10, option.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            // 808 - 1000 (price) - 1 fee
            Assert.AreEqual(-193, portfolio.CashBook["EUR"].Amount);
            // 100000 initial amount, no fee deducted
            Assert.AreEqual(100000, portfolio.CashBook[Currencies.USD].Amount);
        }

        private Security InitializeTest(DateTime reference, out SecurityPortfolioManager portfolio)
        {
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick { Value = 100 });
            var timeKeeper = new TimeKeeper(reference);
            var securityManager = new SecurityManager(timeKeeper);
            securityManager.Add(security);
            var transactionManager = new SecurityTransactionManager(null, securityManager);
            portfolio = new SecurityPortfolioManager(securityManager, transactionManager);
            portfolio.SetCash(Currencies.USD, 100 * 1000m, 1m);
            Assert.AreEqual(0, security.Holdings.Quantity);
            Assert.AreEqual(100 * 1000m, portfolio.CashBook[Currencies.USD].Amount);
            return security;
        }

        private static SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
