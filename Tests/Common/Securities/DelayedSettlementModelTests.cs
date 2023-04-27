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
    public class DelayedSettlementModelTests
    {
        private static readonly DateTime Noon = new DateTime(2015, 11, 2, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        [Test]
        public void SellOnMondaySettleOnThursday()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            // settlement at T+3, 8:00 AM
            var model = new DelayedSettlementModel(3, TimeSpan.FromHours(8));
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            portfolio.SetCash(3000);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            // Sell on Monday
            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, timeUtc, new CashAmount(1000, Currencies.USD), null));
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Tuesday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Wednesday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Thursday at 7:55 AM, still unsettled
            timeUtc = timeUtc.AddDays(1).AddHours(-4).AddMinutes(-5);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Thursday at 8 AM, now settled
            timeUtc = timeUtc.AddMinutes(5);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(4000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        [Test]
        public void SellOnThursdaySettleOnTuesday()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            // settlement at T+3, 8:00 AM
            var model = new DelayedSettlementModel(3, TimeSpan.FromHours(8));
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            portfolio.SetCash(3000);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            // Sell on Thursday
            var timeUtc = Noon.AddDays(3).ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, timeUtc, new CashAmount(1000, Currencies.USD), null));
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Friday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Saturday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Sunday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Monday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Tuesday at 7:55 AM, still unsettled
            timeUtc = timeUtc.AddDays(1).AddHours(-4).AddMinutes(-5);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Tuesday at 8 AM, now settled
            timeUtc = timeUtc.AddMinutes(5);
            model.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            Assert.AreEqual(4000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }

    }
}
