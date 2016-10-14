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
using QuantConnect.Util;

namespace QuantConnect.ToolBox.AlgoSeekFuturesConverter
{
    /// <summary>
    /// AlgoSeek Options Converter: Convert raw OPRA channel files into QuantConnect Options Data Format.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var date = args[0];
            // There are practical file limits we need to override for this to work. 
            // By default programs are only allowed 1024 files open; for futures parsing we need 100k
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "disabled");
            Log.LogHandler = new CompositeLogHandler(new ILogHandler[] { new ConsoleLogHandler(), new FileLogHandler("log.txt") });

            // Directory for the data, output and processed cache:
            var dataDirectory = Config.Get("data-directory");
            var sourceDirectory = Config.Get("futures-source-directory").Replace("{0}", date);

            // Date for the option bz files.
            var referenceDate = DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
            //var referenceDate = DateTime.ParseExact(Config.Get("futures-reference-date"), DateFormat.EightCharacter, CultureInfo.InvariantCulture);

            // Convert the date:
            var timer = Stopwatch.StartNew();
            var converter = new AlgoSeekFuturesConverter(Resolution.Minute, referenceDate, sourceDirectory, dataDirectory);
            converter.Convert();
            Log.Trace(string.Format("AlgoSeekFuturesConverter.Main(): {0} Conversion finished in time: {1}", referenceDate, timer.Elapsed));

            // Compress the memory cache to zips.
            timer.Restart();
            converter.Package(referenceDate);
            Log.Trace(string.Format("AlgoSeekFuturesConverter.Main(): {0} Compression finished in time: {1}", referenceDate, timer.Elapsed));
        }
    }
}
