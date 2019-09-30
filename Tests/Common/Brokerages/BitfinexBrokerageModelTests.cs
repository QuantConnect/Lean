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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class BitfinexBrokerageModelTests
    {
        protected Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
        protected Security Security
        {
            get
            {
                return new Security(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new SubscriptionDataConfig(
                        typeof(TradeBar),
                        Symbol,
                        Resolution.Minute,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        false,
                        false,
                        false
                    ),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                );
            }
        }

        [Test]
        public void GetCashBuyingPowerModelTest()
        {
            BitfinexBrokerageModel model = new BitfinexBrokerageModel(AccountType.Cash);
            Assert.IsInstanceOf<CashBuyingPowerModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(1, model.GetLeverage(Security));
        }

        [Test]
        public void GetSecurityMarginModelTest()
        {
            BitfinexBrokerageModel model = new BitfinexBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<SecurityMarginModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(3.3M, model.GetLeverage(Security));
        }

        [Test]
        public void GetEquityLeverage_ThrowsArgumentException_Test()
        {
            var equity = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var model = new BitfinexBrokerageModel();
            Assert.Throws<ArgumentException>(() => model.GetLeverage(equity));
        }

        [Test]
        public void GetCustomDataLeverageTest()
        {
            var dummy = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    QuantConnect.Symbol.Create("DUMMY", SecurityType.Base, Market.Bitfinex),
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var model = new BitfinexBrokerageModel();
            Assert.AreEqual(1M, model.GetLeverage(dummy));
        }
    }
}