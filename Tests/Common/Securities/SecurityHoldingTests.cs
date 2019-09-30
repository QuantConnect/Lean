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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityHoldingTests
    {
        [Test]
        public void ComputesUnrealizedProfit()
        {
            var security = GetSecurity<QuantConnect.Securities.Equity.Equity>(Symbols.SPY, Resolution.Daily);
            var holding = new SecurityHolding(security, new IdentityCurrencyConverter(Currencies.USD));

            var last = 100m;
            var bid = 99m;
            var ask = 101m;
            var orderFee = 1m;

            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, last, bid, ask));

            // Long scenario: expected unrealized profit take the bid price
            var quantity = 100m;
            var expected = (bid - last) * quantity - orderFee;
            holding.SetHoldings(last, quantity);
            Assert.AreEqual(expected, holding.UnrealizedProfit);

            // Short scenario: expected unrealized profit take the ask price
            quantity = -100m;
            expected = (ask - last) * quantity - orderFee;
            holding.SetHoldings(last, quantity);
            Assert.AreEqual(expected, holding.UnrealizedProfit);
        }

        private Security GetSecurity<T>(Symbol symbol, Resolution resolution)
        {
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var reference = DateTime.Now;
            var referenceUtc = reference.ConvertToUtc(subscriptionDataConfig.DataTimeZone);
            var timeKeeper = new TimeKeeper(referenceUtc);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(subscriptionDataConfig.DataTimeZone));

            return security;
        }
    }
}