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
using QuantConnect.Logging;
using System.Diagnostics;
using System.Globalization;
using QuantConnect.Configuration;
using System.IO;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    /// <summary>
    /// AlgoSeek Options Converter: Convert raw OPRA channel files into QuantConnect Options Data Format.
    /// </summary>
    public static class AlgoSeekOptionsConverterProgram
    {
        public static void AlgoSeekOptionsConverter(string date)
        {
            // There are practical file limits we need to override for this to work.
            // By default programs are only allowed 1024 files open; for options parsing we need 100k
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "disabled");
            Environment.SetEnvironmentVariable("MONO_GC_PARAMS", "max-heap-size=4g");
            Log.LogHandler = new CompositeLogHandler(new ILogHandler[] { new ConsoleLogHandler(), new FileLogHandler("log.txt") });

            // Directory for the data, output and processed cache:
            var remoteMask = Config.Get("options-remote-file-mask", "*.bz2").Replace("{0}", date);
            var remoteDirectory = Config.Get("options-remote-directory").Replace("{0}", date);
            var sourceDirectory = Config.Get("options-source-directory").Replace("{0}", date);
            var dataDirectory = Config.Get("data-directory").Replace("{0}", date);
            var cleanSourceDirectory = Config.GetBool("clean-source-directory", false);

            Log.Trace("CONFIGURATION:");
            Log.Trace("Processor Count: " + Environment.ProcessorCount);
            Log.Trace("Remote Directory: " + remoteDirectory);
            Log.Trace("Source Directory: " + sourceDirectory);
            Log.Trace("Destination Directory: " + dataDirectory);

            // Date for the option bz files.
            var referenceDate = DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);

            Log.Trace($"DateTime: {referenceDate.Date.ToStringInvariant()}");

            // checking if remote folder exists
            if (!Directory.Exists(remoteDirectory))
            {
                Log.Error("Remote Directory doesn't exist: " + remoteDirectory);
                return;
            }

            // Convert the date:
            var timer = Stopwatch.StartNew();
            var converter = new AlgoSeekOptionsConverter(Resolution.Minute, referenceDate, remoteDirectory, remoteMask, sourceDirectory, dataDirectory);

            converter.Clean(referenceDate);
            Log.Trace($"AlgoSeekOptionConverter.Main(): {referenceDate.ToStringInvariant()} Cleaning finished in time: {timer.Elapsed.ToStringInvariant(null)}");

            timer.Restart();
            converter.Convert();
            Log.Trace($"AlgoSeekOptionConverter.Main(): {referenceDate.ToStringInvariant()} Conversion finished in time: {timer.Elapsed.ToStringInvariant(null)}");

            timer.Restart();
            converter.Package(referenceDate);
            Log.Trace($"AlgoSeekOptionConverter.Main(): {referenceDate.ToStringInvariant()} Compression finished in time: {timer.Elapsed.ToStringInvariant(null)}");

            if (cleanSourceDirectory)
            {
                Log.Trace($"AlgoSeekOptionConverter.Main(): Cleaning source directory: {sourceDirectory}");

                try
                {
                    Directory.Delete(sourceDirectory, true);
                }
                catch (Exception err)
                {
                    Log.Trace($"AlgoSeekOptionConverter.Main(): Error while cleaning source directory {err.Message}");
                }
            }

        }
    }
}
