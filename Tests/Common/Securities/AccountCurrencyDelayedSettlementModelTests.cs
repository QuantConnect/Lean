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
    public class AccountCurrencyDelayedSettlementModelTests
    {
        private static readonly DateTime Noon = new DateTime(2015, 11, 2, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        [Test]
        public void BuySettlesImmediatelyInTheAccountCurrency()
        {
            var (portfolio, security) = CreatePortfolioAndSecurity();
            var model = new AccountCurrencyDelayedSettlementModel(1, TimeSpan.FromHours(8));

            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, timeUtc, new CashAmount(-1000, Currencies.USD), null));

            // 1,000 USD is 800 GBP, and no USD balance is left behind.
            Assert.AreEqual(200, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
            Assert.AreEqual(0, portfolio.CashBook[Currencies.USD].Amount);
        }

        [Test]
        public void SellSettlesInTheAccountCurrencyAfterTPlusOne()
        {
            var (portfolio, security) = CreatePortfolioAndSecurity();
            var model = new AccountCurrencyDelayedSettlementModel(1, TimeSpan.FromHours(8));

            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, timeUtc, new CashAmount(1000, Currencies.USD), null));
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));

            // The 1,000 USD proceeds are 800 GBP, unsettled.
            Assert.AreEqual(1000, portfolio.Cash);
            Assert.AreEqual(800, portfolio.UnsettledCash);
            Assert.AreEqual(0, portfolio.CashBook[Currencies.USD].Amount);

            // Tuesday at 7:55 AM, still unsettled.
            timeUtc = timeUtc.AddDays(1).AddHours(-4).AddMinutes(-5);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(1000, portfolio.Cash);
            Assert.AreEqual(800, portfolio.UnsettledCash);

            // Tuesday at 8 AM, settled in GBP.
            timeUtc = timeUtc.AddMinutes(5);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(1800, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        private static (SecurityPortfolioManager Portfolio, Security Security) CreatePortfolioAndSecurity()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());
            portfolio.SetAccountCurrency(Currencies.GBP, 1000);
            // One USD is worth 0.8 GBP.
            portfolio.SetCash(Currencies.USD, 0, 0.8m);

            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute,
                TimeZones.NewYork, TimeZones.NewYork, true, true, false);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 0.8m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            return (portfolio, security);
        }
    }
}
