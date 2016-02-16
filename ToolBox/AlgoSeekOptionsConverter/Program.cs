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
using System.Collections.Generic;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    public class Program
    {
        static void Main()
        {
            var directory = "F:/Downloads/AlgoSeek";//smaller";
            directory = "F:/Downloads/opra38"; // SPY 20gb file
            var dataDirectory = "./Data";

            Log.LogHandler = new CompositeLogHandler(new ILogHandler[]
            {
                new ConsoleLogHandler(),
                new FileLogHandler("AlgoSeekOptionsConverter.txt")
            });

            // first process tick/second/minute -- we'll do hour/daily at the end on a per symbol basis
            var parallelism = Environment.ProcessorCount;
            var options = new ParallelOptions {MaxDegreeOfParallelism = parallelism};
            var resolutions = new[] {Resolution.Tick, Resolution.Minute, Resolution.Second};
            
            var files = Directory.EnumerateFiles(directory);
            Parallel.ForEach(files, options, file =>
            {
                Log.Trace("Begin tick/second/minute: " + file);

                var quotes = DataProcessor.Zip(dataDirectory, resolutions, TickType.Quote, true);
                var trades = DataProcessor.Zip(dataDirectory, resolutions, TickType.Trade, true);

                var streamProvider = StreamProvider.ForExtension(Path.GetExtension(file));
                if (!RawFileProcessor.Run(file, new[] {file}, streamProvider, new AlgoSeekOptionsParser(), quotes, trades))
                { 
                    return;
                }

                Log.Trace("Completed tick/second/minute: " + file);
            });

            var optionsDirectory = Path.Combine(dataDirectory, "option", "usa", "minute");
            var directories = Directory.EnumerateDirectories(optionsDirectory);
            Parallel.ForEach(directories, options, dir =>
            {
                Log.Trace("Begin hour/daily: " + dir + " trade");
                
                var trades = DataProcessor.Zip(dataDirectory, new[] {Resolution.Hour, Resolution.Daily}, TickType.Trade, false);
                if (!RawFileProcessor.Run(dir, Directory.EnumerateFiles(dir, "*_trade_*.zip"), new ZipStreamProvider(), new LeanParser(), trades))
                {
                    return;
                }

                Log.Trace("Begin hour/daily: " + dir + " quote");
                
                var quotes = DataProcessor.Zip(dataDirectory, new[] { Resolution.Hour, Resolution.Daily }, TickType.Quote, false);
                if (!RawFileProcessor.Run(dir, Directory.EnumerateFiles(dir, "*_quote_*.zip"), new ZipStreamProvider(), new LeanParser(), quotes))
                {
                    return;
                }

                Log.Trace("Completed hour/daily: " + dir);
            });

            Log.Trace("Finished processing directory: " + directory);
        }
    }
}
