using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
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
            string includeCoarseString
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

                // define consolidators via settings
                var consolidators = settings.CreateConsolidators().ToList();

                // generate and consolidate data
                foreach (var tick in tickGenerator.GenerateTicks(symbol))
                {
                    if (tick.Time.Month != previousMonth)
                    {
                        output.Info.WriteLine($"\tMonth: {tick.Time:MMMM}");
                        previousMonth = tick.Time.Month;
                    }

                    foreach (var item in consolidators)
                    {
                        item.Consolidator.Update(tick);
                    }
                }

                var min = consolidators[0].Data[0];
                var max = consolidators[0].Data.Last();

                // count each stage as a point, so total points is 2*symbol-count
                // and the current progress is twice the current, but less one because we haven't finished writing data yet
                progress = 100*(2 * count - 1) / (2.0 * settings.SymbolCount);
                output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Progress: {progress:0.0}% - Saving data in LEAN format");

                // persist consolidated data to disk
                foreach (var item in consolidators)
                {
                    var writer = new LeanDataWriter(item.Resolution, symbol, Globals.DataFolder, item.TickType);

                    // IDataConsolidator defines its output as IBaseData, but they're all derived from BaseData,
                    // so this OfType<BaseData> is safe and will not filter any items out of the result set
                    writer.Write(item.Data.OfType<BaseData>());
                }

                // update progress
                progress = 100 * (2 * count) / (2.0 * settings.SymbolCount);
                output.Warn.WriteLine($"\tSymbol[{count}]: {symbol} Progress: {progress:0.0}% - Symbol data generation and output completed");
            }

            output.Info.WriteLine("Random data generation has completed.");
        }
    }
}
