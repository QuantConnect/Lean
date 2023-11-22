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

using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using QuantConnect.Logging;
using QuantConnect.Data;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Creates and starts <see cref="RandomDataGenerator"/> instance
    /// </summary>
    public static class RandomDataGeneratorProgram
    {
        private static readonly IRiskFreeInterestRateModel _interestRateProvider = new InterestRateProvider();

        public static void RandomDataGenerator(
            string startDateString,
            string endDateString,
            string symbolCountString,
            string market,
            string securityTypeString,
            string resolutionString,
            string dataDensityString,
            string includeCoarseString,
            string quoteTradeRatioString,
            string randomSeed,
            string hasIpoPercentageString,
            string hasRenamePercentageString,
            string hasSplitsPercentageString,
            string hasDividendsPercentageString,
            string dividendEveryQuarterPercentageString,
            string optionPriceEngineName,
            string volatilityModelResolutionString,
            string chainSymbolCountString,
            List<string> tickers
            )
        {
            var settings = RandomDataGeneratorSettings.FromCommandLineArguments(
                startDateString,
                endDateString,
                symbolCountString,
                market,
                securityTypeString,
                resolutionString,
                dataDensityString,
                includeCoarseString,
                quoteTradeRatioString,
                randomSeed,
                hasIpoPercentageString,
                hasRenamePercentageString,
                hasSplitsPercentageString,
                hasDividendsPercentageString,
                dividendEveryQuarterPercentageString,
                optionPriceEngineName,
                volatilityModelResolutionString,
                chainSymbolCountString,
                tickers
            );

            if (settings.Start.Year < 1998)
            {
                Log.Error($"RandomDataGeneratorProgram(): Required parameter --start must be at least 19980101");
                Environment.Exit(1);
            }

            var securityManager = new SecurityManager(new TimeKeeper(settings.Start, new[] { TimeZones.Utc }));
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

            var generator = new RandomDataGenerator();
            generator.Init(settings, securityManager);
            generator.Run();

            if (settings.IncludeCoarse && settings.SecurityType == SecurityType.Equity)
            {
                Log.Trace("RandomDataGeneratorProgram(): Launching coarse data generator...");

                CoarseUniverseGeneratorProgram.CoarseUniverseGenerator();
            }

            if (!Console.IsInputRedirected)
            {
                Log.Trace("RandomDataGeneratorProgram(): Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
