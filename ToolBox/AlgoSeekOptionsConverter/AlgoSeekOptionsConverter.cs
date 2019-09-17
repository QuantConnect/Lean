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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
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
        private string _remote;
        private string _remoteMask;
        private string _destination;
        private Resolution _resolution;
        private DateTime _referenceDate;

        private readonly ParallelOptions parallelOptionsProcessing = new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount};
        private readonly ParallelOptions parallelOptionsZipping = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 };

        /// <summary>
        /// Create a new instance of the AlgoSeekOptions Converter. Parse a single input directory into an output.
        /// </summary>
        /// <param name="resolution">Convert this resolution</param>
        /// <param name="referenceDate">Datetime to be added to the milliseconds since midnight. Algoseek data is stored in channel files (XX.bz2) and in a source directory</param>
        /// <param name="source">Remote directory of the .bz algoseek files</param>
        /// <param name="source">Source directory of the .csv algoseek files</param>
        /// <param name="destination">Data directory of LEAN</param>
        public AlgoSeekOptionsConverter(Resolution resolution, DateTime referenceDate, string remote, string remoteMask, string source, string destination)
        {
            _remote = remote;
            _remoteMask = remoteMask;
            _source = source;
            _referenceDate = referenceDate;
            _destination = destination;
            _resolution = resolution;
        }

        /// <summary>
        /// Give the reference date and source directory, convert the algoseek options data into n-resolutions LEAN format.
        /// </summary>
        /// <param name="symbolFilter">HashSet of symbols as string to process. *Only used for testing*</param>
        public void Convert(HashSet<string> symbolFilter = null)
        {
            //Get the list of all the files, then for each file open a separate streamer.
            var compressedRawDatafiles = Directory.EnumerateFiles(_remote, _remoteMask).Select(f => new FileInfo(f)).ToList();
            var rawDatafiles = new List<FileInfo>();

            Log.Trace("AlgoSeekOptionsConverter.Convert(): Loading {0} AlgoSeekOptionsReader for {1} ", compressedRawDatafiles.Count, _referenceDate);

            //Initialize parameters
            var totalLinesProcessed = 0L;
            var totalFiles = compressedRawDatafiles.Count;
            var totalFilesProcessed = 0;
            var start = DateTime.MinValue;

            foreach (var compressedRawDatafile in compressedRawDatafiles)
            {
                var counter = 1;
                var timer = DateTime.UtcNow;
                var rawDataFile = new FileInfo(Path.Combine(_source, compressedRawDatafile.Name.Replace(".bz2", "")));
                var decompressSuccessful = false;
                do
                {
                    var attempt = counter == 1 ? string.Empty : $" attempt {counter} of 3";
                    Log.Trace($"AlgoSeekOptionsConverter.Convert(): Extracting {compressedRawDatafile.Name}{attempt}.");
                    decompressSuccessful = DecompressOpraFile(compressedRawDatafile, rawDataFile);
                    counter++;
                } while (!decompressSuccessful && counter <= 3);

                if (!decompressSuccessful)
                {
                    Log.Error($"Error decompressing {compressedRawDatafile}. Process Stop.");
                    throw new NotImplementedException();
                }

                Log.Trace($"AlgoSeekOptionsConverter.Convert(): Extraction successful in {DateTime.UtcNow - timer:g}.");
                rawDatafiles.Add(rawDataFile);
            }

            //Process each file massively in parallel.
            Parallel.ForEach(rawDatafiles, parallelOptionsProcessing, rawDataFile =>
            {
                Log.Trace("Source File :" + rawDataFile.Name);

                // setting up local processors and the flush event
                var processors = new Processors();
                var waitForFlush = new ManualResetEvent(true);

                // symbol filters
                // var symbolFilterNames = new string[] { "AAPL", "TWX", "NWSA", "FOXA", "AIG", "EGLE", "EGEC" };
                // var symbolFilter = symbolFilterNames.SelectMany(name => new[] { name, name + "1", name + ".1" }).ToHashSet();
                // var reader = new AlgoSeekOptionsReader(csvFile, _referenceDate, symbolFilter);

                using (var reader = new AlgoSeekOptionsReader(rawDataFile.FullName, _referenceDate, symbolFilter))
                {
                    if (start == DateTime.MinValue)
                    {
                        start = DateTime.Now;
                    }
                    var flushStep = TimeSpan.FromMinutes(10);
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
                                symbolProcessors = new List<AlgoSeekOptionsProcessor>(3)
                                {
                                    new AlgoSeekOptionsProcessor(tick.Symbol, _referenceDate, TickType.Trade, _resolution, _destination),
                                    new AlgoSeekOptionsProcessor(tick.Symbol, _referenceDate, TickType.Quote, _resolution, _destination),
                                    new AlgoSeekOptionsProcessor(tick.Symbol, _referenceDate, TickType.OpenInterest, _resolution, _destination)
                                };
                                processors[tick.Symbol] = symbolProcessors;
                            }
                            // Pass current tick into processor: enum 0 = trade; 1 = quote, , 2 = oi
                            symbolProcessors[(int)tick.TickType].Process(tick);
                            if (Interlocked.Increment(ref totalLinesProcessed) % 1000000m == 0)
                            {
                                Log.Trace(
                                    "AlgoSeekOptionsConverter.Convert(): Processed {0,3}M ticks( {1}k / sec); Memory in use: {2} MB; Total progress: {3}%",
                                    Math.Round(totalLinesProcessed / 1000000m, 2),
                                    Math.Round(totalLinesProcessed / 1000L / (DateTime.Now - start).TotalSeconds),
                                    Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024),
                                    100 * totalFilesProcessed / totalFiles);
                            }
                        } while (reader.MoveNext());
                        Log.Trace("AlgoSeekOptionsConverter.Convert(): Performing final flush to disk... ");
                        Flush(processors, DateTime.MaxValue, true);
                        WriteToDisk(processors, waitForFlush, DateTime.MaxValue, flushStep, true);
                    }
                    Log.Trace("AlgoSeekOptionsConverter.Convert(): Cleaning up extracted options file {0}", rawDataFile.FullName);
                }
                rawDataFile.Delete();
                processors = null;
                Log.Trace("AlgoSeekOptionsConverter.Convert(): Finished processing file: " + rawDataFile);
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
                try
                {
                    foreach (var type in Enum.GetValues(typeof(TickType)))
                    {
                        var tickType = type;
                        var groups = processors.Values.Select(x => x[(int)tickType]).Where(x => x.Queue.Count > 0).GroupBy(process => process.Symbol.Underlying.Value);

                        Parallel.ForEach(groups, group =>
                        {
                            string zip = string.Empty;

                            try
                            {
                                var symbol = group.Key;
                                zip = group.First().ZipPath.Replace(".zip", string.Empty);

                                foreach (var processor in group)
                                {
                                    var tempFileName = Path.Combine(zip, processor.EntryPath);

                                    Directory.CreateDirectory(zip);
                                    File.AppendAllText(tempFileName, FileBuilder(processor));
                                }
                            }
                            catch (Exception err)
                            {
                                Log.Error("AlgoSeekOptionsConverter.WriteToDisk() returned error: " + err.Message + " zip name: " + zip);
                            }
                        });
                    }
                }
                catch (Exception err)
                {
                    Log.Error("AlgoSeekOptionsConverter.WriteToDisk() returned error: " + err.Message);
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
            Log.Trace("AlgoSeekOptionsConverter.Package(): Zipping all files ...");

            var destination = Path.Combine(_destination, "option");
            var dateMask = date.ToStringInvariant(DateFormat.EightCharacter);

            var files =
                Directory.EnumerateFiles(destination, dateMask + "*.csv", SearchOption.AllDirectories)
                .GroupBy(x => Directory.GetParent(x).FullName);

            //Zip each file massively in parallel.
            Parallel.ForEach(files, file =>
            {
                try
                {
                    var outputFileName = file.Key + ".zip";
                    // Create and open a new ZIP file
                    var filesToCompress = Directory.GetFiles(file.Key, "*.csv", SearchOption.AllDirectories);
                    using (var zip = ZipFile.Open(outputFileName, ZipArchiveMode.Create))
                    {
                        Log.Trace("AlgoSeekOptionsConverter.Package(): Zipping " + outputFileName);

                        foreach (var fileToCompress in filesToCompress)
                        {
                            // Add the entry for each file
                            zip.CreateEntryFromFile(fileToCompress, Path.GetFileName(fileToCompress), CompressionLevel.Optimal);
                        }
                    }

                    try
                    {
                        Directory.Delete(file.Key, true);
                    }
                    catch (Exception err)
                    {
                        Log.Error("AlgoSeekOptionsConverter.Package(): Directory.Delete returned error: " + err.Message);
                    }
                }
                catch (Exception err)
                {
                    Log.Error("File: {0} Err: {1} Source {2} Stack {3}", file, err.Message, err.Source, err.StackTrace);
                }
            });
        }

        /// <summary>
        /// Cleans zip archives and source data folders before run
        /// </summary>
        public void Clean(DateTime date)
        {
            Log.Trace("AlgoSeekOptionsConverter.Clean(): cleaning all zip and csv files for {0} before start...", date.ToShortDateString());

            var extensions = new HashSet<string> { ".zip", ".csv" };
            var destination = Path.Combine(_destination, "option");
            Directory.CreateDirectory(destination);
            var dateMask = date.ToStringInvariant(DateFormat.EightCharacter);

            var files =
                Directory.EnumerateFiles(destination, dateMask + "_" + "*.*", SearchOption.AllDirectories)
                .Where(x => extensions.Contains(Path.GetExtension(x)))
                .ToList();

            Log.Trace("AlgoSeekOptionsConverter.Clean(): found {0} files..", files.Count);

            //Clean each file massively in parallel.
            Parallel.ForEach(files, parallelOptionsZipping, file =>
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception err)
                {
                    Log.Error("AlgoSeekOptionsConverter.Clean(): File.Delete returned error: " + err.Message);
                }
            });
        }

        /// <summary>
        /// Decompress huge AlgoSeek's opra bz2 files and returns the outcome status.
        /// </summary>
        /// <param name="compressedRawDatafile">Fileinfo of the compressed orpa file.</param>
        /// <param name="rawDatafile">Fileinfo of the uncompressed output file.</param>
        /// <returns>Boolean indicating if this the process was successful.</returns>
        /// <remarks>Public static members of the SharpZipLib.BZip2 type are safe for multithreaded operations.
        ///          Source: https://documentation.help/SharpZip/ICSharpCode.SharpZipLib.BZip2.BZip2.html
        /// </remarks>>
        private static bool DecompressOpraFile(FileInfo compressedRawDatafile, FileInfo rawDatafile)
        {
            var outcome = false;
            using (FileStream fileToDecompressAsStream = compressedRawDatafile.OpenRead())
            using (FileStream decompressedStream = File.Create(rawDatafile.FullName))
            {
                try
                {
                    BZip2.Decompress(fileToDecompressAsStream, decompressedStream);
                    outcome = true;
                }
                catch (Exception ex)
                {
                    Log.Error($"AlgoSeekOptionsConverter.DecompressOpraFile({compressedRawDatafile.Name}, {rawDatafile.Name}): SharpzipLib.BZip2.Decompress returned error: " + ex);
                }
            }
            return outcome;
        }
    }
}
