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

namespace QuantConnect.ToolBox.AlgoSeekFuturesConverter
{
    /// <summary>
    /// Process a directory of algoseek option files into separate resolutions.
    /// </summary>
    public class AlgoSeekFuturesConverter
    {
        private string _source;
        private string _destination;
        private Resolution _resolution;
        private DateTime _referenceDate;
        private ManualResetEvent _waitForFlush;
        private Dictionary<Symbol, List<AlgoSeekFuturesProcessor>> _processors;

        /// <summary>
        /// Create a new instance of the AlgoSeekFutures Converter. Parse a single input directory into an output.
        /// </summary>
        /// <param name="resolution">Convert this resolution</param>
        /// <param name="referenceDate">Datetime to be added to the milliseconds since midnight. Algoseek data is stored in channel files (XX.bz2) and in a source directory</param>
        /// <param name="source">Source directory of the .bz algoseek files</param>
        /// <param name="destination">Data directory of LEAN</param>
        /// <param name="cache">Cache for the temporary serialized data</param>
        public AlgoSeekFuturesConverter(Resolution resolution, DateTime referenceDate, string source, string destination)
        {
            _source = source;
            _referenceDate = referenceDate;
            _destination = destination;
            _resolution = resolution;
            _processors = new Dictionary<Symbol, List<AlgoSeekFuturesProcessor>>();
            _waitForFlush = new ManualResetEvent(true);
        }

        /// <summary>
        /// Give the reference date and source directory, convert the algoseek options data into n-resolutions LEAN format.
        /// </summary>
        public void Convert()
        {
            //Get the list of all the files, then for each file open a separate streamer.
            var files = Directory.EnumerateFiles(_source, "*.bz2");
            Log.Trace("AlgoSeekFuturesConverter.Convert(): Loading {0} AlgoSeekFuturesReader for {1} ", files.Count(), _referenceDate);

            //Initialize parameters
            var totalLinesProcessed = 0L;
            var frontier = DateTime.MinValue;
            var estimatedEndTime = _referenceDate.AddHours(16);
            var zipper = OS.IsWindows ? "C:/Program Files/7-Zip/7z.exe" : "7z";

            files = files.Where(x => Path.GetFileNameWithoutExtension(x).ToLower().IndexOf("option") == -1);
            
            //Extract each file massively in parallel.
            Parallel.ForEach(files,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 },
                file =>
            {
                if (File.Exists(file.Replace(".bz2", ""))) return;
                Log.Trace("AlgoSeekFuturesConverter.Convert(): Extracting " + file);
                var psi = new ProcessStartInfo(zipper, " e " + file + " -o" + _source)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = Process.Start(psi);
                process.WaitForExit();
                if (process.ExitCode > 0)
                {
                    throw new Exception("7Zip Exited Unsuccessfully: " + file);
                }
            });

            //Fetch the new CSV files:
            files = Directory.EnumerateFiles(_source, "*.csv");
            if (!files.Any()) throw new Exception("No csv files found");

            // Create multithreaded readers; start them in threads and store the ticks in queues
            var readers = files.Select(file => new AlgoSeekFuturesReader(file));
            var synchronizer = new SynchronizingEnumerator(readers);

            Log.Trace("AlgoSeekFuturesConverter.Convert(): Synchronizing and processing ticks...", files.Count(), _referenceDate);

            // Prime the synchronizer if required:
            if (synchronizer.Current == null)
            {
                synchronizer.MoveNext();
            }

            var start = DateTime.Now;

            // Store time:
            var flushStep = TimeSpan.FromMinutes(5);
            var previousFlush = synchronizer.Current.Time.RoundDown(flushStep);

            do
            {
                var tick = synchronizer.Current as Tick;

                //If the next minute has clocked over; flush the consolidators; serialize and store data to disk.
                if (tick.Time.RoundDown(flushStep) > previousFlush)
                {
                    previousFlush = WriteToDisk(tick.Time, previousFlush, flushStep);
                }

                frontier = tick.Time;

                //Add or create the consolidator-flush mechanism for symbol:
                List<AlgoSeekFuturesProcessor> symbolProcessors;
                if (!_processors.TryGetValue(tick.Symbol, out symbolProcessors))
                {
                    symbolProcessors = new List<AlgoSeekFuturesProcessor>(2)
                    {
                        new AlgoSeekFuturesProcessor(tick.Symbol, _referenceDate, TickType.Trade, _resolution, _destination),
                        new AlgoSeekFuturesProcessor(tick.Symbol, _referenceDate, TickType.Quote, _resolution, _destination)
                    };
                    _processors[tick.Symbol] = symbolProcessors;
                }

                // Pass current tick into processor: enum 0 = trade; 1 = quote.
                symbolProcessors[ (int)tick.TickType ].Process(tick);

                totalLinesProcessed++;
                if (totalLinesProcessed % 1000000m == 0)
                {
                    var completed = Math.Round(1 - (estimatedEndTime - frontier).TotalMinutes / TimeSpan.FromHours(6.5).TotalMinutes, 3);
                    Log.Trace("AlgoSeekFuturesConverter.Convert(): Processed {0,3}M ticks( {1}k / sec ); Memory in use: {2} MB; Frontier Time: {3}; Completed: {4:P3}. ASOP Count: {5}", Math.Round(totalLinesProcessed / 1000000m, 2), Math.Round(totalLinesProcessed / 1000L / (DateTime.Now - start).TotalSeconds), Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024), frontier.ToString("u"), completed, _processors.Count);
                }

            }
            while (synchronizer.MoveNext());

            Log.Trace("AlgoSeekFuturesConverter.Convert(): Performing final flush to disk... ");
            Flush(DateTime.MaxValue, true);
            WriteToDisk(DateTime.MaxValue, previousFlush, flushStep, true);

            //Tidy up any existing files:
            Log.Trace("AlgoSeekFuturesConverter.Convert(): Cleaning temporary files...");
            //Directory.EnumerateFiles(_source, "*.csv").ToList().ForEach(file => { File.Delete(file); });

            Log.Trace("AlgoSeekFuturesConverter.Convert(): Finished processing directory: " + _source);
        }

        /// <summary>
        /// Write the processor queues to disk
        /// </summary>
        /// <param name="peekTickTime">Time of the next tick in the stream</param>
        /// <param name="previousFlush"></param>
        /// <param name="step">Period between flushes to disk</param>
        /// <param name="final">Final push to disk</param>
        /// <returns></returns>
        private DateTime WriteToDisk(DateTime peekTickTime, DateTime previousFlush, TimeSpan step, bool final = false)
        {
            _waitForFlush.WaitOne();
            _waitForFlush.Reset();
            Flush(peekTickTime, final);

            //Save off the object;
            var temp = _processors;
            var previousFlushTime = previousFlush;
            Task.Run(() =>
            {
                foreach (var type in Enum.GetValues(typeof(TickType)))
                {
                    var tickType = type;
                    var groups = temp.Values.Select(x => x[(int)tickType]).Where(x => x.Queue.Count > 0).GroupBy(process => process.Symbol.Underlying.Value);

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
                _waitForFlush.Set();
            });
            _processors = new Dictionary<Symbol, List<AlgoSeekFuturesProcessor>>();

            //Pause while writing the final flush.
            if (final) _waitForFlush.WaitOne();

            return peekTickTime.RoundDown(step);
        }


        /// <summary>
        /// Compress the queue buffers directly to a zip file. Lightening fast as streaming ram-> compressed zip.
        /// </summary>
        public void Package(DateTime date)
        {
            var count = 0;
            var zipper = OS.IsWindows ? "C:/Program Files/7-Zip/7z.exe" : "7z";

            Log.Trace("AlgoSeekOptionsConverter.Package(): Zipping all files ...");

            var files =
                Directory.EnumerateFiles(_destination, "*.csv", SearchOption.AllDirectories)
                .GroupBy(x => Directory.GetParent(x).FullName);

            //Zip each file massively in parallel.
            Parallel.ForEach(files,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 },
                file =>
            {
                var outputFileName = file.Key + ".zip";
                var inputFileNames = Path.Combine(file.Key, "*.csv");
                var cmdArgs = " a " + outputFileName + " " + inputFileNames;

                Log.Trace("AlgoSeekFuturesConverter.Convert(): Zipping " + outputFileName);
                var psi = new ProcessStartInfo(zipper, cmdArgs)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = Process.Start(psi);
                process.WaitForExit();
                if (process.ExitCode > 0)
                {
                    throw new Exception("7Zip Exited Unsuccessfully: " + outputFileName);
                }
                else
                {
                    Directory.Delete(file.Key, true);
                }
            });
        }
        /// <summary>
        /// Output a list of basedata objects into a string csv line.
        /// </summary>
        /// <param name="processor"></param>
        /// <returns></returns>
        private string FileBuilder(AlgoSeekFuturesProcessor processor)
        {
            var sb = new StringBuilder();
            foreach (var data in processor.Queue)
            {
                sb.AppendLine(LeanData.GenerateLine(data, SecurityType.Future, processor.Resolution));
            }
            return sb.ToString();
        }

        private void Flush(DateTime time, bool final)
        {
            foreach (var symbol in _processors.Keys)
            {
                _processors[symbol].ForEach(x => x.FlushBuffer(time, final));
            }
        }

        /// <summary>
        /// Data Transfer Class
        /// </summary>
        private class AlgoSeekOptionSerializationTransfer
        {
            public List<AlgoSeekFuturesProcessor> Processors;
        }
    }
}