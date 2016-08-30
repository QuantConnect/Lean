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
using QuantConnect.Configuration;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    public class Program
    {
      public static void Main()
        {
			Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "disabled");

			var sourceDirectory = "/Users/jona8276/Projects/QuantConnect/TestData/RawGoog/20151224";
			var destinationDirectory = "/Users/jona8276/Projects/QuantConnect/TestData";

//            var sourceDirectory = Config.Get("sourceDirectory");
//            var destinationDirectory = Config.Get("destinationDirectory");

//            var logFilePath = Config.Get("logFilePath");
            var logFilePath = "AlgoSeekOptionsConverter";
            Log.LogHandler = new CompositeLogHandler(new ILogHandler[]
            {
                new ConsoleLogHandler(),
                new FileLogHandler(logFilePath)
            });

//			var flushInterval = long.Parse(Config.Get("flushInterval"));
            var flushInterval = 1000000L;
            var converter = new AlgoSeekOptionsConverter();

			var conversionTimer = Stopwatch.StartNew();
			converter.ConvertToFineResolution(sourceDirectory, destinationDirectory, flushInterval);
			conversionTimer.Stop();

			Log.Trace(string.Format("Conversion finished in time: {0}", conversionTimer.Elapsed));

//            var compressionTimer = Stopwatch.StartNew();
//            converter.Compress(destinationDirectory, 1);
//            compressionTimer.Stop();
//
//            Log.Trace(string.Format("Compression finished in time: {0}", compressionTimer.Elapsed));
        }
    }
}
