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
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class StandardDeviationOfReturnsVolatilityModelTests
    {
        [Test]
        public void UpdatesAfterCorrectDailyPeriodElapses()
        {
            const int periods = 3;
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            security.VolatilityModel = model;

            var first = new IndicatorDataPoint(reference, 1);
            security.SetMarketPrice(first);

            Assert.AreEqual(0m, model.Volatility);

            var second = new IndicatorDataPoint(reference.AddDays(1), 2);
            security.SetMarketPrice(second);
            Assert.AreEqual(0, model.Volatility);

            // update should not be applied since not enough time has passed
            var third = new IndicatorDataPoint(reference.AddDays(1.01), 1000);
            security.SetMarketPrice(third);
            Assert.AreEqual(0, model.Volatility);

            var fourth = new IndicatorDataPoint(reference.AddDays(2), 3);
            security.SetMarketPrice(fourth);
            Assert.AreEqual(5.6124, (double)model.Volatility, 0.0001);
        }

        [Test]
        public void DoesntUpdateOnZeroPrice()
        {
            const int periods = 3;
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            security.VolatilityModel = model;

            var first = new IndicatorDataPoint(reference, 1);
            security.SetMarketPrice(first);

            Assert.AreEqual(0m, model.Volatility);

            var second = new IndicatorDataPoint(reference.AddDays(1), 2);
            security.SetMarketPrice(second);
            Assert.AreEqual(0, model.Volatility);

            // update should not be applied since not enough time has passed
            var third = new IndicatorDataPoint(reference.AddDays(1.01), 1000);
            security.SetMarketPrice(third);
            Assert.AreEqual(0, model.Volatility);

            var fourth = new IndicatorDataPoint(reference.AddDays(2), 3);
            security.SetMarketPrice(fourth);
            Assert.AreEqual(5.6124, (double)model.Volatility, 0.0001);

            // update should not be applied as price is 0
            var fifth = new IndicatorDataPoint(reference.AddDays(3), 0m);
            security.SetMarketPrice(fifth);
            Assert.AreEqual(5.6124, (double)model.Volatility, 0.0001);
        }

        [Test]
        public void GetHistoryRequirementsWorks()
        {
            const int periods = 3;
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            model.SetSubscriptionDataConfigProvider(new MockSubscriptionDataConfigProvider(config));
            var result = model.GetHistoryRequirements(security, DateTime.UtcNow).First();

            Assert.AreEqual(config.DataNormalizationMode, result.DataNormalizationMode);
            Assert.AreEqual(config.Symbol, result.Symbol);
            Assert.AreEqual(config.DataTimeZone, result.DataTimeZone);
            Assert.AreEqual(config.IsCustomData, result.IsCustomData);
            Assert.AreEqual(config.FillDataForward, result.FillForwardResolution != null);
            Assert.AreEqual(config.ExtendedMarketHours, result.IncludeExtendedMarketHours);
            // the StandardDeviationOfReturnsVolatilityModel always uses daily
            Assert.AreEqual(Resolution.Daily, result.Resolution);
        }

        [Test]
        public void GetHistoryRequirementsWorksForTwoDifferentSubscriptions()
        {
            const int periods = 3;
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            var mock = new MockSubscriptionDataConfigProvider(config);
            mock.SubscriptionDataConfigs.Add(
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Second,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false,
                    true));
            model.SetSubscriptionDataConfigProvider(mock);
            var result = model.GetHistoryRequirements(security, DateTime.UtcNow).First();

            Assert.AreEqual(config.DataNormalizationMode, result.DataNormalizationMode);
            Assert.AreEqual(config.Symbol, result.Symbol);
            Assert.AreEqual(config.DataTimeZone, result.DataTimeZone);
            Assert.AreEqual(true, result.IsCustomData);
            Assert.AreEqual(true, result.FillForwardResolution != null);
            Assert.AreEqual(true, result.IncludeExtendedMarketHours);
            // the StandardDeviationOfReturnsVolatilityModel always uses daily
            Assert.AreEqual(Resolution.Daily, result.Resolution);
        }
    }
}
