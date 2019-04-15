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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    /// <summary>
    ///     AlgoSeek Options Converter: Convert raw OPRA channel files into QuantConnect Options Data Format.
    /// </summary>
    public static class AlgoSeekOptionsConverterProgram
    {
        public static void AlgoSeekOptionsConverter(string date, string opraFileName = "")
        {
            // There are practical file limits we need to override for this to work.
            // By default programs are only allowed 1024 files open; for options parsing we need 100k
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "disabled");
            Environment.SetEnvironmentVariable("MONO_GC_PARAMS", "max-heap-size=4g");

            // Directory for the data, output and processed cache:
            var remoteMask = Config.Get("options-remote-file-mask", "*.bz2").Replace("{0}", date);
            var remoteDirectory = Config.Get("options-remote-directory").Replace("{0}", date);
            var sourceDirectory = Config.Get("options-source-directory").Replace("{0}", date);
            var tempOutputDirectory = Config.Get("temp-output-directory");
            var cleanSourceDirectory = Config.GetBool("clean-source-directory", false);

            Log.Trace("CONFIGURATION:");
            Log.Trace("Processor Count: " + Environment.ProcessorCount);
            Log.Trace("Remote Directory: " + remoteDirectory);
            Log.Trace("Source Directory: " + sourceDirectory);
            Log.Trace("Destination Directory: " + tempOutputDirectory);

            // Date for the option bz files.
            var referenceDate = DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);

            Log.Trace("DateTime: " + referenceDate.Date);

            // checking if remote folder exists
            if (!Directory.Exists(remoteDirectory))
            {
                Log.Error("Remote Directory doesn't exist: " + remoteDirectory);
                return;
            }

            if (opraFileName == string.Empty)
                SingleInstanceProcessing(referenceDate, remoteDirectory, remoteMask, sourceDirectory, tempOutputDirectory, cleanSourceDirectory);
            else
                MultipleInstanceProcessing(referenceDate, remoteDirectory, sourceDirectory, tempOutputDirectory, opraFileName);
        }

        private static void MultipleInstanceProcessing(DateTime referenceDate, string remoteDirectory, string sourceDirectory, string destinationDirectory, string opraFileName)
        {
            // After many iterations, this shows to be GC parameters with the best performance.
            Environment.SetEnvironmentVariable("MONO_GC_PARAMS", "major=marksweep-fixed");
            var remoteOpraFile = new FileInfo(Path.Combine(remoteDirectory, opraFileName));
            if (!remoteOpraFile.Exists)
            {
                Log.Error($"AlgoSeekOptionConverter.SingleInstanceProcessing(): {remoteOpraFile.FullName} not present in remote folder.");
                return;
            }

            var converter = new AlgoSeekOptionsConverterMultipleInstances(referenceDate, sourceDirectory, destinationDirectory, remoteOpraFile);
            converter.Convert();
        }

        private static void SingleInstanceProcessing(DateTime referenceDate, string remoteDirectory, string remoteMask, string sourceDirectory, string dataDirectory, bool cleanSourceDirectory)
        {
            // Convert the date:
            var timer = Stopwatch.StartNew();
            var converter = new AlgoSeekOptionsConverterOld(Resolution.Minute, referenceDate, remoteDirectory, remoteMask, sourceDirectory, dataDirectory);

            converter.Clean(referenceDate);
            Log.Trace(string.Format("AlgoSeekOptionConverter.SingleInstanceProcessing(): {0} Cleaning finished in time: {1}", referenceDate, timer.Elapsed));

            timer.Restart();
            converter.Convert();
            Log.Trace(string.Format("AlgoSeekOptionConverter.SingleInstanceProcessing(): {0} Conversion finished in time: {1}", referenceDate, timer.Elapsed));

            timer.Restart();
            converter.Package(referenceDate);
            Log.Trace(string.Format("AlgoSeekOptionConverter.SingleInstanceProcessing(): {0} Compression finished in time: {1}", referenceDate, timer.Elapsed));

            if (cleanSourceDirectory)
            {
                Log.Trace(string.Format("AlgoSeekOptionConverter.SingleInstanceProcessing(): Cleaning source directory: {0}", sourceDirectory));

                try
                {
                    Directory.Delete(sourceDirectory, true);
                }
                catch (Exception err)
                {
                    Log.Trace(string.Format("AlgoSeekOptionConverter.SingleInstanceProcessing(): Error while cleaning source directory {0}", err.Message));
                }
            }
        }
    }
}