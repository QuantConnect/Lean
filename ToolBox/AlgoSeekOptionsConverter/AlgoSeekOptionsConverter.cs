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
using System.IO;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    public class AlgoSeekOptionsConverter
    {
        public static readonly Resolution[] FineResolutions = { Resolution.Tick, Resolution.Second, Resolution.Minute };
        public static readonly Resolution[] CoarseResolutions = { Resolution.Hour, Resolution.Daily };

        public long CurrentMemoryUsage
        {
            get { return Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024); }
        }

        public void ConvertToFineResolution(string sourceDirectory, string destinationDirectory, long flushInterval)
        {
            var files = Directory.EnumerateFiles(sourceDirectory).OrderByDescending(x => new FileInfo(x).Length);
            var referenceDate = DateTime.ParseExact(new DirectoryInfo(sourceDirectory).Name, DateFormat.EightCharacter, null);
            var optionsReaders = files.Select(file => new AlgoSeekOptionsReader(file, referenceDate)).ToList();

            var quoteProcessor = new AlgoSeekOptionsProcessor(TickType.Quote, FineResolutions, destinationDirectory, x => ((Tick) x).TickType == TickType.Quote);
            var tradeProcessor = new AlgoSeekOptionsProcessor(TickType.Trade, FineResolutions, destinationDirectory, x => ((Tick) x).TickType == TickType.Trade);

            var totalLinesProcessed = 0L;
            var frontierTime = referenceDate;
            while (optionsReaders.Any(reader => reader.HasNext))
            {
                var activeReaders = optionsReaders.Where(reader => reader.HasNext);
                var nextTick = activeReaders.EarliestTick().Take();
                frontierTime = nextTick.Time;

                quoteProcessor.Process(nextTick);
                tradeProcessor.Process(nextTick);

                totalLinesProcessed++;
                if (totalLinesProcessed % flushInterval == 0)
                {
                    Log.Trace("Processed {0,3}M lines; Memory in use: {1} MB", totalLinesProcessed / flushInterval, CurrentMemoryUsage);

                    quoteProcessor.FlushToDisk(frontierTime);
                    tradeProcessor.FlushToDisk(frontierTime);
                }
            }

            quoteProcessor.FlushToDisk(frontierTime, true);
            tradeProcessor.FlushToDisk(frontierTime, true);

            Log.Trace("Finished processing directory: " + sourceDirectory);
            Log.Trace("Number of Quotes: " + quoteProcessor.GetStats());
            Log.Trace("Number of Trades: " + tradeProcessor.GetStats());
        }

        public void Compress(string dataDirectory, int parallelism)
        {
            Log.Trace("Begin compressing csv files");

            var root = Path.Combine(dataDirectory, "option", "usa");
            var fine =
                from res in FineResolutions
                let path = Path.Combine(root, res.ToLower())
                from sym in Directory.EnumerateDirectories(path)
                from dir in Directory.EnumerateDirectories(sym)
                select new DirectoryInfo(dir).FullName;

            var coarse =
                from res in CoarseResolutions
                let path = Path.Combine(root, res.ToLower())
                from dir in Directory.EnumerateDirectories(path)
                select new DirectoryInfo(dir).FullName;

            var all = fine.Union(coarse);

            var options = new ParallelOptions {MaxDegreeOfParallelism = parallelism};
            Parallel.ForEach(all, options, dir =>
            {
                try
                {
                    // zip the contents of the directory and then delete the directory
                    Compression.ZipDirectory(dir, dir + ".zip", false);
                    Directory.Delete(dir, true);
                    Log.Trace("Processed: " + dir);
                }
                catch (Exception err)
                {
                    Log.Error(err, "Zipping " + dir);
                }
            });
        }

        public void ExtractSymbol(string symbol, string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            var inputFiles = Directory.EnumerateFiles(sourceDirectory).OrderByDescending(x => new FileInfo(x).Length);
            Parallel.ForEach(inputFiles, inputFile =>
            {
                var fileType = Path.GetExtension(inputFile);
                var streamProvider = StreamProvider.ForExtension(fileType);

                var inputStream = streamProvider.Open(inputFile).First();
                var inputReader = new StreamReader(inputStream);

                var outputFile = Path.Combine(destinationDirectory, Path.GetFileNameWithoutExtension(inputFile));
                var outputWriter = new StreamWriter(outputFile);

                var count = 0L;
                var logInterval = 1000000L;
                while (inputReader.Peek() != -1)
                {
                    var line = inputReader.ReadLine();
                    count++;
                    if (count % logInterval == 0)
                    {
                        Log.Trace("({0}): Parsed {1,3}M lines", inputFile, count / logInterval);
                    }
                    const int columns = 11;
                    var csv = line.ToCsv(columns);
                    if (csv.Count < columns)
                    {
                        continue;
                    }
                    var underlying = csv[4];
                    if (underlying.ToLower().StartsWith(symbol))
                    {
                        outputWriter.WriteLine(line);
                    }
                }
                inputReader.Dispose();
                inputStream.Dispose();
                outputWriter.Dispose();
            });
        }
    }

    internal static partial class Extensions
    {
        public static AlgoSeekOptionsReader EarliestTick(this IEnumerable<AlgoSeekOptionsReader> readers)
        {
            return readers.Aggregate((earliestTick, nextCandidate) => earliestTick.Current.Time <= nextCandidate.Current.Time ? earliestTick : nextCandidate);
        }
    }
}