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
using QuantConnect.Securities;
using QuantConnect.Securities.Volatility;
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class BaseVolatilityModelTests
    {
        [Test]
        public void GetHistoryRequirementsWorks(
            [ValueSource(nameof(GetDataNormalizationModes))] DataNormalizationMode dataNormalizationMode,
            [Values] bool passResolution)
        {
            const int periods = 3;
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
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
                false,
                dataNormalizationMode: dataNormalizationMode);
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

            var model = new BaseVolatilityModel();
            model.SetSubscriptionDataConfigProvider(new MockSubscriptionDataConfigProvider(config));
            var result = model.GetHistoryRequirements(security, DateTime.UtcNow, passResolution ? config.Resolution : null, periods).First();

            Assert.AreEqual(config.DataNormalizationMode, result.DataNormalizationMode);
            Assert.AreEqual(config.Symbol, result.Symbol);
            Assert.AreEqual(config.DataTimeZone, result.DataTimeZone);
            Assert.AreEqual(config.IsCustomData, result.IsCustomData);
            Assert.AreEqual(config.FillDataForward, result.FillForwardResolution != null);
            Assert.AreEqual(config.ExtendedMarketHours, result.IncludeExtendedMarketHours);
            // Max resolution is used if no resolution is passed
            Assert.AreEqual(passResolution ? config.Resolution : Resolution.Daily, result.Resolution);
        }

        [Test]
        public void GetHistoryRequirementsWorksForTwoDifferentSubscriptions(
            [ValueSource(nameof(GetDataNormalizationModes))] DataNormalizationMode dataNormalizationMode,
            [Values] bool passResolution)
        {
            const int periods = 3;
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
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
                false,
                dataNormalizationMode: dataNormalizationMode);
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

            var model = new BaseVolatilityModel();
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
                    true,
                dataNormalizationMode: dataNormalizationMode));
            model.SetSubscriptionDataConfigProvider(mock);
            var result = model.GetHistoryRequirements(security, DateTime.UtcNow, passResolution ? config.Resolution : null, periods).First();

            Assert.AreEqual(config.DataNormalizationMode, result.DataNormalizationMode);
            Assert.AreEqual(config.Symbol, result.Symbol);
            Assert.AreEqual(config.DataTimeZone, result.DataTimeZone);
            Assert.AreEqual(true, result.IsCustomData);
            Assert.AreEqual(true, result.FillForwardResolution != null);
            Assert.AreEqual(true, result.IncludeExtendedMarketHours); ;
            // Max resolution is used if no resolution is passed
            Assert.AreEqual(passResolution ? config.Resolution : Resolution.Daily, result.Resolution);
        }

        private static DataNormalizationMode[] GetDataNormalizationModes => (DataNormalizationMode[])Enum.GetValues(typeof(DataNormalizationMode));
    }
}
