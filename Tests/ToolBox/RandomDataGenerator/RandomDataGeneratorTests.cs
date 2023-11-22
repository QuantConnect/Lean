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
using System.Collections.Generic;
using QuantConnect.Securities;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.ToolBox.RandomDataGenerator;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;
using QuantConnect.Util;
using static QuantConnect.ToolBox.RandomDataGenerator.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomDataGeneratorTests
    {
        [Test]
        [TestCase("2020, 1, 1 00:00:00", "2020, 1, 1 00:00:00", "2020, 1, 1 00:00:00")]
        [TestCase("2020, 1, 1 00:00:00", "2020, 2, 1 00:00:00", "2020, 1, 16 12:00:00")] // (31 days / 2) = 15.5 = 16 Rounds up to 12 pm
        [TestCase("2020, 1, 1 00:00:00", "2020, 3, 1 00:00:00", "2020, 1, 31 00:00:00")] // (60 days / 2) = 30
        [TestCase("2020, 1, 1 00:00:00", "2020, 6, 1 00:00:00", "2020, 3, 17 00:00:00")] // (152 days / 2) = 76

        public void NextRandomGeneratedData(DateTime start, DateTime end, DateTime expectedMidPoint)
        {
            var randomValueGenerator = new RandomValueGenerator();
            var midPoint = QuantConnect.ToolBox.RandomDataGenerator.RandomDataGenerator.GetDateMidpoint(start, end);
            var delistDate = QuantConnect.ToolBox.RandomDataGenerator.RandomDataGenerator.GetDelistingDate(start, end, randomValueGenerator);

            // midPoint and expectedMidPoint must be the same
            Assert.AreEqual(expectedMidPoint, midPoint);

            // start must be less than or equal to end
            Assert.LessOrEqual(start, end);

            // delistDate must be less than or equal to end
            Assert.LessOrEqual(delistDate, end);
            Assert.GreaterOrEqual(delistDate, midPoint);
        }

        [TestCase("20220101", "20230101")]
        public void RandomGeneratorProducesValuesBoundedForEquitiesWhenSplit(string start, string end)
        {
            var settings = RandomDataGeneratorSettings.FromCommandLineArguments(
                start,
                end,
                "1",
                "usa",
                "Equity",
                "Minute",
                "Dense",
                "true",
                "1",
                null,
                "5.0",
                "30.0",
                "100.0",
                "60.0",
                "30.0",
                "BaroneAdesiWhaleyApproximationEngine",
                "Daily",
                "1",
                new List<string>(),
                100
            );

            var securityManager = new SecurityManager(new TimeKeeper(settings.Start, new[] { TimeZones.Utc }));
            var securityService = GetSecurityService(settings, securityManager);
            securityManager.SetSecurityService(securityService);

            var security = securityManager.CreateSecurity(Symbols.AAPL, new List<SubscriptionDataConfig>(), underlying: null);
            var randomValueGenerator = new RandomValueGenerator();
            var tickGenerator = new TickGenerator(settings, new TickType[1] {TickType.Trade}, security, randomValueGenerator).GenerateTicks().GetEnumerator();
            using var sync = new SynchronizingBaseDataEnumerator(tickGenerator);
            var tickHistory = new List<Tick>();

            while (sync.MoveNext())
            {
                var dataPoint = sync.Current;
                tickHistory.Add(dataPoint as Tick);
            }

            var dividendsSplitsMaps = new DividendSplitMapGenerator(
                        Symbols.AAPL,
                        settings,
                        randomValueGenerator,
                        BaseSymbolGenerator.Create(settings, randomValueGenerator),
                        new Random(),
                        GetDelistingDate(settings.Start, settings.End, randomValueGenerator),
                        false);

            dividendsSplitsMaps.GenerateSplitsDividends(tickHistory);
            Assert.IsTrue(0.099m <= dividendsSplitsMaps.FinalSplitFactor && dividendsSplitsMaps.FinalSplitFactor <= 1.5m);

            foreach (var tick in tickHistory)
            {
                tick.Value = tick.Value / dividendsSplitsMaps.FinalSplitFactor;
                Assert.IsTrue( 0.001m <= tick.Value && tick.Value <= 1000000000, $"The tick value was {tick.Value} but should have been bounded by 0.001 and 1 000 000 000");
            }
        }

        private static readonly IRiskFreeInterestRateModel _interestRateProvider = new InterestRateProvider();

        private static SecurityService GetSecurityService(RandomDataGeneratorSettings settings, SecurityManager securityManager)
        {
            var securityService = new SecurityService(
                new CashBook(),
                MarketHoursDatabase.FromDataFolder(),
                SymbolPropertiesDatabase.FromDataFolder(),
                new SecurityInitializerProvider(new FuncSecurityInitializer(security =>
                {
                    // init price
                    security.SetMarketPrice(new Tick(settings.Start, security.Symbol, 100, 100));
                    security.SetMarketPrice(new OpenInterest(settings.Start, security.Symbol, 10000));

                    // from settings
                    security.VolatilityModel = new StandardDeviationOfReturnsVolatilityModel(settings.VolatilityModelResolution);

                    // from settings
                    if (security is Option option)
                    {
                        option.PriceModel = OptionPriceModels.Create(settings.OptionPriceEngineName,
                            _interestRateProvider.GetRiskFreeRate(settings.Start, settings.End));
                    }
                })),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(
                    new SecurityPortfolioManager(securityManager, new SecurityTransactionManager(null, securityManager), new AlgorithmSettings())),
                new MapFilePrimaryExchangeProvider(Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider")))
            );
            securityManager.SetSecurityService(securityService);

            return securityService;
        }
    }
}
