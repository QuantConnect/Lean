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

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random data according to the specified parameters
    /// </summary>
    public class RandomDataGeneratorProgram
    {

        private static Dictionary<SecurityType, TickType[]> _tickTypesPerSecurityType =
            new()
            {
                { SecurityType.Base, new[] { TickType.Trade } },
                { SecurityType.Equity, new[] { TickType.Trade } },
                { SecurityType.Forex, new[] { TickType.Quote } },
                { SecurityType.Cfd, new[] { TickType.Quote } },

                { SecurityType.Option, new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest } },
                { SecurityType.Future, new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest } },

                { SecurityType.Crypto, new[] { TickType.Trade, TickType.Quote } }
            };

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
            string volatilityPercentageString
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
                volatilityPercentageString,

                output
            );

            if (settings.Start.Year < 1998)
            {
                output.Error.WriteLine($"Required parameter --start must be at least 19980101");
                Environment.Exit(1);
            }

            GenerateRandomData(settings, output);

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

        public static DateTime GetDateMidpoint(DateTime start, DateTime end)
        {
            TimeSpan span = end.Subtract(start);
            int span_time = (int)span.TotalMinutes;
            double diff_span = -(span_time / 2.0);
            DateTime start_time = end.AddMinutes(Math.Round(diff_span, 2, MidpointRounding.ToEven));

            //Returns a DateTime object that is halfway between start and end
            return start_time;
        }

        public static DateTime GetDelistingDate(DateTime start, DateTime end, RandomValueGenerator randomValueGenerator)
        {
            var mid_point = GetDateMidpoint(start, end);
            var delist_Date = randomValueGenerator.NextDate(mid_point, end, null);

            //Returns a DateTime object that is a random value between the mid_point and end
            return delist_Date;
        }

        public static void GenerateRandomData(RandomDataGeneratorSettings settings, ConsoleLeveledOutput output)
        {
            // can specify a seed value in this ctor if determinism is desired
            var random = new Random();
            var randomValueGenerator = new RandomValueGenerator();
            if (settings.RandomSeedSet)
            {
                random = new Random(settings.RandomSeed);
                randomValueGenerator = new RandomValueGenerator(settings.RandomSeed);
            }

            var securityManager = new SecurityManager(new TimeKeeper(settings.Start, new[] { TimeZones.Utc }));
            var securityService = new SecurityService(
                new CashBook(),
                MarketHoursDatabase.FromDataFolder(),
                SymbolPropertiesDatabase.FromDataFolder(),
                new SecurityInitializerProvider(new FuncSecurityInitializer(secutiry =>
                {
                    // from settings
                    secutiry.VolatilityModel = new StandardDeviationOfReturnsVolatilityModel(settings.Resolution);

                    // from settings
                    if (secutiry is Option option)
                    {
                        option.PriceModel = OptionPriceModels.BlackScholes();
                    }
                })),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(
                    new SecurityPortfolioManager(securityManager, new SecurityTransactionManager(null, securityManager))),
                new MapFilePrimaryExchangeProvider(Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider")))
            );

            var symbolGenerator = SymbolGenerator.Create(settings, randomValueGenerator);

            var maxSymbolCount = symbolGenerator.GetAvailableSymbolCount();
            if (settings.SymbolCount > maxSymbolCount)
            {
                output.Warn.WriteLine($"Limiting Symbol count to {maxSymbolCount}, we don't have more {settings.SecurityType} tickers for {settings.Market}");
                settings.SymbolCount = maxSymbolCount;
            }

            output.Warn.WriteLine($"Begin data generation of {settings.SymbolCount} randomly generated {settings.SecurityType} assets...");

            // iterate over our randomly generated symbols
            var count = 0;
            var progress = 0d;
            var previousMonth = -1;

            foreach (var (symbolRef, currentSymbolGroup) in symbolGenerator.GenerateRandomSymbols()
                .GroupBy(s => s.HasUnderlying ? s.Underlying : s)
                .Select(g => (g.Key, g.OrderBy(s => s.HasUnderlying).ToList())))
            {
                output.Warn.WriteLine($"\tSymbol[{++count}]: {symbolRef} Progress: {progress:0.0}% - Generating data...");

                var tickGenerators = new Dictionary<Security, IEnumerator<IEnumerable<Tick>>>();
                var tickHistories = new Dictionary<Symbol, List<Tick>>();
                Security underlyingSecurity = null;
                foreach (var currentSymbol in currentSymbolGroup)
                {
                    var security = securityService.CreateSecurity(
                        currentSymbol,
                        new List<SubscriptionDataConfig>(),
                        underlying: underlyingSecurity);

                    underlyingSecurity ??= security;

                    tickGenerators.Add(
                        security,
                        TickGenerator.Create(settings, _tickTypesPerSecurityType[currentSymbol.SecurityType], randomValueGenerator, security)
                            .GenerateTicks()
                            .GetEnumerator());

                    tickHistories.Add(
                        currentSymbol,
                        new List<Tick>());
                }

                var go = true;
                while (go)
                {
                    foreach (var (security, tickGenerator) in tickGenerators)
                    {
                        go = tickGenerator.MoveNext();
                        if (!go)
                        {
                            break;
                        }

                        var ticks = tickGenerator.Current.ToList();
                        tickHistories[security.Symbol].AddRange(ticks);

                        foreach (var group in ticks.GroupBy(t => t.TickType))
                        {
                            security.Update(group.ToList(), group.First().GetType(), false);
                        }
                    }
                }

                foreach (var (currentSymbol, tickHistory) in tickHistories)
                {
                    var symbol = currentSymbol;

                    // This is done so that we can update the Symbol in the case of a rename event
                    var delistDate = GetDelistingDate(settings.Start, settings.End, randomValueGenerator);
                    var willBeDelisted = randomValueGenerator.NextBool(1.0);

                    // Companies rarely IPO then disappear within 6 months
                    if (willBeDelisted && tickHistory.Select(tick => tick.Time.Month).Distinct().Count() <= 6)
                    {
                        willBeDelisted = false;
                    }

                    var dividendsSplitsMaps = new DividendSplitMapGenerator(
                        symbol,
                        settings,
                        randomValueGenerator,
                        symbolGenerator,
                        random,
                        delistDate,
                        willBeDelisted);

                    // Keep track of renamed symbols and the time they were renamed. 
                    var renamedSymbols = new Dictionary<Symbol, DateTime>();

                    if (settings.SecurityType == SecurityType.Equity)
                    {
                        dividendsSplitsMaps.GenerateSplitsDividends(tickHistory);

                        if (!willBeDelisted)
                        {
                            dividendsSplitsMaps.DividendsSplits.Add(new CorporateFactorRow(new DateTime(2050, 12, 31), 1m, 1m));

                            if (dividendsSplitsMaps.MapRows.Count > 1)
                            {
                                // Remove the last element if we're going to have a 20501231 entry
                                dividendsSplitsMaps.MapRows.RemoveAt(dividendsSplitsMaps.MapRows.Count - 1);
                            }
                            dividendsSplitsMaps.MapRows.Add(new MapFileRow(new DateTime(2050, 12, 31), dividendsSplitsMaps.CurrentSymbol.Value));
                        }

                        // If the Symbol value has changed, update the current Symbol
                        if (symbol != dividendsSplitsMaps.CurrentSymbol)
                        {
                            // Add all Symbol rename events to dictionary
                            // We skip the first row as it contains the listing event instead of a rename event
                            foreach (var renameEvent in dividendsSplitsMaps.MapRows.Skip(1))
                            {
                                // Symbol.UpdateMappedSymbol does not update the underlying security ID Symbol, which 
                                // is used to create the hash code. Create a new equity Symbol from scratch instead.
                                symbol = Symbol.Create(renameEvent.MappedSymbol, SecurityType.Equity, settings.Market);
                                renamedSymbols.Add(symbol, renameEvent.Date);

                                output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} will be renamed on {renameEvent.Date}");
                            }
                        }
                        else
                        {
                            // This ensures that ticks will be written for the current Symbol up until 9999-12-31
                            renamedSymbols.Add(symbol, new DateTime(9999, 12, 31));
                        }

                        symbol = dividendsSplitsMaps.CurrentSymbol;

                        // Write Splits and Dividend events to directory factor_files
                        var factorFile = new CorporateFactorProvider(symbol.Value, dividendsSplitsMaps.DividendsSplits, settings.Start);
                        var mapFile = new MapFile(symbol.Value, dividendsSplitsMaps.MapRows);

                        factorFile.WriteToFile(symbol);
                        mapFile.WriteToCsv(settings.Market, symbol.SecurityType);

                        output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Dividends, splits, and map files have been written to disk.");
                    }
                    else
                    {
                        // This ensures that ticks will be written for the current Symbol up until 9999-12-31
                        renamedSymbols.Add(symbol, new DateTime(9999, 12, 31));
                    }

                    // define aggregators via settings
                    var aggregators = CreateAggregators(settings, _tickTypesPerSecurityType[currentSymbol.SecurityType]).ToList();
                    Symbol previousSymbol = null;
                    var currentCount = 0;
                    var monthsTrading = 0;

                    foreach (var renamed in renamedSymbols)
                    {
                        var previousRenameDate = previousSymbol == null ? new DateTime(1, 1, 1) : renamedSymbols[previousSymbol];
                        var previousRenameDateDay = new DateTime(previousRenameDate.Year, previousRenameDate.Month, previousRenameDate.Day);
                        var renameDate = renamed.Value;
                        var renameDateDay = new DateTime(renameDate.Year, renameDate.Month, renameDate.Day);

                        foreach (var tick in tickHistory.Where(tick => tick.Time >= previousRenameDate && previousRenameDateDay != TickDay(tick)))
                        {
                            // Prevents the aggregator from being updated with ticks after the rename event
                            if (TickDay(tick) > renameDateDay)
                            {
                                break;
                            }

                            if (tick.Time.Month != previousMonth)
                            {
                                output.Info.WriteLine($"\tSymbol[{count}]: Month: {tick.Time:MMMM}");
                                previousMonth = tick.Time.Month;
                                monthsTrading++;
                            }

                            foreach (var item in aggregators)
                            {
                                tick.Value = tick.Value / dividendsSplitsMaps.FinalSplitFactor;
                                item.Consolidator.Update(tick);
                            }

                            if (monthsTrading >= 6 && willBeDelisted && tick.Time > delistDate)
                            {
                                output.Warn.WriteLine($"\tSymbol[{count}]: {renamed.Key} delisted at {tick.Time:MMMM yyyy}");
                                break;
                            }
                        }

                        // count each stage as a point, so total points is 2*Symbol-count
                        // and the current progress is twice the current, but less one because we haven't finished writing data yet
                        progress = 100 * (2 * count - 1) / (2.0 * settings.SymbolCount);

                        output.Warn.WriteLine($"\tSymbol[{count}]: {renamed.Key} Progress: {progress:0.0}% - Saving data in LEAN format");

                        // persist consolidated data to disk
                        foreach (var item in aggregators)
                        {
                            var writer = new LeanDataWriter(item.Resolution, renamed.Key, Globals.DataFolder, item.TickType);

                            // send the flushed data into the writer. pulling the flushed list is very important,
                            // lest we likely wouldn't get the last piece of data stuck in the consolidator
                            // Filter out the data we're going to write here because filtering them in the consolidator update phase
                            // makes it write all dates for some unknown reason
                            writer.Write(item.Flush().Where(data => data.Time > previousRenameDate && previousRenameDateDay != DataDay(data)));
                        }

                        // update progress
                        progress = 100 * (2 * count) / (2.0 * settings.SymbolCount);
                        output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Progress: {progress:0.0}% - Symbol data generation and output completed");

                        previousSymbol = renamed.Key;
                        currentCount++;
                    }
                }
            }

            output.Info.WriteLine("Random data generation has completed.");

            DateTime TickDay(Tick tick) => new(tick.Time.Year, tick.Time.Month, tick.Time.Day);
            DateTime DataDay(BaseData data) => new(data.Time.Year, data.Time.Month, data.Time.Day);
        }


        public static IEnumerable<TickAggregator> CreateAggregators(RandomDataGeneratorSettings settings, TickType[] tickTypes)
        {
            // create default aggregators for tick type/resolution
            foreach (var tickAggregator in TickAggregator.ForTickTypes(settings.Resolution, tickTypes))
            {
                yield return tickAggregator;
            }


            // ensure we have a daily consolidator when coarse is enabled
            if (settings.IncludeCoarse && settings.Resolution != Resolution.Daily)
            {
                // prefer trades for coarse - in practice equity only does trades, but leaving this as configurable
                if (tickTypes.Contains(TickType.Trade))
                {
                    yield return TickAggregator.ForTickTypes(Resolution.Daily, TickType.Trade).Single();
                }
                else
                {
                    yield return TickAggregator.ForTickTypes(Resolution.Daily, TickType.Quote).Single();
                }
            }
        }
    }
}
