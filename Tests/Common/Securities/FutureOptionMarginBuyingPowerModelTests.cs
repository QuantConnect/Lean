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
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class FutureOptionMarginBuyingPowerModelTests
    {
        // Test class to enable calling protected methods
        public class TestFutureMarginModel : FutureMarginModel
        {
            public TestFutureMarginModel(Security security = null)
                : base(security: security)
            {
            }

            public new decimal GetMaintenanceMargin(Security security)
            {
                return base.GetMaintenanceMargin(security);
            }
        }

        // Test class to enable calling protected methods
        public class TestFuturesOptionsMarginModel : FuturesOptionsMarginModel
        {
            public TestFuturesOptionsMarginModel(Option futureOption) : base(futureOption: futureOption)
            {
            }

            public new decimal GetMaintenanceMargin(Security security)
            {
                return base.GetMaintenanceMargin(security);
            }
        }

        [Test]
        public void MarginForSymbolWithOneLinerHistory()
        {
            const decimal price = 1.2345m;
            var time = new DateTime(2020, 10, 14);
            var expDate = new DateTime(2021, 3, 19);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.CME, expDate);
            var symbol = Symbol.CreateOption(future, Market.CME, OptionStyle.American, OptionRight.Call, 2550m, new DateTime(2021, 3, 19));

            var optionSecurity = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = price, Time = time });
            optionSecurity.Underlying.Holdings.SetHoldings(1.5m, 1);

            var futureBuyingPowerModel = new TestFutureMarginModel(optionSecurity.Underlying);
            var futureOptionBuyingPowerModel = new TestFuturesOptionsMarginModel(optionSecurity);

            Assert.AreNotEqual(0m, futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
            Assert.AreEqual(futureBuyingPowerModel.GetMaintenanceMargin(optionSecurity.Underlying) * 1.5m, futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
        }
    }
}

