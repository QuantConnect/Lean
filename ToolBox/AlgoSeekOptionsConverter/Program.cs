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

using System.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Logging;
using  System.Diagnostics;
using QuantConnect.Data.Market;
using QuantConnect.Data;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    public class Program
    {
        private const string LogFilePath = "AlgoSeekOptionsConverter.txt";

        static void Main()
        {
			Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "disabled");

			var sourceDirectory = "/cache/TestData/RawGoog/20151224";
			var destinationDirectory = "/cache/TestData/Converted";

//			var sourceDirectory = "/cache/DevData/RawDataSmall/20140516";
//			var destinationDirectory = "/cache/DevData/ConvertedData";

            Log.LogHandler = new CompositeLogHandler(new ILogHandler[]
            {
                new ConsoleLogHandler(),
                new FileLogHandler(LogFilePath)
            });

			var flushInterval = 1000000L;

			Stopwatch sw = Stopwatch.StartNew();
			AlgoSeekOptionsConverter.ConvertToFineResolution(sourceDirectory, destinationDirectory, flushInterval);
			sw.Stop();

//			AlgoSeekOptionsConverter.ExtractSymbol("goog", "/cache/TestData/RawAll/20151224", "/cache/TestData/RawGoog/20151224");

			Log.Trace(String.Format("Conversion finished in time: {0}", sw.Elapsed));
            
			// TODO Coarse Resolution
			// TODO Compression
        }
    }
}
