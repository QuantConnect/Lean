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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class IndicatorVolatilityModelTests
    {
        [Test]
        public void UpdatesAfterCorrectPeriodElapses()
        {
            const int periods = 3;
            var model = new IndicatorVolatilityModel(
                new StandardDeviation(periods),
                (s, d, i) => i.Update(d)
            );
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);
            for (var i = 0; i < periods; i++)
            {
                security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(i + 1), i + 1));
            }

            var expected = Math.Sqrt(2.0 / 3);
            Assert.AreEqual(expected, model.Volatility);
        }

        [Test]
        public void DoesntUpdateOnZeroPrice()
        {
            const int periods = 3;
            var model = new IndicatorVolatilityModel(
                new StandardDeviation(periods),
                (s, d, i) =>
                {
                    if (s.Price > 0)
                        i.Update(d);
                }
            );
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);
            for (var i = 0; i < periods; i++)
            {
                security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(i + 1), i + 1));
            }

            var expected = Math.Sqrt(2.0 / 3);
            Assert.AreEqual(expected, model.Volatility);

            // update should not be applied as price is 0 since this condition is defined by indicatorUpdate
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(3), 0m));
            Assert.AreEqual(expected, model.Volatility);
        }

        [Test]
        public void GetHistoryRequirementsWorks()
        {
            const int periods = 3;
            var model = new IndicatorVolatilityModel(
                new StandardDeviation(periods),
                (s, d, i) => i.Update(d)
            );
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            var result = model.GetHistoryRequirements(security, DateTime.UtcNow);
            Assert.AreEqual(Enumerable.Empty<HistoryRequest>(), result);
        }

        private static Security GetSecurity(DateTime reference, IVolatilityModel model)
        {
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                false,
                false
            );
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            security.VolatilityModel = model;

            return security;
        }
    }
}
