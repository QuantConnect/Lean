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
using System.Linq;
using QuantConnect.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    using Processors = Dictionary<Symbol, List<AlgoSeekOptionsProcessor>>;
    /// <summary>
    /// Process a directory of algoseek option files into separate resolutions.
    /// </summary>
    public class AlgoSeekOptionsConverter
    {
        private string _source;
        private string _destination;
        private Resolution _resolution;
        private DateTime _referenceDate;

        private readonly ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 };

    /// <summary>
    /// Create a new instance of the AlgoSeekOptions Converter. Parse a single input directory into an output.
    /// </summary>
    /// <param name="resolution">Convert this resolution</param>
    /// <param name="referenceDate">Datetime to be added to the milliseconds since midnight. Algoseek data is stored in channel files (XX.bz2) and in a source directory</param>
    /// <param name="source">Source directory of the .bz algoseek files</param>
    /// <param name="destination">Data directory of LEAN</param>
    /// <param name="cache">Cache for the temporary serialized data</param>
        public AlgoSeekOptionsConverter(Resolution resolution, DateTime referenceDate, string source, string destination)
        {
            _source = source;
            _referenceDate = referenceDate;
            _destination = destination;
            _resolution = resolution;
        }

        /// <summary>
        /// Give the reference date and source directory, convert the algoseek options data into n-resolutions LEAN format.
        /// </summary>
        public void Convert()
        {
            //Get the list of all the files, then for each file open a separate streamer.
            var files = Directory.EnumerateFiles(_source, "*.bz2");
            Log.Trace("AlgoSeekOptionsConverter.Convert(): Loading {0} AlgoSeekOptionsReader for {1} ", files.Count(), _referenceDate);

            //Initialize parameters
            var totalLinesProcessed = 0L;
            var totalFiles = files.Count();
            var totalFilesProcessed = 0;
            var start = DateTime.MinValue;

            var zipper = OS.IsWindows ? "C:/Program Files/7-Zip/7z.exe" : "7z";
            var random = new Random((int)DateTime.Now.Ticks);

            //Extract each file massively in parallel.
            Parallel.ForEach(files, parallelOptions, file =>
            {
                var csvFile = file.Replace(".bz2", "");
                if (!File.Exists(csvFile))
                {
                    Log.Trace("AlgoSeekOptionsConverter.Convert(): Extracting " + file);
                    var psi = new ProcessStartInfo(zipper, " e " + file + " -o" + _source)
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var process = Process.Start(psi);
                    process.WaitForExit();
                    if (process.ExitCode > 0)
                    {
                        Log.Error("7Zip Exited Unsuccessfully: " + file);
                    }
                }

                // setting up local processors and the flush event
                var processors = new Processors();
                var waitForFlush = new ManualResetEvent(true);

                // symbol filters 
                // var symbolFilterNames = new string[] { "AAPL", "TWX", "NWSA", "FOXA", "AIG", "EGLE", "EGEC" };
                // var symbolFilter = symbolFilterNames.SelectMany(name => new[] { name, name + "1", name + ".1" }).ToHashSet();
                // var reader = new AlgoSeekOptionsReader(csvFile, _referenceDate, symbolFilter);

                var reader = new AlgoSeekOptionsReader(csvFile, _referenceDate);
                if (start == DateTime.MinValue)
                {
                    start = DateTime.Now;
                }

                var flushStep = TimeSpan.FromMinutes(15 + random.NextDouble() * 5);

                if (reader.Current != null) // reader contains the data
                {
                    var previousFlush = reader.Current.Time.RoundDown(flushStep);

                    do
                    {
                        var tick = reader.Current as Tick;

                        //If the next minute has clocked over; flush the consolidators; serialize and store data to disk.
                        if (tick.Time.RoundDown(flushStep) > previousFlush)
                        {
                            previousFlush = WriteToDisk(processors, waitForFlush, tick.Time, flushStep);
                            processors = new Processors();
                        }

                        //Add or create the consolidator-flush mechanism for symbol:
                        List<AlgoSeekOptionsProcessor> symbolProcessors;
                        if (!processors.TryGetValue(tick.Symbol, out symbolProcessors))
                        {
                            symbolProcessors = new List<AlgoSeekOptionsProcessor>(2)
                                        {
                                            new AlgoSeekOptionsProcessor(tick.Symbol, _referenceDate, TickType.Trade, _resolution, _destination),
                                            new AlgoSeekOptionsProcessor(tick.Symbol, _referenceDate, TickType.Quote, _resolution, _destination)
                                        };

                            processors[tick.Symbol] = symbolProcessors;
                        }

                        // Pass current tick into processor: enum 0 = trade; 1 = quote.
                        symbolProcessors[(int)tick.TickType].Process(tick);

                        if (Interlocked.Increment(ref totalLinesProcessed) % 1000000m == 0)
                        {
                            Log.Trace("AlgoSeekOptionsConverter.Convert(): Processed {0,3}M ticks( {1}k / sec); Memory in use: {2} MB; Total progress: {3}%", Math.Round(totalLinesProcessed / 1000000m, 2), Math.Round(totalLinesProcessed / 1000L / (DateTime.Now - start).TotalSeconds), Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024), 100 * totalFilesProcessed / totalFiles);
                        }

                    }
                    while (reader.MoveNext());

                    Log.Trace("AlgoSeekOptionsConverter.Convert(): Performing final flush to disk... ");
                    Flush(processors, DateTime.MaxValue, true);
                    WriteToDisk(processors, waitForFlush, DateTime.MaxValue, flushStep, true);
                }

                Log.Trace("AlgoSeekOptionsConverter.Convert(): Finished processing file: " + file);
                Interlocked.Increment(ref totalFilesProcessed);
            });

          
        }

        /// <summary>
        /// Write the processor queues to disk
        /// </summary>
        /// <param name="peekTickTime">Time of the next tick in the stream</param>
        /// <param name="step">Period between flushes to disk</param>
        /// <param name="final">Final push to disk</param>
        /// <returns></returns>
        private DateTime WriteToDisk(Processors processors, ManualResetEvent waitForFlush, DateTime peekTickTime, TimeSpan step, bool final = false)
        {
            waitForFlush.WaitOne();
            waitForFlush.Reset();
            Flush(processors, peekTickTime, final);

            Task.Run(() =>
            {
                foreach (var type in Enum.GetValues(typeof(TickType)))
                {
                    var tickType = type;
                    var groups = processors.Values.Select(x => x[(int) tickType]).Where(x => x.Queue.Count > 0).GroupBy(process => process.Symbol.Underlying.Value);

                    Parallel.ForEach(groups, group =>
                    {
                        var symbol = group.Key;
                        var zip = group.First().ZipPath.Replace(".zip", string.Empty);

                        foreach (var processor in group)
                        {
                            var tempFileName = Path.Combine(zip, processor.EntryPath);

                            Directory.CreateDirectory(zip);
                            File.AppendAllText(tempFileName, FileBuilder(processor));
                        }
                    });
                }
                waitForFlush.Set();
            });

            //Pause while writing the final flush.
            if (final) waitForFlush.WaitOne();

            return peekTickTime.RoundDown(step);
        }


        /// <summary>
        /// Output a list of basedata objects into a string csv line.
        /// </summary>
        /// <param name="processor"></param>
        /// <returns></returns>
        private string FileBuilder(AlgoSeekOptionsProcessor processor)
        {
            var sb = new StringBuilder();
            foreach (var data in processor.Queue)
            {
                sb.AppendLine(LeanData.GenerateLine(data, SecurityType.Option, processor.Resolution));
            }
            return sb.ToString();
        }

        private void Flush(Processors processors, DateTime time, bool final)
        {
            foreach (var symbol in processors.Keys)
            {
                processors[symbol].ForEach(x => x.FlushBuffer(time, final));
            }
        }

        /// <summary>
        /// Compress the queue buffers directly to a zip file. Lightening fast as streaming ram-> compressed zip.
        /// </summary>
        public void Package(DateTime date)
        {
            var zipper = OS.IsWindows ? "C:/Program Files/7-Zip/7z.exe" : "7z";

            Log.Trace("AlgoSeekOptionsConverter.Package(): Zipping all files ...");

            var destination = Path.Combine(_destination, "option");
            var dateMask = date.ToString(DateFormat.EightCharacter);

            var files =
                Directory.EnumerateFiles(destination, dateMask + "*.csv", SearchOption.AllDirectories)
                .GroupBy(x => Directory.GetParent(x).FullName);

            //Zip each file massively in parallel.
            Parallel.ForEach(files, parallelOptions, file =>
            {
                var outputFileName = file.Key + ".zip";
                var inputFileNames = Path.Combine(file.Key, "*.csv");
                var cmdArgs = " a " + outputFileName + " " + inputFileNames;

                Log.Trace("AlgoSeekOptionsConverter.Convert(): Zipping " + outputFileName);
                var psi = new ProcessStartInfo(zipper, cmdArgs)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = Process.Start(psi);
                process.WaitForExit();
                if (process.ExitCode > 0)
                {
                    Log.Error("7Zip Exited Unsuccessfully: " + outputFileName);
                }
                else
                {
                    try
                    {
                        Directory.Delete(file.Key, true);
                    }
                    catch (Exception err)
                    {
                        Log.Error("Directory.Delete returned error: " + err.Message);
                    }
                }
            });
        }

    }
}