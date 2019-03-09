using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

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
            string quoteTradeRatioString
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
                output
            );

            GenerateRandomData(settings, output);

            if (settings.IncludeCoarse && settings.SecurityType == SecurityType.Equity)
            {
                output.Info.WriteLine("Launching coarse data generator...");
                var coarseFiles = CoarseUniverseGenerator.CoarseUniverseGeneratorProgram.ProcessEquityDirectories(
                    Globals.DataFolder,
                    false,
                    settings.Start
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
            var randomValueGenerator = new RandomValueGenerator();
            var symbolGenerator = new SymbolGenerator(settings, randomValueGenerator);
            var tickGenerator = new TickGenerator(settings, randomValueGenerator);

            output.Warn.WriteLine($"Begin data generation of {settings.SymbolCount} randomly generated {settings.SecurityType} assets...");

            // iterate over our randomly generated symbols
            var count = 0;
            var progress = 0d;
            var previousMonth = -1;
            foreach (var symbol in symbolGenerator.GenerateRandomSymbols())
            {
                output.Warn.WriteLine($"\tSymbol[{++count}]: {symbol} Progress: {progress:0.0}% - Generating data...");

                // define aggregators via settings
                var aggregators = settings.CreateAggregators().ToList();

                // generate and consolidate data
                foreach (var tick in tickGenerator.GenerateTicks(symbol))
                {
                    if (tick.Time.Month != previousMonth)
                    {
                        output.Info.WriteLine($"\tMonth: {tick.Time:MMMM}");
                        previousMonth = tick.Time.Month;
                    }

                    foreach (var item in aggregators)
                    {
                        item.Consolidator.Update(tick);
                    }
                }

                // count each stage as a point, so total points is 2*symbol-count
                // and the current progress is twice the current, but less one because we haven't finished writing data yet
                progress = 100*(2 * count - 1) / (2.0 * settings.SymbolCount);
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
