using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random data according to the specified parameters
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
            string dividendEveryQuarterPercentageString
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

                output
            );

            GenerateRandomData(settings, output);

            if (settings.IncludeCoarse && settings.SecurityType == SecurityType.Equity)
            {
                output.Info.WriteLine("Launching coarse data generator...");
                var coarseFiles = CoarseUniverseGenerator.CoarseUniverseGeneratorProgram.ProcessEquityDirectories(
                    Globals.DataFolder,
                    false
                ).ToList();
                output.Info.WriteLine("Coarse data generation completed. Produced the following files:");
                foreach (var coarseFile in coarseFiles)
                {
                    output.Info.WriteLine($"Generated coarse file: {coarseFile}");
                }
            }

            output.Info.WriteLine("Press any key to exit...");
            Console.ReadKey();
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
            var symbolGenerator = new SymbolGenerator(settings, randomValueGenerator);
            var tickGenerator = new TickGenerator(settings, randomValueGenerator);

            output.Warn.WriteLine($"Begin data generation of {settings.SymbolCount} randomly generated {settings.SecurityType} assets...");

            // iterate over our randomly generated symbols
            var count = 0;
            var progress = 0d;
            var previousMonth = -1;
            var previousDay = settings.Start;

            Func<Tick, DateTime> tickDay = (tick => new DateTime(tick.Time.Year, tick.Time.Month, tick.Time.Day));
            Func<Data.BaseData, DateTime> dataDay = (data => new DateTime(data.Time.Year, data.Time.Month, data.Time.Day));

            foreach (var currentSymbol in symbolGenerator.GenerateRandomSymbols())
            {
                // This is done so that we can update the symbol in the case of a rename event
                var symbol = currentSymbol;
                var willBeDelisted = randomValueGenerator.NextBool(1.0);
                var delistDate = randomValueGenerator.NextDate(settings.Start.AddMonths(6), settings.End, null);
                var monthsTrading = 0;

                // Keep track of renamed symbols and the time they were renamed. 
                var renamedSymbols = new Dictionary<Symbol, DateTime>();

                output.Warn.WriteLine($"\tSymbol[{++count}]: {symbol} Progress: {progress:0.0}% - Generating data...");

                // define aggregators via settings
                var aggregators = settings.CreateAggregators().ToList();
                var tickHistory = tickGenerator.GenerateTicks(symbol).ToList();

                var dividendsSplitsMaps = new DividendSplitMapGenerator(
                    symbol, 
                    settings, 
                    randomValueGenerator, 
                    random, 
                    delistDate, 
                    willBeDelisted);

                if (settings.SecurityType == SecurityType.Equity)
                {
                    dividendsSplitsMaps.GenerateSplitsDividends(tickHistory);

                    if (!willBeDelisted)
                    {
                        dividendsSplitsMaps.DividendsSplits.Add(new FactorFileRow(new DateTime(2050, 12, 31), 1m, 1m));

                        if (dividendsSplitsMaps.MapRows.Count > 1)
                        {
                            // Remove the last element if we're going to have a 20501231 entry
                            dividendsSplitsMaps.MapRows.RemoveAt(dividendsSplitsMaps.MapRows.Count - 1);
                        }
                        dividendsSplitsMaps.MapRows.Add(new MapFileRow(new DateTime(2050, 12, 31), dividendsSplitsMaps.CurrentSymbol.Value));
                    }

                    // If the symbol value has changed, update the current symbol
                    if (symbol != dividendsSplitsMaps.CurrentSymbol)
                    {
                        // Add all symbol rename events to dictionary
                        foreach (var renameEvent in dividendsSplitsMaps.MapRows)
                        {
                            // Symbol.UpdateMappedSymbol does not update the underlying security ID symbol, which 
                            // is used to create the hash code. Create a new equity symbol from scratch instead.
                            symbol = Symbol.Create(renameEvent.MappedSymbol, SecurityType.Equity, settings.Market);
                            renamedSymbols.Add(symbol, renameEvent.Date);

                            output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} will be renamed on {renameEvent.Date}");
                        }
                    }
                    else
                    {
                        // This ensures that ticks will be written for the current symbol up until 9999-12-31
                        renamedSymbols.Add(symbol, new DateTime(9999, 12, 31));
                    }

                    symbol = dividendsSplitsMaps.CurrentSymbol;

                    // Write Splits and Dividend events to directory factor_files
                    var factorFile = new FactorFile(symbol.Value, dividendsSplitsMaps.DividendsSplits, settings.Start);
                    var mapFile = new MapFile(symbol.Value, dividendsSplitsMaps.MapRows);

                    factorFile.WriteToCsv(symbol);
                    mapFile.WriteToCsv(settings.Market);

                    output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Dividends, splits, and map files have been written to disk.");
                }

                Symbol previousSymbol = null;
                var currentCount = 0;

                foreach (var renamed in renamedSymbols) 
                {
                    var previousRenameDate = previousSymbol == null ? new DateTime(1, 1, 1) : renamedSymbols[previousSymbol];
                    var previousRenameDateDay = new DateTime(previousRenameDate.Year, previousRenameDate.Month, previousRenameDate.Day);
                    var renameDate = renamed.Value;
                    var renameDateDay = new DateTime(renameDate.Year, renameDate.Month, renameDate.Day);

                    foreach (var tick in tickHistory.Where(tick => tick.Time >= previousRenameDate && previousRenameDateDay != tickDay(tick)))
                    {
                        // Prevents the aggregator from being updated with ticks after the rename event
                        if (tickDay(tick) > renameDateDay)
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

                    // count each stage as a point, so total points is 2*symbol-count
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
                        writer.Write(item.Flush().Where(data => data.Time > previousRenameDate && previousRenameDateDay != dataDay(data)));
                    }

                    // update progress
                    progress = 100 * (2 * count) / (2.0 * settings.SymbolCount);
                    output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Progress: {progress:0.0}% - Symbol data generation and output completed");

                    previousSymbol = renamed.Key;
                    currentCount++;
                }
            }

            output.Info.WriteLine("Random data generation has completed.");
        }
    }
}
