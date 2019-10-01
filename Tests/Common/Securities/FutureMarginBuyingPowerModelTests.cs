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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class FutureMarginBuyingPowerModelTests
    {
        // Test class to enable calling protected methods
        public class TestFutureMarginModel : FutureMarginModel
        {
            public new decimal GetMaintenanceMargin(Security security)
            {
                return base.GetMaintenanceMargin(security);
            }
        }

        [Test]
        public void TestMarginForSymbolWithOneLinerHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2016, 1, 1);
            var expDate = new DateTime(2017, 1, 1);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Softs.Coffee;
            var symbol = Symbol.CreateFuture(ticker, Market.USA, expDate);

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new TestFutureMarginModel();
            Assert.AreEqual(2900m, buyingPowerModel.GetMaintenanceMargin(futureSecurity));
        }

        [Test]
        public void TestMarginForSymbolWithNoHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2016, 1, 1);
            var expDate = new DateTime(2017, 1, 1);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history at all
            var ticker = "NOT-A-SYMBOL";
            var symbol = Symbol.CreateFuture(ticker, Market.USA, expDate);

            var futureSecurity = new Future(SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new TestFutureMarginModel();
            Assert.AreEqual(0m, buyingPowerModel.GetMaintenanceMargin(futureSecurity));
        }

        [Test]
        public void TestMarginForSymbolWithHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2013, 1, 1);
            var expDate = new DateTime(2017, 1, 1);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have history
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;
            var symbol = Symbol.CreateFuture(ticker, Market.USA, expDate);

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new TestFutureMarginModel();
            Assert.AreEqual(625m, buyingPowerModel.GetMaintenanceMargin(futureSecurity));

            // now we move forward to exact date when margin req changed
            time = new DateTime(2014, 06, 13);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            Assert.AreEqual(725m, buyingPowerModel.GetMaintenanceMargin(futureSecurity));

            // now we fly beyond the last line of the history file (currently) to see how margin model resolves future dates
            time = new DateTime(2016, 06, 04);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            Assert.AreEqual(585m, buyingPowerModel.GetMaintenanceMargin(futureSecurity));
        }

        [TestCase(0)]
        [TestCase(10000)]
        public void NonAccountCurrency_GetBuyingPower(decimal nonAccountCurrencyCash)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, nonAccountCurrencyCash, 0.88m);

            // For this symbol we dont have history
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;

            var futureSecurity = algorithm.AddFuture(ticker);

            var buyingPowerModel = new FutureMarginModel();
            var quantity =  buyingPowerModel.GetBuyingPower(new BuyingPowerParameters(
                algorithm.Portfolio, futureSecurity, OrderDirection.Buy));

            Assert.AreEqual(10000m + algorithm.Portfolio.CashBook[Currencies.USD].ValueInAccountCurrency,
                quantity.Value);
        }

        [Test]
        public void NonAccountCurrency_GetMaintenanceMarginRequirement()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Portfolio.SetAccountCurrency("EUR");
            algorithm.Portfolio.SetCash(10000);
            algorithm.Portfolio.SetCash(Currencies.USD, 0, 0.88m);

            // For this symbol we dont have history
            var ticker = QuantConnect.Securities.Futures.Financials.EuroDollar;

            const decimal price = 1.2345m;
            var time = new DateTime(2013, 1, 1);
            var futureSecurity = algorithm.AddFuture(ticker);
            futureSecurity.SetMarketPrice(new Tick { Value = price, Time = time });
            futureSecurity.Holdings.SetHoldings(1.5m, 1);

            var buyingPowerModel = new FutureMarginModel();
            // 625 overnight / (1.5 price * 1 quantity * 0.88 rate * 2500 multiplier) ~= 0.18939
            var res = buyingPowerModel.GetMaintenanceMarginRequirement(futureSecurity);
            Assert.AreEqual(0.18939, (double) res, 0.00001);
        }
    }
}
