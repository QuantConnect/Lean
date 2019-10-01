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
using QuantConnect.Benchmarks;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Benchmarks
{
    [TestFixture]
    public class SecurityBenchmarkTests
    {
        [TestCase(1)]
        [TestCase(10)]
        public void EvaluatesInAccountCurrency(decimal conversionRate)
        {
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true, true, false),
                new Cash(Currencies.USD, 0, conversionRate),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var price = 25;
            security.SetMarketPrice(new Tick { Value = price });
            var benchmark = new SecurityBenchmark(security);

            Assert.AreEqual(price * conversionRate, benchmark.Evaluate(DateTime.Now));
        }
    }
}
