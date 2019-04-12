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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    using Processors = Dictionary<string, List<AlgoSeekOptionsProcessor>>;

    /// <summary>
    ///     Process a directory of algoseek option files into separate resolutions.
    /// </summary>
    public class AlgoSeekOptionsConverterMultipleInstances
    {
        private readonly Resolution _resolution = Resolution.Minute;
        private readonly bool _testing = Config.GetBool("testing", false);

        private readonly ParallelOptions parallelOptionsZipping = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 };
        private readonly DirectoryInfo _destination;
        private readonly List<Processors> _globalProcessors;
        private readonly DateTime _referenceDate;
        private readonly FileInfo _remoteOpraFile;
        private readonly DirectoryInfo _source;

        private long _totalLinesProcessed;

        public AlgoSeekOptionsConverterMultipleInstances(DateTime referenceDate, string sourceDirectory, string dataDirectory, FileInfo remoteOpraFile)
        {
            _referenceDate = referenceDate;
            _source = new DirectoryInfo(sourceDirectory);
            _destination = new DirectoryInfo(dataDirectory);
            _remoteOpraFile = remoteOpraFile;
            _globalProcessors = new List<Processors>();
            Log.DebuggingEnabled = _testing;
        }

        /// <summary>
        ///     Give the reference date and source directory, convert the algoseek options data into n-resolutions LEAN format.
        /// </summary>
        /// <param name="symbolFilter">HashSet of symbols as string to process. *Only used for testing*</param>
        public void Convert(HashSet<string> symbolFilter = null)
        {
            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.Convert(): Copying {_remoteOpraFile.Name} into {_source.FullName}");
            var localOpraFile = _remoteOpraFile.CopyTo(Path.Combine(_source.FullName, _remoteOpraFile.Name));

            Log.Trace(
                $"AlgoSeekOptionsConverterMultipleInstances.Convert(): {localOpraFile.Name} OPRA files for {_referenceDate:yyyy-MM-dd} " +
                $"with total size of {localOpraFile.Length / Math.Pow(1024, 3):N1} GB copied locally."
            );

            var decompressedOpraFile = new FileInfo(Path.Combine(_source.FullName, Path.GetFileNameWithoutExtension(localOpraFile.Name)));
            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.Convert(): Decompress {localOpraFile.Name} into {decompressedOpraFile.FullName}");
            var timer = new Stopwatch();
            timer.Start();
            if (!DecompressOpraFile(localOpraFile, decompressedOpraFile))
            {
                Log.Error($"AlgoSeekOptionsConverterMultipleInstances.Convert(): Decompressing {localOpraFile.Name} failed!");
                return;
            }

            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.Convert(): {localOpraFile.Name} decompressed in {timer.Elapsed:g} full size {decompressedOpraFile.Length / Math.Pow(1024, 3):N1} GB.");
            localOpraFile.Delete();

            var thread = new Thread(ProcessOpraFile);
            thread.Priority = ThreadPriority.Highest;
            thread.Start(decompressedOpraFile);
        }

        public void ProcessOpraFile(object opraFileInfo)
        {
            var rawDataFile = (FileInfo)opraFileInfo;
            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.ProcessOpraFile(): Starting processing {rawDataFile.Name}...");
            var processors = new Processors();
            var totalLinesProcessed = 0L;
            var start = DateTime.Now;
            using (var reader = new AlgoSeekOptionsReader(rawDataFile.FullName, _referenceDate))
            {
                var previousTime = start;

                // reader contains the data
                if (reader.Current != null)
                { 
                    do
                    {
                        var tick = reader.Current;
                        //Add or create the consolidator mechanism for symbol:

                        List<AlgoSeekOptionsProcessor> symbolProcessors;
                        if (!processors.TryGetValue(tick.SecurityRawIdentifier, out symbolProcessors))
                        {
                            symbolProcessors = new List<AlgoSeekOptionsProcessor>(3)
                            {
                                new AlgoSeekOptionsProcessor(tick.SecurityRawIdentifier, _referenceDate, TickType.Trade, _resolution, _destination.FullName),
                                new AlgoSeekOptionsProcessor(tick.SecurityRawIdentifier, _referenceDate, TickType.Quote, _resolution, _destination.FullName),
                                new AlgoSeekOptionsProcessor(tick.SecurityRawIdentifier, _referenceDate, TickType.OpenInterest, _resolution, _destination.FullName)
                            };
                            processors[tick.SecurityRawIdentifier] = symbolProcessors;
                        }

                        // Pass current tick into processor: enum 0 = trade; 1 = quote, , 2 = oi
                        symbolProcessors[(int)tick.Tick.TickType].Process(tick.Tick);

                        if (++totalLinesProcessed % 1000000 == 0)
                        {
                            var now = DateTime.Now;
                            var averageSpeed = totalLinesProcessed / 1000L / (now - start).TotalSeconds;
                            var speed = 1000 / (now - previousTime).TotalSeconds;

                            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.Convert(): {rawDataFile.Name} - Processed {totalLinesProcessed} ticks at {speed:N2} k/sec" +
                                      $"(average speed {averageSpeed:N2} k/sec);  Memory in use: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024):N2} MB");
                            previousTime = DateTime.Now;
                        }

                    } while (reader.MoveNext());
                }
            }
            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.Convert(): Finished processing file {rawDataFile.Name}, {totalLinesProcessed / 1000000L:N2} M lines processed in {DateTime.Now - start:g}.");
            rawDataFile.Delete();

            Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.Convert(): Saving processed ticks to disk...");
            WriteToDisk(processors);
        }

        /// <summary>
        ///     Write the processor queues to disk
        /// </summary>
        /// <param name="peekTickTime">Time of the next tick in the stream</param>
        /// <param name="step">Period between flushes to disk</param>
        /// <param name="final">Final push to disk</param>
        /// <returns></returns>
        private void WriteToDisk(Processors processors)
        {
            Flush(processors, DateTime.MaxValue, true);



            foreach (var processor in processors)
            {

                var entryPath = processor.Value.First().EntryPath;
                var zipPath = processor.Value.First().ZipPath;

            }


            var dataByZipFile = processors.SelectMany(p => p.Value)
                .OrderBy(p => p.Symbol.Underlying.Value)
                .GroupBy(p => p.ZipPath);
            Parallel.ForEach(
                dataByZipFile,
                parallelOptionsZipping,
                (zipFileData, loopState) =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(zipFileData.Key));

                    var filenamesAndData = new List<KeyValuePair<string, byte[]>>();
                    var dataByZipEntry = zipFileData.GroupBy(d => d.EntryPath);

                    foreach (var entryData in dataByZipEntry)
                    {
                        var data = entryData.SelectMany(d => d.Queue)
                            .OrderBy(b => b.Time)
                            .Select(b => LeanData.GenerateLine(b, SecurityType.Option, Resolution.Minute));
                        var bytesData = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, data));
                        filenamesAndData.Add(new KeyValuePair<string, byte[]>(entryData.Key, bytesData));
                    }

                    Compression.ZipData(zipFileData.Key, filenamesAndData);
                    Log.Trace($"AlgoSeekOptionsConverterMultipleInstances.WriteToDisk(): {zipFileData.Key} saved!");
                }
            );
        }

        private void Flush(Processors processors, DateTime time, bool final)
        {
            foreach (var symbol in processors.Keys) processors[symbol].ForEach(x => x.FlushBuffer(time, final));
        }

        /// <summary>
        ///     Decompress huge AlgoSeek's opra bz2 files and returns the outcome status.
        /// </summary>
        /// <param name="compressedRawDatafile">Fileinfo of the compressed orpa file.</param>
        /// <param name="rawDatafile">Fileinfo of the uncompressed output file.</param>
        /// <returns>Boolean indicating if this the process was successful.</returns>
        /// <remarks>
        ///     Public static members of the SharpZipLib.BZip2 type are safe for multithreaded operations.
        ///     Source: https://documentation.help/SharpZip/ICSharpCode.SharpZipLib.BZip2.BZip2.html
        /// </remarks>
        /// >
        private static bool DecompressOpraFile(FileInfo compressedRawDatafile, FileInfo rawDatafile)
        {
            var outcome = false;
            using (var fileToDecompressAsStream = compressedRawDatafile.OpenRead())
            using (var decompressedStream = File.Create(rawDatafile.FullName))
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

        /// <summary>
        ///     Cleans zip archives and source data folders before run
        /// </summary>
        public void Clean(DateTime date)
        {
            Log.Trace(
                "AlgoSeekOptionsConverterMultipleInstances.Clean(): cleaning all zip and csv files for {0} before start...",
                date.ToShortDateString()
            );
            var extensions = new HashSet<string> { ".zip", ".csv" };
            var destination = Path.Combine(_destination.FullName, "option");
            Directory.CreateDirectory(destination);
            var dateMask = date.ToString(DateFormat.EightCharacter);
            var files = Directory.EnumerateFiles(destination, dateMask + "_" + "*.*", SearchOption.AllDirectories)
                .Where(x => extensions.Contains(Path.GetExtension(x))).ToList();
            Log.Trace("AlgoSeekOptionsConverterMultipleInstances.Clean(): found {0} files..", files.Count);

            //Clean each file massively in parallel.
            Parallel.ForEach(
                files,
                parallelOptionsZipping,
                file =>
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception err)
                    {
                        Log.Error("AlgoSeekOptionsConverterMultipleInstances.Clean(): File.Delete returned error: " + err.Message);
                    }
                }
            );
        }
    }
}