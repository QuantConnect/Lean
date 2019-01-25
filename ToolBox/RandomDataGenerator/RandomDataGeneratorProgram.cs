using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Auxiliary;

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
            string randomSeed
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

            foreach (var currentSymbol in symbolGenerator.GenerateRandomSymbols())
            {
                // This is done so that we can update the symbol in the case of a rename event
                var symbol = currentSymbol;
                var willBeDelisted = randomValueGenerator.NextBool(1.0);
                var delistDate = randomValueGenerator.NextDate(settings.Start.AddMonths(6), settings.End, null);
                var monthsTrading = 0;

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
                    // If the symbol value has changed, update the current symbol
                    if (symbol != dividendsSplitsMaps.CurrentSymbol)
                    {
                        output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} will be renamed to {dividendsSplitsMaps.CurrentSymbol}");
                    }
                    symbol = dividendsSplitsMaps.CurrentSymbol;

                    if (!willBeDelisted)
                    {
                        dividendsSplitsMaps.DividendsSplits.Add(new FactorFileRow(new DateTime(2050, 12, 31), 1m, 1m));
                        dividendsSplitsMaps.MapRows.Add(new MapFileRow(new DateTime(2050, 12, 31), symbol.Value));
                    }

                    // Write Splits and Dividend events to directory factor_files
                    var factorFile = new FactorFile(symbol.Value, dividendsSplitsMaps.DividendsSplits, settings.Start);
                    var mapFile = new MapFile(symbol.Value, dividendsSplitsMaps.MapRows);

                    factorFile.WriteToCsv(symbol);
                    mapFile.WriteToCsv(settings.Market);

                    output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Dividends, splits, and map files have been written to disk.");
                }

                foreach (var tick in tickHistory)
                {
                    if (tick.Time.Month != previousMonth)
                    {
                        output.Info.WriteLine($"\tMonth: {tick.Time:MMMM}");
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
                        output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} delisted at {tick.Time:MMMM yyyy}");
                        break;
                    }
                }

                // count each stage as a point, so total points is 2*symbol-count
                // and the current progress is twice the current, but less one because we haven't finished writing data yet
                progress = 100*(2 * count - 1) / (2.0 * settings.SymbolCount);

                output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} maps, splits, and dividends have been saved");
                output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Progress: {progress:0.0}% - Saving data in LEAN format");

                // persist consolidated data to disk
                foreach (var item in aggregators)
                {
                    var writer = new LeanDataWriter(item.Resolution, symbol, Globals.DataFolder, item.TickType);

                    // send the flushed data into the writer. pulling the flushed list is very important,
                    // lest we likely wouldn't get the last piece of data stuck in the consolidator
                    writer.Write(item.Flush());
                }

                // update progress
                progress = 100 * (2 * count) / (2.0 * settings.SymbolCount);
                output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Progress: {progress:0.0}% - Symbol data generation and output completed");
            }

            output.Info.WriteLine("Random data generation has completed.");
        }
    }
}
