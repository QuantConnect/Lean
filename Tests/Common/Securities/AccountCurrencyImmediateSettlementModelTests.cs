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
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class AccountCurrencyImmediateSettlementModelTests
    {
        private static readonly DateTime Noon = new DateTime(2014, 6, 24, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        [Test]
        public void FundsAreSettledImmediately()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            var model = new AccountCurrencyImmediateSettlementModel();
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            portfolio.SetCash(1000);
            Assert.AreEqual(1000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(portfolio, security, timeUtc, Currencies.USD, 1000);

            Assert.AreEqual(2000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            model.ApplyFunds(portfolio, security, timeUtc, Currencies.USD, -500);

            Assert.AreEqual(1500, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            model.ApplyFunds(portfolio, security, timeUtc, Currencies.USD, 1000);

            Assert.AreEqual(2500, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        [Test]
        public void FundsAreSettledInAccountCurrency()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            var model = new AccountCurrencyImmediateSettlementModel();
            portfolio.SetCash(1000);
            portfolio.SetCash("EUR", 0, 1.1m);

            var config = CreateTradeBarConfig(Symbols.DE30EUR);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                portfolio.CashBook["EUR"],
                SymbolProperties.GetDefault("EUR"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            Assert.AreEqual(1000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(portfolio, security, timeUtc, "EUR", 1000);

            // 1000 + 1000 * 1.1 = 2100
            Assert.AreEqual(2100, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            model.ApplyFunds(portfolio, security, timeUtc, "EUR", -500);

            // 2100 - 500 * 1.1 = 1550
            Assert.AreEqual(1550, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            model.ApplyFunds(portfolio, security, timeUtc, "EUR", 1000);

            // 1550 + 1000 * 1.1 = 2650
            Assert.AreEqual(2650, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
