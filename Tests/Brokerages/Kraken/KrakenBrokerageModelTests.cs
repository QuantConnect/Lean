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
 *
*/


using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Kraken
{
    [TestFixture]
    public class KrakenBrokerageModelTests
    {
        private readonly KrakenBrokerageModel _brokerageModel = new KrakenBrokerageModel(
            AccountType.Margin
        );

        private static TestCaseData[] Symbols =>
            new[]
            {
                new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Kraken), 5m),
                new TestCaseData(Symbol.Create("USDTUSD", SecurityType.Crypto, Market.Kraken), 2m),
                new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Kraken), 5m),
                new TestCaseData(Symbol.Create("ADAETH", SecurityType.Crypto, Market.Kraken), 3m),
                new TestCaseData(Symbol.Create("ADAEUR", SecurityType.Crypto, Market.Kraken), 3m),
                new TestCaseData(Symbol.Create("XRPBTC", SecurityType.Crypto, Market.Kraken), 3m),
                new TestCaseData(Symbol.Create("BTCETH", SecurityType.Crypto, Market.Kraken), 1m), // BTC available only with fiats
                new TestCaseData(Symbol.Create("XRPETH", SecurityType.Crypto, Market.Kraken), 1m), // XRP not available with ETH
                new TestCaseData(Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Kraken), 1m), // BTC available only with fiats
            };

        [Test]
        [TestCaseSource(nameof(Symbols))]
        public void GetLeverageTest(Symbol symbol, decimal expectedLeverage)
        {
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
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
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var leverage = _brokerageModel.GetLeverage(security);

            Assert.AreEqual(
                leverage,
                expectedLeverage,
                "Expected leverage doesn't match with returned"
            );
        }
    }
}
