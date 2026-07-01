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
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class OptionContractTests
    {
        private static Option CreateOption(Symbol symbol)
        {
            return new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }

        [SetUp]
        public void ResetSharedOptionData()
        {
            // Other tests can leave the shared OptionPriceModelResultData.Null singleton holding a
            // trade bar, which then leaks into any contract that hasn't set its own price model.
            // Reset it by updating a throwaway (singleton-backed) contract with a zero-priced trade bar.
            var symbol = Symbols.SPY_C_192_Feb19_2016;
            new OptionContract(CreateOption(symbol))
                .Update(new TradeBar(new DateTime(2016, 02, 16), symbol, 0, 0, 0, 0, 0));
        }

        [Test]
        public void PriceValueAndCloseAliasLastPrice()
        {
            var symbol = Symbols.SPY_C_192_Feb19_2016;
            var contract = new OptionContract(CreateOption(symbol)) { Time = new DateTime(2016, 02, 16) };
            contract.SetOptionPriceModel(() => OptionPriceModelResult.None);

            // No data yet, all aliases default to zero
            Assert.AreEqual(0, contract.LastPrice);
            Assert.AreEqual(contract.LastPrice, contract.Price);
            Assert.AreEqual(contract.LastPrice, contract.Value);
            Assert.AreEqual(contract.LastPrice, contract.Close);

            var tradeBar = new TradeBar(new DateTime(2016, 02, 16), symbol, 1, 2, 3, 4, 5);
            contract.Update(tradeBar);

            Assert.AreEqual(4, contract.LastPrice);
            Assert.AreEqual(contract.LastPrice, contract.Price);
            Assert.AreEqual(contract.LastPrice, contract.Value);
            Assert.AreEqual(contract.LastPrice, contract.Close);
        }
    }
}
