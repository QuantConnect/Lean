using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Creates and starts <see cref="RandomDataGenerator"/> instance
    /// </summary>
    public class RandomDataGeneratorProgram
    {
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
            string volatilityModelResolutionString
            )
        {
            var output = new ConsoleLeveledOutput();
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

                output
            );

            if (settings.Start.Year < 1998)
            {
                output.Error.WriteLine($"Required parameter --start must be at least 19980101");
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
                        option.PriceModel = OptionPriceModels.Create(settings.OptionPriceEngineName, Statistics.PortfolioStatistics.GetRiskFreeRate());
                    }
                })),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(
                    new SecurityPortfolioManager(securityManager, new SecurityTransactionManager(null, securityManager))),
                new MapFilePrimaryExchangeProvider(Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider")))
            );
            securityManager.SetSecurityService(securityService);

            var generator = new RandomDataGenerator();
            generator.Init(settings, output, securityManager);
            generator.Run();

            if (settings.IncludeCoarse && settings.SecurityType == SecurityType.Equity)
            {
                output.Info.WriteLine("Launching coarse data generator...");

                CoarseUniverseGeneratorProgram.CoarseUniverseGenerator();
            }

            if (!Console.IsInputRedirected)
            {
                output.Info.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
